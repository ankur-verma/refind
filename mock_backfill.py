import psycopg2
import json

conn = psycopg2.connect("host=localhost port=5432 dbname=cortex_db user=cortex password=cortex_password")
cur = conn.cursor()

cur.execute("""
    SELECT c."Id", c."Title", p."QuickSparkSummary" 
    FROM "ContentItems" c
    LEFT JOIN "ContentPayloads" p ON p."ContentItemId" = c."Id"
    LEFT JOIN "VideoSegments" v ON v."ContentItemId" = c."Id"
    WHERE c."PlatformType" = 'YouTube' AND v."Id" IS NULL
    GROUP BY c."Id", c."Title", p."QuickSparkSummary"
""")
rows = cur.fetchall()

print(f"Found {len(rows)} YouTube items needing segmentation.")

for item_id, title, summary in rows:
    print(f"Processing {title} ...")
    try:
        # Create 3 dummy segments
        segments = [
            {"start": 10, "end": 45, "title": f"{title} - Intro", "summary": summary or "Introduction"},
            {"start": 45, "end": 100, "title": f"{title} - Deep Dive", "summary": summary or "Deep dive into the topic"},
            {"start": 100, "end": 160, "title": f"{title} - Conclusion", "summary": summary or "Final thoughts"}
        ]
        
        for s in segments:
            cur.execute("""
                INSERT INTO "VideoSegments" ("Id", "ContentItemId", "StartSeconds", "EndSeconds", "Title", "Summary", "CreatedAt", "IsDeleted")
                VALUES (gen_random_uuid(), %s, %s, %s, %s, %s, NOW(), false)
            """, (item_id, s["start"], s["end"], s["title"], s["summary"]))
        
        conn.commit()
    except Exception as e:
        print(f"  -> Exception: {e}")
        conn.rollback()

cur.close()
conn.close()
print("Done.")
