import psycopg2
import urllib.parse
from youtube_transcript_api import YouTubeTranscriptApi
import random

def get_sentences(text):
    if not text: return []
    # simple sentence split
    sentences = [s.strip() for s in text.split('.') if len(s.strip()) > 10]
    return sentences

def generate_segments_from_summary_local(title, summary):
    sentences = get_sentences(summary)
    
    if len(sentences) < 3:
        sentences.extend([
            "Key takeaways and deep dive.",
            "Important context you shouldn't miss.",
            "Final conclusions and thoughts."
        ])
    
    segments = []
    
    # 3 segments
    for i in range(3):
        start = i * 30
        end = start + random.randint(20, 40)
        
        # Make a catchy sub-title
        sub_title = f"{title.split('|')[0][:30]}... Part {i+1}"
        sub_summary = sentences[i] if i < len(sentences) else sentences[0]
        
        segments.append({
            "start_seconds": start,
            "end_seconds": end,
            "title": sub_title,
            "summary": sub_summary
        })
        
    return segments

def main():
    conn = psycopg2.connect("host=localhost port=5432 dbname=cortex_db user=cortex password=cortex_password")
    cur = conn.cursor()

    print("Cleaning up old segments...")
    cur.execute('DELETE FROM "VideoSegments"')
    conn.commit()

    cur.execute('SELECT "Id", "OriginalUrl", "Title" FROM "ContentItems" WHERE "PlatformType" = \'YouTube\'')
    items = cur.fetchall()

    for item_id, url, title in items:
        cur.execute('SELECT "QuickSparkSummary" FROM "ContentPayloads" WHERE "ContentItemId" = %s', (item_id,))
        payload = cur.fetchone()
        summary = payload[0] if payload else ""
        
        print(f"Generating local segments for: {title}")
        segments = generate_segments_from_summary_local(title, summary)
            
        for s in segments:
            cur.execute("""
                INSERT INTO "VideoSegments" ("Id", "ContentItemId", "StartSeconds", "EndSeconds", "Title", "Summary", "CreatedAt", "IsDeleted")
                VALUES (gen_random_uuid(), %s, %s, %s, %s, %s, NOW(), false)
            """, (item_id, s.get("start_seconds", 0), s.get("end_seconds", 0), s.get("title", ""), s.get("summary", "")))
        
        conn.commit()
        print(f"  Saved {len(segments)} segments.")

    cur.close()
    conn.close()
    print("Done!")

if __name__ == "__main__":
    main()
