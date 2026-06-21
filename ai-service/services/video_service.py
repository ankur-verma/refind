import google.generativeai as genai
import requests
import json
import time
import os
import urllib.parse
from youtube_transcript_api import YouTubeTranscriptApi

def call_gemini_openai_compat(api_key, completion_model, system_prompt, user_prompt, max_retries=3):
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
            response = requests.post(url, headers=headers, json=payload, timeout=90)
            if response.status_code == 429:
                sleep_time = 2 ** attempt
                print(f"Gemini API rate limited (429) in video service. Retrying in {sleep_time}s...")
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
            
    raise Exception(f"Failed to query Gemini API. Last error: {last_ex}")

def extract_youtube_transcript(url: str):
    """
    Extracts transcript from a YouTube URL.
    Returns a list of segments matching the expected format: [{'start': 10.5, 'text': '...'}]
    """
    parsed_url = urllib.parse.urlparse(url)
    video_id = None
    if parsed_url.hostname in ['www.youtube.com', 'youtube.com']:
        if parsed_url.path.startswith('/shorts/'):
            video_id = parsed_url.path.split('/shorts/')[1]
        else:
            query = urllib.parse.parse_qs(parsed_url.query)
            video_id = query.get("v", [None])[0]
    elif parsed_url.hostname in ['youtu.be']:
        video_id = parsed_url.path.lstrip('/')
        
    if not video_id:
        raise ValueError("Invalid YouTube URL")
        
    transcript = YouTubeTranscriptApi.get_transcript(video_id)
    return transcript

def segment_transcript(transcript_segments, api_key, completion_model):
    """
    Splits timestamped transcript segments into topic segments.
    transcript_segments should be a list of dicts: [{'start': 10.5, 'text': '...'}]
    """
    # Sample or format segments to avoid blowing up context if too long
    # Usually we can merge text into chunks of 30-45 seconds
    formatted_transcript = []
    chunk_start = None
    chunk_text = []
    
    for item in transcript_segments:
        start = item.get("start", 0.0)
        text = item.get("text", "")
        if chunk_start is None:
            chunk_start = start
        
        chunk_text.append(text)
        
        # If we have accumulated enough text or reached the end, output a chunk
        if len(chunk_text) >= 15: # roughly 15 sentences/lines (~45 seconds)
            formatted_transcript.append(f"[{int(chunk_start)}s] {' '.join(chunk_text)}")
            chunk_start = None
            chunk_text = []
            
    if chunk_text:
        formatted_transcript.append(f"[{int(chunk_start or 0)}s] {' '.join(chunk_text)}")
        
    transcript_text = "\n".join(formatted_transcript)
    
    system_prompt = (
        "You are an expert video intelligence assistant. Analyze the timestamped transcript "
        "and segment it into logical topic clips. Return ONLY a valid JSON object matching this schema:\n"
        "{\n"
        "  \"segments\": [\n"
        "    { \"start_seconds\": 0, \"end_seconds\": 180, \"title\": \"Segment Title\", \"summary\": \"Brief summary of what is discussed in this segment\" }\n"
        "  ]\n"
        "}"
    )
    
    user_prompt = f"Analyze the following video transcript segments:\n{transcript_text}\n\nSplit this video into logical topic segments."
    
    response_content = call_gemini_openai_compat(api_key, completion_model, system_prompt, user_prompt)
    data = json.loads(response_content)
    return data.get("segments", [])

def analyze_video_file_native(mp4_file_path, api_key, completion_model="gemini-2.5-flash"):
    """
    Uploads video file directly to Gemini File API and extracts topic segments.
    """
    if not os.path.exists(mp4_file_path):
        raise FileNotFoundError(f"Local video file not found at {mp4_file_path}")
        
    # Configure genai with custom key
    genai.configure(api_key=api_key)
    
    print(f"Uploading file {mp4_file_path} to Gemini File API...")
    myfile = genai.upload_file(mp4_file_path)
    print(f"Uploaded file name: {myfile.name}. Polling for ACTIVE state...")
    
    try:
        # Poll until active
        attempts = 0
        while myfile.state.name == "PROCESSING" and attempts < 60:
            time.sleep(10)
            myfile = genai.get_file(myfile.name)
            attempts += 1
            print(f"File state: {myfile.state.name} (attempt {attempts}/60)")
            
        if myfile.state.name != "ACTIVE":
            raise Exception(f"File did not become active. State: {myfile.state.name}")
            
        print("File is active. Generating content...")
        
        prompt = (
            "Analyze this video and return its topic segmentation in JSON format. "
            "Return ONLY a JSON object with this structure:\n"
            "{\n"
            "  \"segments\": [\n"
            "    { \"start_seconds\": 0, \"end_seconds\": 120, \"title\": \"Introduction\", \"summary\": \"Explaining the overview of the video topic\" }\n"
            "  ]\n"
            "}"
        )
        
        model = genai.GenerativeModel(completion_model)
        
        # Use standard generate_content
        # Set response_mime_type to application/json to enforce structured output
        generation_config = {"response_mime_type": "application/json"}
        response = model.generate_content([myfile, prompt], generation_config=generation_config)
        
        data = json.loads(response.text)
        return data.get("segments", [])
    finally:
        # Clean up file on Gemini
        try:
            print(f"Cleaning up file {myfile.name} on Gemini...")
            genai.delete_file(myfile.name)
            print("Gemini cleanup succeeded.")
        except Exception as e:
            print(f"Failed to delete file {myfile.name} from Gemini: {e}")
