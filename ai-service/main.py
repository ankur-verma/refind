from fastapi import FastAPI, Header, HTTPException, Body
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
from pydantic import BaseModel
from typing import List, Optional, Dict, Any
import os
import psycopg2
from psycopg2.extras import RealDictCursor
import requests
import json

from services.profile_service import update_user_interest_profile
from services.video_service import segment_transcript, analyze_video_file_native, extract_youtube_transcript
from services.ml_service import generate_user_embedding, cluster_users, generate_hybrid_recommendations, embed_video_segments, generate_dynamic_reel
from services.video_synthesis_service import synthesize_ai_video

app = FastAPI(title="Refind AI Brain Service", version="1.0.0")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

os.makedirs("static", exist_ok=True)
app.mount("/static", StaticFiles(directory="static"), name="static")

DEFAULT_DB_URL = "postgresql://cortex:cortex_password@localhost:5432/cortex_db"
DEFAULT_MODEL = "gemini-2.5-flash-lite"
DEFAULT_EMBEDDING_MODEL = "gemini-embedding-2"

class VideoAnalyzeRequest(BaseModel):
    url: Optional[str] = None
    raw_text: Optional[str] = None
    transcript_segments: Optional[List[Dict[str, Any]]] = None
    mp4_file_path: Optional[str] = None
    api_key: Optional[str] = None
    completion_model: Optional[str] = None

class ProfileUpdateRequest(BaseModel):
    user_id: str
    db_url: Optional[str] = None
    api_key: Optional[str] = None
    completion_model: Optional[str] = None

class SuggestCollectionsRequest(BaseModel):
    user_id: str
    db_url: Optional[str] = None
    api_key: Optional[str] = None
    completion_model: Optional[str] = None

class RecommendRequest(BaseModel):
    user_id: str
    db_url: Optional[str] = None
    api_key: Optional[str] = None
    limit: int = 5

def get_api_key(request_key: Optional[str], auth_header: Optional[str], x_key: Optional[str]) -> str:
    """Helper to extract Gemini API Key from request body, Authorization bearer header, X-header, or environment."""
    if request_key:
        return request_key
    if x_key:
        return x_key
    if auth_header and auth_header.startswith("Bearer "):
        return auth_header.split(" ")[1]
    
    env_key = os.getenv("GEMINI_API_KEY")
    if env_key:
        return env_key
        
    raise HTTPException(status_code=400, detail="Missing Gemini API Key. Provide it in body, Authorization Bearer, or X-Gemini-API-Key header.")

def get_db_url(request_url: Optional[str]) -> str:
    """Helper to get DB URL."""
    if request_url:
        return request_url
    return os.getenv("DATABASE_URL", DEFAULT_DB_URL)

def get_embedding(text: str, api_key: str) -> List[float]:
    """Helper to fetch 1536-dimensional embedding from Gemini Embedding API."""
    url = f"https://generativelanguage.googleapis.com/v1beta/models/{DEFAULT_EMBEDDING_MODEL}:embedContent?key={api_key}"
    payload = {
        "model": f"models/{DEFAULT_EMBEDDING_MODEL}",
        "content": {
            "parts": [{"text": text}]
        }
    }
    try:
        response = requests.post(url, json=payload, timeout=30)
        response.raise_for_status()
        data = response.json()
        return data["embedding"]["values"]
    except Exception as e:
        # Fallback to a mock embedding if API fails
        print(f"Failed to generate embedding from Gemini: {e}")
        # Return a zero vector or basic dummy vector
        return [0.0] * 1536

@app.get("/")
def read_root():
    return {"status": "online", "service": "Refind AI Brain"}

@app.post("/api/ai/video/analyze")
def analyze_video(
    req: VideoAnalyzeRequest = Body(...),
    authorization: Optional[str] = Header(None),
    x_gemini_api_key: Optional[str] = Header(None)
):
    try:
        api_key = get_api_key(req.api_key, authorization, x_gemini_api_key)
        model = req.completion_model or DEFAULT_MODEL
        
        # 1. Native File API path if local MP4 file path is provided
        if req.mp4_file_path:
            # Native model defaults to gemini-2.5-flash for media analysis
            native_model = req.completion_model or "gemini-2.5-flash"
            segments = analyze_video_file_native(req.mp4_file_path, api_key, native_model)
            return {"segments": segments}
            
        # 2. URL path (YouTube)
        if req.url:
            try:
                transcript_segments = extract_youtube_transcript(req.url)
                segments = segment_transcript(transcript_segments, api_key, model)
                return {"segments": segments}
            except Exception as e:
                raise HTTPException(status_code=400, detail=f"Failed to process YouTube URL: {str(e)}")

        # 3. Transcript Segments path
        if req.transcript_segments:
            segments = segment_transcript(req.transcript_segments, api_key, model)
            return {"segments": segments}
            
        # 3. Raw Text transcript path (mock timestamps or generic split)
        if req.raw_text:
            # We construct a mock segment from raw text
            words = req.raw_text.split()
            mock_segments = []
            words_per_segment = max(100, len(words) // 5)
            for i in range(0, len(words), words_per_segment):
                segment_words = words[i:i+words_per_segment]
                start_sec = (i / len(words)) * 600 # Assume 10 mins video
                mock_segments.append({
                    "start": start_sec,
                    "text": " ".join(segment_words)
                })
            segments = segment_transcript(mock_segments, api_key, model)
            return {"segments": segments}
            
        raise HTTPException(status_code=400, detail="Provide transcript_segments, raw_text, or mp4_file_path.")
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/api/ai/profile/update")
def update_profile(
    req: ProfileUpdateRequest = Body(...),
    authorization: Optional[str] = Header(None),
    x_gemini_api_key: Optional[str] = Header(None)
):
    try:
        api_key = get_api_key(req.api_key, authorization, x_gemini_api_key)
        db_url = get_db_url(req.db_url)
        model = req.completion_model or DEFAULT_MODEL
        
        result = update_user_interest_profile(req.user_id, db_url, api_key, model)
        return result
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/api/ai/collections/suggest")
def suggest_collections(
    req: SuggestCollectionsRequest = Body(...),
    authorization: Optional[str] = Header(None),
    x_gemini_api_key: Optional[str] = Header(None)
):
    # This endpoint suggests collections and interest profiles, returning them.
    # It acts identical to update_profile but can return the recommendations dynamically without forcing save,
    # or just delegates to the profile updater.
    try:
        api_key = get_api_key(req.api_key, authorization, x_gemini_api_key)
        db_url = get_db_url(req.db_url)
        model = req.completion_model or DEFAULT_MODEL
        
        result = update_user_interest_profile(req.user_id, db_url, api_key, model)
        return {"suggested_collections": result.get("collections", [])}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/api/ai/recommend")
def recommend_content(
    req: RecommendRequest = Body(...),
    authorization: Optional[str] = Header(None),
    x_gemini_api_key: Optional[str] = Header(None)
):
    try:
        api_key = get_api_key(req.api_key, authorization, x_gemini_api_key)
        db_url = get_db_url(req.db_url)
        
        conn = psycopg2.connect(db_url, cursor_factory=RealDictCursor)
        try:
            with conn.cursor() as cur:
                # 1. Fetch user's top interest categories
                cur.execute("""
                    SELECT "Category", "Score"
                    FROM "UserInterests"
                    WHERE "UserId" = %s AND "IsDeleted" = FALSE AND "Score" > 40
                    ORDER BY "Score" DESC
                    LIMIT 3
                """, (req.user_id,))
                categories = cur.fetchall()
                
                # 2. Fetch active intents
                cur.execute("""
                    SELECT "GoalDescription"
                    FROM "UserIntents"
                    WHERE "UserId" = %s AND "IsDeleted" = FALSE AND "IsResolved" = FALSE
                    ORDER BY "Confidence" DESC
                    LIMIT 2
                """, (req.user_id,))
                intents = cur.fetchall()
                
                # Combine top categories and intents to formulate query terms
                query_terms = [c["Category"] for c in categories] + [i["GoalDescription"] for i in intents]
                
                if not query_terms:
                    # Fallback to recommending most recent content items
                    cur.execute("""
                        SELECT c0."Id", c0."Title", c0."OriginalUrl", c0."HeroImageUrl", c0."ConsumeTimeMins"
                        FROM "ContentItems" c0
                        WHERE c0."UserId" = %s AND c0."IsDeleted" = FALSE AND c0."Status" = 'Ready'
                        ORDER BY c0."CreatedAt" DESC
                        LIMIT %s
                    """, (req.user_id, req.limit))
                    return {"recommendations": cur.fetchall(), "strategy": "Recent Content (No Profile)"}
                
                query_text = " ".join(query_terms)
                print(f"Recommending for user {req.user_id} using query terms: '{query_text}'")
                
                # Generate query embedding
                query_vector = get_embedding(query_text, api_key)
                
                # Pgvector similarity query. Cosine distance operator <=>
                cur.execute("""
                    SELECT c0."Id", c0."Title", c0."OriginalUrl", c0."HeroImageUrl", c0."ConsumeTimeMins",
                           (c."GeminiEmbedding" <=> %s::vector) as distance
                    FROM "ContentPayloads" c
                    JOIN "ContentItems" c0 ON c."ContentItemId" = c0."Id"
                    WHERE c0."UserId" = %s AND c0."IsDeleted" = FALSE AND c0."Status" = 'Ready' AND c."GeminiEmbedding" IS NOT NULL
                    ORDER BY distance ASC
                    LIMIT %s
                """, (query_vector, req.user_id, req.limit))
                recommendations = cur.fetchall()
                
                return {
                    "recommendations": recommendations,
                    "strategy": f"Semantic Profiling ({query_text})"
                }
        finally:
            conn.close()
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

class EmbedUserRequest(BaseModel):
    user_id: str
    db_url: Optional[str] = None
    api_key: Optional[str] = None

class ClusterRequest(BaseModel):
    db_url: Optional[str] = None
    api_key: Optional[str] = None

@app.post("/api/ml/embed/user")
def embed_user(
    req: EmbedUserRequest = Body(...),
    authorization: Optional[str] = Header(None),
    x_gemini_api_key: Optional[str] = Header(None)
):
    try:
        # api_key = get_api_key(req.api_key, authorization, x_gemini_api_key) # Not needed for local sentence-transformers, but good for validation
        db_url = get_db_url(req.db_url)
        
        success = generate_user_embedding(req.user_id, db_url)
        if not success:
            raise HTTPException(status_code=400, detail="Failed to generate user embedding. Possibly no behavioral data.")
        return {"success": True, "message": "User embedding generated and stored."}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/api/ml/cluster")
def cluster_users_endpoint(
    req: ClusterRequest = Body(...),
    authorization: Optional[str] = Header(None),
    x_gemini_api_key: Optional[str] = Header(None)
):
    try:
        db_url = get_db_url(req.db_url)
        
        success = cluster_users(db_url)
        if not success:
            raise HTTPException(status_code=400, detail="Failed to cluster users.")
        return {"success": True, "message": "Users clustered successfully."}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

class RecommendHybridRequest(BaseModel):
    user_id: str
    db_url: Optional[str] = None
    api_key: Optional[str] = None

@app.post("/api/ml/recommend_hybrid")
def recommend_hybrid_endpoint(
    req: RecommendHybridRequest = Body(...),
    authorization: Optional[str] = Header(None),
    x_gemini_api_key: Optional[str] = Header(None)
):
    try:
        db_url = get_db_url(req.db_url)
        
        results = generate_hybrid_recommendations(req.user_id, db_url)
        return {"recommendations": results}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/api/ml/embed/segments")
def embed_segments_endpoint(
    req: ClusterRequest = Body(...),
    authorization: Optional[str] = Header(None)
):
    try:
        db_url = get_db_url(req.db_url)
        success = embed_video_segments(db_url)
        if not success:
            raise HTTPException(status_code=400, detail="Failed to embed segments.")
        return {"success": True, "message": "Segments embedded successfully."}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

class DynamicReelRequest(BaseModel):
    user_id: str
    limit: int = 5
    db_url: Optional[str] = None

@app.post("/api/ml/dynamic_reel")
def dynamic_reel_endpoint(
    req: DynamicReelRequest = Body(...),
    authorization: Optional[str] = Header(None)
):
    try:
        db_url = get_db_url(req.db_url)
        reel = generate_dynamic_reel(req.user_id, db_url, req.limit)
        return {"success": True, "data": reel}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

class SynthesizeVideoRequest(BaseModel):
    transcripts: List[str]
    api_key: Optional[str] = None

from fastapi import BackgroundTasks

from services.video_synthesis_service import synthesize_ai_video, synthesis_tasks
import uuid

@app.post("/api/ml/synthesize")
def synthesize_video_endpoint(
    req: SynthesizeVideoRequest = Body(...),
    background_tasks: BackgroundTasks = None,
    authorization: Optional[str] = Header(None),
    x_gemini_api_key: Optional[str] = Header(None)
):
    try:
        api_key = get_api_key(req.api_key, authorization, x_gemini_api_key)
        if not api_key:
            raise HTTPException(status_code=401, detail="Gemini API Key is required")
            
        task_id = str(uuid.uuid4())
        # Add to background tasks
        if background_tasks:
            background_tasks.add_task(synthesize_ai_video, task_id, req.transcripts, api_key)
        else:
            # Fallback if BackgroundTasks injection fails for some reason
            import threading
            threading.Thread(target=synthesize_ai_video, args=(task_id, req.transcripts, api_key)).start()
            
        return {"task_id": task_id}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/api/ml/synthesize/status")
def synthesize_status_endpoint(task_id: str):
    task = synthesis_tasks.get(task_id)
    if not task:
        raise HTTPException(status_code=404, detail="Task not found")
    return task
