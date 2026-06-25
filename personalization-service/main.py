from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import List, Optional
import google.generativeai as genai
import os
import json
import asyncio

app = FastAPI(title="Refind Personalization Service")

class BehaviorEvent(BaseModel):
    eventType: str
    entityId: Optional[str]
    metadata: Optional[str]
    timestamp: str

class PersonalizationRequest(BaseModel):
    userId: str
    events: List[BehaviorEvent]

class InterestScore(BaseModel):
    name: str
    score: float

class Trend(BaseModel):
    name: str
    change: float

class Prediction(BaseModel):
    intent: str
    confidence: float

class PersonalizationResponse(BaseModel):
    interests: List[InterestScore]
    trends: List[Trend]
    predictions: List[Prediction]

# Initialize Gemini
api_key = os.getenv("GEMINI_API_KEY")
if api_key:
    genai.configure(api_key=api_key)

@app.post("/api/v1/personalization/analyze", response_model=PersonalizationResponse)
async def analyze_behavior(request: PersonalizationRequest):
    """
    Analyzes a batch of recent user behavior events to detect active intents, 
    shifting interests, and trending categories using Gemini.
    """
    if not api_key:
        # Fallback Mock logic if no API key
        return PersonalizationResponse(
            interests=[InterestScore(name="Travel", score=92), InterestScore(name="Photography", score=85)],
            trends=[Trend(name="Travel", change=14), Trend(name="Food", change=-5)],
            predictions=[Prediction(intent="Planning Hokkaido Trip", confidence=91)]
        )

    try:
        model = genai.GenerativeModel('gemini-2.5-flash')
        
        # Summarize events for the prompt
        events_summary = []
        for e in request.events:
            events_summary.append(f"- {e.eventType} at {e.timestamp}: {e.metadata}")
            
        prompt = f"""
        Analyze the following user behavior events to detect their current interests, trends, and upcoming intents.
        
        User Events:
        {chr(10).join(events_summary)}
        
        Return ONLY a JSON object matching this schema:
        {{
            "interests": [{{"name": "string", "score": float (0-100)}}],
            "trends": [{{"name": "string", "change": float (-100 to 100)}}],
            "predictions": [{{"intent": "string", "confidence": float (0-100)}}]
        }}
        """
        
        # Run asynchronously
        response = await asyncio.to_thread(
            model.generate_content,
            prompt,
            generation_config={"response_mime_type": "application/json"}
        )
        
        result = json.loads(response.text)
        
        return PersonalizationResponse(
            interests=[InterestScore(**i) for i in result.get("interests", [])],
            trends=[Trend(**t) for t in result.get("trends", [])],
            predictions=[Prediction(**p) for p in result.get("predictions", [])]
        )
        
    except Exception as e:
        print(f"Error analyzing behavior: {str(e)}")
        raise HTTPException(status_code=500, detail="Failed to process personalization")

class SavedItem(BaseModel):
    id: str
    title: str
    tags: List[str]
    category: str

class CollectionsGenerateRequest(BaseModel):
    userId: str
    recentSaves: List[SavedItem]
    
class GeneratedCollection(BaseModel):
    title: str
    description: str
    category: str
    itemIds: List[str]
    confidence: float
    reasoning: str

class CollectionsGenerateResponse(BaseModel):
    collections: List[GeneratedCollection]

class MemoryInferRequest(BaseModel):
    userId: str
    recentSaves: List[SavedItem]

class MemoryTimeline(BaseModel):
    monthYear: str
    theme: str
    summary: str
    
class MemoryInferResponse(BaseModel):
    timelineEvents: List[MemoryTimeline]

@app.post("/api/v1/personalization/collections/generate", response_model=CollectionsGenerateResponse)
async def generate_collections(request: CollectionsGenerateRequest):
    if not api_key:
        return CollectionsGenerateResponse(collections=[])
    try:
        model = genai.GenerativeModel('gemini-2.5-flash')
        items_summary = [f"- {i.id}: {i.title} ({i.category}, {', '.join(i.tags)})" for i in request.recentSaves]
        prompt = f"""
        Cluster the following recently saved content items into high-level "Smart Collections". 
        A collection is an intent-based group like "Bali Travel Ideas", "Weekend Outings", or "Camera Gear".
        Do NOT create a collection if there are fewer than 2 related items.
        
        Items:
        {chr(10).join(items_summary)}
        
        Return ONLY a JSON array matching this schema for each collection:
        {{
            "collections": [
                {{
                    "title": "string",
                    "description": "string",
                    "category": "string",
                    "itemIds": ["id1", "id2"],
                    "confidence": float (0-1.0),
                    "reasoning": "string"
                }}
            ]
        }}
        """
        response = await asyncio.to_thread(
            model.generate_content,
            prompt,
            generation_config={"response_mime_type": "application/json"}
        )
        result = json.loads(response.text)
        return CollectionsGenerateResponse(collections=[GeneratedCollection(**c) for c in result.get("collections", [])])
    except Exception as e:
        print(f"Error generating collections: {str(e)}")
        raise HTTPException(status_code=500, detail="Failed to generate collections")

@app.post("/api/v1/personalization/memory/infer", response_model=MemoryInferResponse)
async def infer_memory(request: MemoryInferRequest):
    if not api_key:
        return MemoryInferResponse(timelineEvents=[])
    try:
        model = genai.GenerativeModel('gemini-2.5-flash')
        items_summary = [f"- {i.title} ({i.category})" for i in request.recentSaves]
        prompt = f"""
        Analyze these saved items and infer broad life themes or "Memory Timelines".
        For example, if they saved lots of hotel links, the theme might be "Travel Planning".
        
        Items:
        {chr(10).join(items_summary)}
        
        Return ONLY a JSON array matching this schema:
        {{
            "timelineEvents": [
                {{
                    "monthYear": "Current Month Year e.g. June 2026",
                    "theme": "string",
                    "summary": "string"
                }}
            ]
        }}
        """
        response = await asyncio.to_thread(
            model.generate_content,
            prompt,
            generation_config={"response_mime_type": "application/json"}
        )
        result = json.loads(response.text)
        return MemoryInferResponse(timelineEvents=[MemoryTimeline(**t) for t in result.get("timelineEvents", [])])
    except Exception as e:
        print(f"Error inferring memory: {str(e)}")
        raise HTTPException(status_code=500, detail="Failed to infer memory")

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8001)
