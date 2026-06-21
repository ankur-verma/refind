import psycopg2
from psycopg2.extras import RealDictCursor
import requests
import json
import uuid
from datetime import datetime
import time

def call_gemini_with_retry(api_key, completion_model, system_prompt, user_prompt, max_retries=3):
    url = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions"
    headers = {
        "Authorization": f"Bearer {api_key}",
        "Content-Type": "application/json"
    }
    payload = {
        "model": completion_model,
        "messages": [
            {"role": "system", "content": system_prompt},
            {"role": "user", "content": user_prompt}
        ],
        "response_format": {"type": "json_object"},
        "temperature": 0.2,
        "max_tokens": 1500
    }
    
    last_ex = None
    for attempt in range(1, max_retries + 1):
        try:
            response = requests.post(url, headers=headers, json=payload, timeout=60)
            if response.status_code == 429:
                # Exponential backoff on rate limit
                sleep_time = 2 ** attempt
                print(f"Gemini API rate limited (429). Retrying in {sleep_time}s...")
                time.sleep(sleep_time)
                continue
            response.raise_for_status()
            result = response.json()
            return result['choices'][0]['message']['content']
        except Exception as e:
            last_ex = e
            sleep_time = 1 * attempt
            print(f"Gemini API attempt {attempt} failed: {e}. Retrying in {sleep_time}s...")
            time.sleep(sleep_time)
            
    raise Exception(f"Failed to query Gemini API after {max_retries} attempts. Last error: {last_ex}")

def update_user_interest_profile(user_id: str, db_url: str, api_key: str, completion_model: str):
    conn = psycopg2.connect(db_url, cursor_factory=RealDictCursor)
    try:
        with conn.cursor() as cur:
            # 1. Fetch user interactions
            cur.execute("""
                SELECT ui."InteractionType", ui."DurationSeconds", ui."SearchQuery", ui."CreatedAt", ci."Title", ci."OriginalUrl"
                FROM "UserInteractions" ui
                LEFT JOIN "ContentItems" ci ON ui."ContentItemId" = ci."Id"
                WHERE ui."UserId" = %s AND ui."IsDeleted" = FALSE
                ORDER BY ui."CreatedAt" DESC
                LIMIT 150
            """, (user_id,))
            interactions = cur.fetchall()
            
            # If no interactions, we can't build a profile yet
            if not interactions:
                print(f"No interactions found for user {user_id}. Skipping profile generation.")
                return {"message": "No interactions found to build profile"}

            # Format interactions for Gemini prompt
            logs_summary = []
            for item in interactions:
                created_at_str = item['CreatedAt'].strftime('%Y-%m-%d %H:%M:%S') if item['CreatedAt'] else ""
                log_entry = f"[{created_at_str}] Type: {item['InteractionType']}"
                if item['Title']:
                    log_entry += f", Content: '{item['Title']}'"
                if item['SearchQuery']:
                    log_entry += f", Search: '{item['SearchQuery']}'"
                if item['DurationSeconds'] > 0:
                    log_entry += f", Duration: {item['DurationSeconds']}s"
                logs_summary.append(log_entry)
            
            logs_text = "\n".join(logs_summary)
            
            # 2. Query Gemini to build interest scores, trends, intents, and suggested auto-collections
            system_prompt = (
                "You are an expert AI profiling assistant. Analyze the user's activity log "
                "to understand their learning interests, emerging trends, active goals (intents), "
                "and recommend collections. Return ONLY a valid JSON object matching this schema:\n"
                "{\n"
                "  \"categories\": [\n"
                "    { \"category\": \"Travel|Shopping|Technology|Cooking|Hobbies|Career|Fitness|Finance|Health|Family\", \"score\": 85, \"trend\": \"Emerging|Stable|Declining|Temporary\" }\n"
                "  ],\n"
                "  \"intents\": [\n"
                "    { \"goal_description\": \"Goal description\", \"confidence\": 0.9, \"is_resolved\": false }\n"
                "  ],\n"
                "  \"collections\": [\n"
                "    { \"name\": \"Collection Name\", \"description\": \"Collection Description\", \"intent_description\": \"Goal description\" }\n"
                "  ]\n"
                "}"
            )
            
            user_prompt = f"Here are the recent user activity logs:\n{logs_text}\n\nPerform interest profiling based on these logs."
            
            response_content = call_gemini_with_retry(api_key, completion_model, system_prompt, user_prompt)
            data = json.loads(response_content)
            
            # 3. Save calculations in DB transaction
            # Delete old interest profile for the user
            cur.execute('DELETE FROM "UserInterestProfiles" WHERE "UserId" = %s', (user_id,))
            
            # Insert new interest categories
            now = datetime.utcnow()
            for cat in data.get("categories", []):
                profile_id = str(uuid.uuid4())
                cur.execute("""
                    INSERT INTO "UserInterestProfiles" ("Id", "UserId", "Category", "Score", "Trend", "LastCalculated", "CreatedAt", "IsDeleted")
                    VALUES (%s, %s, %s, %s, %s, %s, %s, FALSE)
                """, (profile_id, user_id, cat["category"], cat["score"], cat["trend"], now, now))
            
            # Handle intents
            # Fetch existing unresolved intents to check for updates or resolutions
            cur.execute("""
                SELECT "Id", "GoalDescription", "IsResolved"
                FROM "UserIntents"
                WHERE "UserId" = %s AND "IsDeleted" = FALSE
            """, (user_id,))
            existing_intents = {item["GoalDescription"]: item for item in cur.fetchall()}
            
            # Mark all existing as resolved unless they are in the new list
            new_intent_goals = {intent["goal_description"] for intent in data.get("intents", [])}
            for goal, item in existing_intents.items():
                if not item["IsResolved"] and goal not in new_intent_goals:
                    # Resolve intent
                    cur.execute('UPDATE "UserIntents" SET "IsResolved" = TRUE WHERE "Id" = %s', (item["Id"],))
            
            intent_map = {} # maps goal description to intent ID in DB
            for intent in data.get("intents", []):
                goal_desc = intent["goal_description"]
                confidence = intent["confidence"]
                is_resolved = intent.get("is_resolved", False)
                
                if goal_desc in existing_intents:
                    intent_id = existing_intents[goal_desc]["Id"]
                    cur.execute("""
                        UPDATE "UserIntents"
                        SET "Confidence" = %s, "IsResolved" = %s
                        WHERE "Id" = %s
                    """, (confidence, is_resolved, intent_id))
                else:
                    intent_id = str(uuid.uuid4())
                    cur.execute("""
                        INSERT INTO "UserIntents" ("Id", "UserId", "GoalDescription", "Confidence", "IsResolved", "CreatedAt", "IsDeleted")
                        VALUES (%s, %s, %s, %s, %s, %s, FALSE)
                    """, (intent_id, user_id, goal_desc, confidence, is_resolved, now))
                intent_map[goal_desc] = intent_id
            
            # Handle auto-collections
            # Fetch existing auto collections
            cur.execute("""
                SELECT "Name" FROM "AutoCollections"
                WHERE "UserId" = %s AND "IsDeleted" = FALSE
            """, (user_id,))
            existing_col_names = {item["Name"] for item in cur.fetchall()}
            
            for col in data.get("collections", []):
                col_name = col["name"]
                if col_name in existing_col_names:
                    continue # avoid duplicate collections
                    
                col_desc = col["description"]
                intent_desc = col.get("intent_description")
                intent_id = intent_map.get(intent_desc) if intent_desc else None
                
                col_id = str(uuid.uuid4())
                cur.execute("""
                    INSERT INTO "AutoCollections" ("Id", "UserId", "UserIntentId", "Name", "Description", "CreatedAt", "IsDeleted")
                    VALUES (%s, %s, %s, %s, %s, %s, FALSE)
                """, (col_id, user_id, intent_id, col_name, col_desc, now))
                
            conn.commit()
            return {
                "status": "success",
                "categories": data.get("categories", []),
                "intents": data.get("intents", []),
                "collections": data.get("collections", [])
            }
    except Exception as e:
        conn.rollback()
        raise e
    finally:
        conn.close()
