import psycopg2

conn = psycopg2.connect("host=localhost port=5432 dbname=cortex_db user=cortex password=cortex_password")
cur = conn.cursor()

# Find items where the summary says "Failed"
cur.execute("""
    UPDATE "ContentItems" c
    SET "Status" = 2
    FROM "ContentPayloads" p
    WHERE p."ContentItemId" = c."Id"
      AND p."QuickSparkSummary" ILIKE '%failed to process%'
""")

# Also find where the raw text is "Failed to generate AI response from video file."
cur.execute("""
    UPDATE "ContentItems" c
    SET "Status" = 2
    FROM "ContentPayloads" p
    WHERE p."ContentItemId" = c."Id"
      AND p."RawText" ILIKE '%Failed to generate AI response from video file.%'
""")

conn.commit()
print(f"Updated {cur.rowcount} items to Failed status.")

cur.close()
conn.close()
