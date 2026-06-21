import psycopg2
import requests
import json
import time

conn = psycopg2.connect("host=localhost port=5432 dbname=cortex_db user=cortex password=cortex_password")
cur = conn.cursor()

cur.execute("""
    SELECT c."Id", c."OriginalUrl", p."RawText" 
    FROM "ContentItems" c
    LEFT JOIN "ContentPayloads" p ON p."ContentItemId" = c."Id"
    LEFT JOIN "VideoSegments" v ON v."ContentItemId" = c."Id"
    WHERE c."PlatformType" = 'YouTube' AND v."Id" IS NULL
    GROUP BY c."Id", c."OriginalUrl", p."RawText"
""")
rows = cur.fetchall()

print(f"Found {len(rows)} YouTube items needing segmentation.")

for item_id, url, raw_text in rows:
    print(f"Processing {url} ...")
    try:
        res = requests.post(
            "http://127.0.0.1:8000/api/ai/video/analyze",
            json={
                "url": url,
                "raw_text": raw_text
            },
            headers={"X-Gemini-API-Key": "AQ.Ab8RN6LWKHDUbA_0HPpEPNKR6XcUPTsHpp3b2Dn-jQXSUxyPzA"}
        )
        if res.status_code == 200:
            data = res.json()
            segments = data.get("segments", [])
            print(f"  -> Generated {len(segments)} segments")
            
            for s in segments:
                cur.execute("""
                    INSERT INTO "VideoSegments" ("Id", "ContentItemId", "StartSeconds", "EndSeconds", "Title", "Summary", "CreatedAt", "IsDeleted")
                    VALUES (gen_random_uuid(), %s, %s, %s, %s, %s, NOW(), false)
                """, (item_id, s.get("start_seconds", 0), s.get("end_seconds", 0), s.get("title", ""), s.get("summary", "")))
            
            conn.commit()
            time.sleep(1)
        else:
            print(f"  -> Failed: {res.status_code} {res.text}")
    except Exception as e:
        print(f"  -> Exception: {e}")
        conn.rollback()

cur.close()
conn.close()
print("Done.")
