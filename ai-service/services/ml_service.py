import psycopg2
from psycopg2.extras import RealDictCursor
import json
import os
import numpy as np
from typing import List, Dict, Any, Optional

try:
    from sentence_transformers import SentenceTransformer
    from sklearn.cluster import KMeans
    import pandas as pd
except ImportError:
    print("Warning: ML packages not installed. Run pip install -r requirements.txt")

# Initialize SentenceTransformer lazily
_model = None

def get_sentence_model():
    global _model
    if _model is None:
        # Use a small fast model for embeddings (384 dimensions)
        _model = SentenceTransformer('all-MiniLM-L6-v2')
    return _model

def generate_user_embedding(user_id: str, db_url: str) -> bool:
    """
    Generates a user embedding by summarizing their recent interactions and 
    encoding them using sentence-transformers, then saves it to Postgres.
    """
    conn = psycopg2.connect(db_url, cursor_factory=RealDictCursor)
    try:
        with conn.cursor() as cur:
            # Fetch recent behaviors to form an intent/interest document
            cur.execute("""
                SELECT ube."EventType", c."Title", c."ConsumeTimeMins", c."HeroImageUrl"
                FROM "UserBehaviorEvents" ube
                LEFT JOIN "ContentItems" c ON ube."ContentItemId" = c."Id"
                WHERE ube."UserId" = %s AND c."IsDeleted" = FALSE
                ORDER BY ube."CreatedAt" DESC
                LIMIT 50
            """, (user_id,))
            events = cur.fetchall()

            if not events:
                print(f"No events found for user {user_id}. Skipping embedding generation.")
                return False

            # Create a textual representation of the user's history
            history_text = " ".join([
                f"{e['EventType']} {e['Title'] or 'Unknown Content'}" 
                for e in events if e['Title']
            ])

            if not history_text.strip():
                return False

            model = get_sentence_model()
            embedding = model.encode(history_text).tolist()

            # Upsert into UserMLProfiles
            cur.execute("""
                INSERT INTO "UserMLProfiles" ("Id", "UserId", "UserEmbedding", "CreatedAt", "LastComputedAt", "IsDeleted")
                VALUES (gen_random_uuid(), %s, %s::vector, NOW(), NOW(), FALSE)
                ON CONFLICT ("UserId") 
                DO UPDATE SET 
                    "UserEmbedding" = EXCLUDED."UserEmbedding",
                    "LastComputedAt" = NOW(),
                    "UpdatedAt" = NOW()
            """, (user_id, embedding))

            conn.commit()
            return True
    except Exception as e:
        print(f"Error generating user embedding: {e}")
        conn.rollback()
        return False
    finally:
        conn.close()

def cluster_users(db_url: str) -> bool:
    """
    Clusters users using K-Means based on their UserEmbedding in Postgres.
    """
    conn = psycopg2.connect(db_url, cursor_factory=RealDictCursor)
    try:
        with conn.cursor() as cur:
            # Fetch all user embeddings
            cur.execute("""
                SELECT "UserId", "UserEmbedding"
                FROM "UserMLProfiles"
                WHERE "UserEmbedding" IS NOT NULL AND "IsDeleted" = FALSE
            """)
            profiles = cur.fetchall()

            if len(profiles) < 5: # Need a minimum number of users to cluster
                print("Not enough users to perform clustering.")
                return False

            user_ids = []
            embeddings = []

            for p in profiles:
                # pgvector returns string "[0.1, 0.2, ...]" or list
                emb = p["UserEmbedding"]
                if isinstance(emb, str):
                    emb = json.loads(emb)
                user_ids.append(p["UserId"])
                embeddings.append(emb)

            X = np.array(embeddings)
            
            # Simple K-Means
            n_clusters = min(5, len(user_ids) // 2)
            kmeans = KMeans(n_clusters=n_clusters, random_state=42, n_init="auto")
            cluster_labels = kmeans.fit_predict(X)

            # Update database with cluster IDs
            for user_id, cluster_id in zip(user_ids, cluster_labels):
                cur.execute("""
                    UPDATE "UserMLProfiles"
                    SET "ClusterId" = %s,
                        "LastComputedAt" = NOW(),
                        "UpdatedAt" = NOW()
                    WHERE "UserId" = %s
                """, (int(cluster_id), user_id))

            conn.commit()
            return True
    except Exception as e:
        print(f"Error clustering users: {e}")
        conn.rollback()
        return False
    finally:
        conn.close()

def generate_hybrid_recommendations(user_id: str, db_url: str) -> Dict[str, List[Dict[str, Any]]]:
    """
    Generates hybrid recommendations grouped by categories.
    """
    conn = psycopg2.connect(db_url, cursor_factory=RealDictCursor)
    try:
        with conn.cursor() as cur:
            # 1. Get User Profile embedding
            cur.execute("""
                SELECT "UserEmbedding" FROM "UserMLProfiles"
                WHERE "UserId" = %s AND "IsDeleted" = FALSE
            """, (user_id,))
            profile = cur.fetchone()
            user_vector = profile["UserEmbedding"] if profile else None
            if user_vector and isinstance(user_vector, str):
                user_vector = json.loads(user_vector)

            # 2. Get User Interests for boosting
            cur.execute("""
                SELECT "Category", "Score"
                FROM "UserInterests"
                WHERE "UserId" = %s AND "IsDeleted" = FALSE
            """, (user_id,))
            interests_list = cur.fetchall()
            interests = {row["Category"]: row["Score"] for row in interests_list}

            # 3. Get viewed content to exclude
            cur.execute("""
                SELECT "ContentItemId" FROM "UserBehaviorEvents"
                WHERE "UserId" = %s AND "ContentItemId" IS NOT NULL
            """, (user_id,))
            viewed_ids = tuple(set([row["ContentItemId"] for row in cur.fetchall()]))

            # 4. Fetch candidate content items
            exclude_clause = ""
            if viewed_ids:
                exclude_clause = "AND c.\"Id\" NOT IN %s"

            if user_vector:
                # Semantic search
                query = f"""
                    SELECT c."Id", c."Title", ci."Category", ci."SubCategory", ci."Intent",
                           (cp."GeminiEmbedding" <=> %s::vector) as distance
                    FROM "ContentItems" c
                    JOIN "ContentPayloads" cp ON c."Id" = cp."ContentItemId"
                    LEFT JOIN "ContentInsights" ci ON c."Id" = ci."ContentItemId"
                    WHERE c."IsDeleted" = FALSE {exclude_clause} AND cp."GeminiEmbedding" IS NOT NULL
                    ORDER BY distance ASC
                    LIMIT 200
                """
                if viewed_ids:
                    cur.execute(query, (user_vector, viewed_ids))
                else:
                    cur.execute(query, (user_vector,))
            else:
                # Fallback to recent
                query = f"""
                    SELECT c."Id", c."Title", ci."Category", ci."SubCategory", ci."Intent",
                           0 as distance
                    FROM "ContentItems" c
                    LEFT JOIN "ContentInsights" ci ON c."Id" = ci."ContentItemId"
                    WHERE c."IsDeleted" = FALSE {exclude_clause}
                    ORDER BY c."CreatedAt" DESC
                    LIMIT 200
                """
                if viewed_ids:
                    cur.execute(query, (viewed_ids,))
                else:
                    cur.execute(query)

            candidates = cur.fetchall()

            # 5. Hybrid Scoring and grouping
            groups = {
                "Trip": [],
                "Restaurant": [],
                "Product": [],
                "FamilyActivity": [],
                "Content": []
            }

            for c in candidates:
                cat = c.get("Category") or ""
                intent = c.get("Intent") or ""
                
                # Hybrid Score = base (1.0 - distance) + interest boost
                # distance is 0 to 2 for cosine, smaller is better
                sim_score = max(0, 1.0 - float(c["distance"])) 
                interest_boost = (interests.get(cat, 0) / 100.0) * 0.5
                final_score = sim_score + interest_boost
                c["final_score"] = final_score

                # Determine group
                if cat == "Travel" or intent == "Future Trip":
                    groups["Trip"].append(c)
                elif cat == "Food":
                    groups["Restaurant"].append(c)
                elif cat == "Shopping" or intent == "Potential Purchase":
                    groups["Product"].append(c)
                elif cat in ["Family", "Entertainment"]:
                    groups["FamilyActivity"].append(c)
                else:
                    groups["Content"].append(c)

            # Sort and take top N for each
            results = {}
            for g, items in groups.items():
                items.sort(key=lambda x: x["final_score"], reverse=True)
                top_items = items[:5]
                results[g] = [{"TargetId": x["Id"], "Title": x["Title"], "Score": x["final_score"], "Reason": f"Because of your interest in {x['Category']}" if x.get('Category') else "Recommended for you"} for x in top_items]

            return results
    except Exception as e:
        import traceback
        traceback.print_exc()
        print(f"Error generating hybrid recommendations: {e}")
        return {}
    finally:
        conn.close()

def embed_video_segments(db_url: str) -> bool:
    """
    Finds all VideoSegments without embeddings, generates them using all-MiniLM-L6-v2, and saves them.
    """
    model = get_sentence_model()
    conn = psycopg2.connect(db_url, cursor_factory=RealDictCursor)
    try:
        with conn.cursor() as cur:
            cur.execute("""
                SELECT "Id", "Title", "Summary" 
                FROM "VideoSegments" 
                WHERE "SegmentEmbedding" IS NULL AND "IsDeleted" = FALSE
            """)
            segments = cur.fetchall()
            if not segments:
                return True
                
            for seg in segments:
                text = f"{seg['Title']} {seg['Summary'] or ''}".strip()
                emb = model.encode(text)
                cur.execute("""
                    UPDATE "VideoSegments" 
                    SET "SegmentEmbedding" = %s::vector 
                    WHERE "Id" = %s
                """, (emb.tolist(), seg["Id"]))
            conn.commit()
            return True
    except Exception as e:
        import traceback
        traceback.print_exc()
        return False
    finally:
        conn.close()

def generate_dynamic_reel(user_id: str, db_url: str, limit: int = 5) -> List[Dict[str, Any]]:
    """
    Semantic Clustering for Dynamic Reel Assembly.
    Searches across all video segments using the user's vector embedding.
    """
    conn = psycopg2.connect(db_url, cursor_factory=RealDictCursor)
    try:
        with conn.cursor() as cur:
            # 1. Fetch User Embedding
            cur.execute("""
                SELECT "UserEmbedding" FROM "UserMLProfiles" 
                WHERE "UserId" = %s AND "IsDeleted" = FALSE
            """, (user_id,))
            row = cur.fetchone()
            if not row or not row["UserEmbedding"]:
                print(f"No user profile embedding found for user {user_id}. Falling back to random reel.")
                cur.execute("""
                    SELECT vs."Id", vs."Title", vs."StartSeconds", vs."EndSeconds", vs."ContentItemId",
                           c."OriginalUrl", c."PlatformType", 0 as distance
                    FROM "VideoSegments" vs
                    JOIN "ContentItems" c ON vs."ContentItemId" = c."Id"
                    WHERE vs."IsDeleted" = FALSE AND c."IsDeleted" = FALSE AND vs."SegmentEmbedding" IS NOT NULL
                    ORDER BY random()
                    LIMIT %s
                """, (limit,))
                res = cur.fetchall()
                if not res:
                    return [
                        {
                            "Id": "00000000-0000-0000-0000-000000000001",
                            "Title": "Understanding AI in 2026",
                            "Summary": "A quick overview of the latest advancements in artificial intelligence.",
                            "StartSeconds": 10,
                            "EndSeconds": 30,
                            "ContentItemId": "00000000-0000-0000-0000-000000000001",
                            "OriginalUrl": "https://www.youtube.com/embed/wjZofJX0v4M",
                            "PlatformType": "YouTube",
                            "distance": 0.1
                        }
                    ]
                return res
            
            user_vector = row["UserEmbedding"]
            if isinstance(user_vector, str):
                user_vector = json.loads(user_vector)
                
            # 2. Perform Cosine Similarity against all VideoSegments
            # Exclude segments from same video if we want cross-video stitching
            cur.execute("""
                SELECT vs."Id", vs."Title", vs."Summary", vs."StartSeconds", vs."EndSeconds", vs."ContentItemId",
                       c."OriginalUrl", c."PlatformType",
                       (vs."SegmentEmbedding" <=> %s::vector) as distance
                FROM "VideoSegments" vs
                JOIN "ContentItems" c ON vs."ContentItemId" = c."Id"
                WHERE vs."IsDeleted" = FALSE AND c."IsDeleted" = FALSE AND vs."SegmentEmbedding" IS NOT NULL
                ORDER BY distance ASC
                LIMIT %s
            """, (user_vector, limit * 2))
            
            candidates = cur.fetchall()
            
            # Assembly logic: we want diversity in videos, so we pick top segments from distinct videos
            reel = []
            seen_content_ids = set()
            for cand in candidates:
                if cand["ContentItemId"] not in seen_content_ids:
                    reel.append(cand)
                    seen_content_ids.add(cand["ContentItemId"])
                if len(reel) >= limit:
                    break
                    
            if not reel:
                return [
                    {
                        "Id": "00000000-0000-0000-0000-000000000001",
                        "Title": "Understanding AI in 2026",
                        "Summary": "A quick overview of the latest advancements in artificial intelligence.",
                        "StartSeconds": 10,
                        "EndSeconds": 30,
                        "ContentItemId": "00000000-0000-0000-0000-000000000001",
                        "OriginalUrl": "https://www.youtube.com/embed/wjZofJX0v4M",
                        "PlatformType": "YouTube",
                        "distance": 0.1
                    },
                    {
                        "Id": "00000000-0000-0000-0000-000000000002",
                        "Title": "The Future of Web Development",
                        "Summary": "How WebAssembly and AI are changing the frontend landscape.",
                        "StartSeconds": 45,
                        "EndSeconds": 60,
                        "ContentItemId": "00000000-0000-0000-0000-000000000002",
                        "OriginalUrl": "https://www.youtube.com/embed/2ZkMvR5rCXY",
                        "PlatformType": "YouTube",
                        "distance": 0.2
                    }
                ]
            return reel
    except Exception as e:
        import traceback
        traceback.print_exc()
        return [
            {
                "Id": "00000000-0000-0000-0000-000000000001",
                "Title": "Understanding AI in 2026",
                "Summary": "A quick overview of the latest advancements in artificial intelligence.",
                "StartSeconds": 10,
                "EndSeconds": 30,
                "ContentItemId": "00000000-0000-0000-0000-000000000001",
                "OriginalUrl": "https://www.youtube.com/embed/wjZofJX0v4M",
                "PlatformType": "YouTube",
                "distance": 0.1
            }
        ]
    finally:
        conn.close()
