import os
import time
import json
import asyncio
import urllib.parse
import requests
import google.generativeai as genai
from typing import List, Dict, Any

# Ensure static dir exists
os.makedirs("static", exist_ok=True)

def get_gemini_model(api_key: str):
    genai.configure(api_key=api_key)
    return genai.GenerativeModel('gemini-1.5-flash')

synthesis_tasks: Dict[str, Dict[str, Any]] = {}

def generate_synthetic_script_and_prompts(transcripts: List[str], api_key: str) -> Dict[str, Any]:
    """
    Takes a list of transcripts from different videos and uses Gemini to 
    write a brand new script and generate visual prompts.
    """
    model = get_gemini_model(api_key)
    combined_transcript = "\n\n---\n\n".join(transcripts)
    
    prompt = f"""
    You are an expert scriptwriter for an AI Host. 
    Read the following transcripts extracted from various videos. 
    Synthesize the core factual information, themes, and key takeaways, and write a brand new, original, and engaging script. 
    
    Requirements:
    - This must be a transformative work (copyright-free). Do NOT just copy the source text.
    - Write it as a monologue for an engaging host.
    - Keep it under 1 minute of spoken text (approx 130 words).
    - Generate EXACTLY 4 detailed, high-quality image generation prompts that correspond to the flow of the script.
    - Output MUST be valid JSON with the schema:
      {{
        "script": "The full spoken text...",
        "prompts": ["Prompt 1 for opening scene", "Prompt 2", "Prompt 3", "Prompt 4"]
      }}
    
    Source Transcripts:
    {combined_transcript}
    """
    
    # Force JSON output
    response = model.generate_content(prompt, generation_config={"response_mime_type": "application/json"})
    return json.loads(response.text.strip())

async def generate_tts(text: str, output_path: str):
    """Generates audio using edge-tts (free local library)."""
    import edge_tts
    communicate = edge_tts.Communicate(text, "en-US-AriaNeural")
    await communicate.save(output_path)

def generate_images(prompts: List[str]) -> List[str]:
    """Fetches images from pollinations.ai (free, no API key)."""
    image_paths = []
    for i, prompt in enumerate(prompts):
        encoded = urllib.parse.quote(prompt)
        url = f"https://image.pollinations.ai/prompt/{encoded}?width=1080&height=1920&nologo=true"
        response = requests.get(url)
        if response.status_code == 200:
            file_path = f"static/scene_{int(time.time())}_{i}.jpg"
            with open(file_path, 'wb') as f:
                f.write(response.content)
            image_paths.append(file_path)
    return image_paths

def assemble_video(audio_path: str, image_paths: List[str], output_path: str):
    """Uses moviepy to stitch audio and images into an mp4."""
    from moviepy import AudioFileClip, ImageClip, concatenate_videoclips
    
    audio = AudioFileClip(audio_path)
    audio_duration = audio.duration
    
    # Calculate duration per image
    duration_per_image = audio_duration / len(image_paths)
    
    clips = []
    for img_path in image_paths:
        # Create an image clip and resize it slightly if needed, or just set duration
        clip = ImageClip(img_path).with_duration(duration_per_image)
        clips.append(clip)
        
    final_video = concatenate_videoclips(clips, method="compose")
    final_video = final_video.with_audio(audio)
    
    # Write to file (fps=24)
    final_video.write_videofile(
        output_path, 
        fps=24, 
        codec="libx264", 
        audio_codec="aac",
        logger=None # Suppress verbose output
    )

def synthesize_ai_video(task_id: str, transcripts: List[str], api_key: str) -> None:
    """
    Orchestrates the REAL Copyright-Free Video Synthesis pipeline.
    """
    synthesis_tasks[task_id] = {"status": "Generating script and visual prompts via Gemini...", "progress": 10, "result": None}
    try:
        # 1. Generate Script and Prompts
        print("Generating script and visual prompts via Gemini...")
        gen_data = generate_synthetic_script_and_prompts(transcripts, api_key)
        script = gen_data["script"]
        prompts = gen_data["prompts"]
        
        # Paths for temporary assets
        timestamp = int(time.time())
        audio_path = f"static/audio_{timestamp}.mp3"
        video_filename = f"synthetic_video_{timestamp}.mp4"
        video_path = f"static/{video_filename}"
        
        # 2. Generate Audio (edge-tts)
        print("Generating TTS audio via edge-tts...")
        synthesis_tasks[task_id]["status"] = "Generating TTS audio via edge-tts..."
        synthesis_tasks[task_id]["progress"] = 30
        # Since this is synchronous, run the async function
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)
        loop.run_until_complete(generate_tts(script, audio_path))
        
        # 3. Generate Images (pollinations.ai)
        print(f"Generating {len(prompts)} images via pollinations.ai...")
        synthesis_tasks[task_id]["status"] = f"Generating {len(prompts)} images via pollinations.ai..."
        synthesis_tasks[task_id]["progress"] = 50
        image_paths = generate_images(prompts)
        
        if not image_paths:
            raise Exception("Failed to generate any images.")
            
        # 4. Assemble Video (moviepy)
        print("Assembling video with moviepy...")
        synthesis_tasks[task_id]["status"] = "Assembling video with moviepy..."
        synthesis_tasks[task_id]["progress"] = 80
        assemble_video(audio_path, image_paths, video_path)
        
        # 5. Cleanup temp files
        if os.path.exists(audio_path):
            os.remove(audio_path)
        for img in image_paths:
            if os.path.exists(img):
                os.remove(img)
                
        # We proxy the static files from the FastAPI backend (localhost:8000)
        # Note: the frontend connects to the python backend locally or through the proxy
        # Since the proxy doesn't route static files automatically, we can just return the absolute URL to the python server
        final_video_url = f"http://localhost:8000/static/{video_filename}"
        
        print(f"Video successfully synthesized at {final_video_url}!")
        synthesis_tasks[task_id]["progress"] = 100
        synthesis_tasks[task_id]["status"] = "Done"
        synthesis_tasks[task_id]["result"] = {
            "success": True,
            "script": script,
            "theme": prompts[0] if prompts else "AI Generated Theme",
            "final_video_url": final_video_url,
            "broll_url": None
        }
    except Exception as e:
        import traceback
        traceback.print_exc()
        synthesis_tasks[task_id]["progress"] = 100
        synthesis_tasks[task_id]["status"] = "Error"
        synthesis_tasks[task_id]["result"] = {
            "success": False,
            "error": str(e)
        }
