import psycopg2
import urllib.request
import json

# Generate embedding for the topic using the python script's gemini call
import google.generativeai as genai
import os

with open("ai-service/.env") as f:
    for line in f:
        if line.startswith("GEMINI_API_KEY"):
            api_key = line.strip().split("=")[1].strip('"\'')
            genai.configure(api_key=api_key)

topic = "AI Model Interoperability and Protocols"
result = genai.embed_content(
    model="models/text-embedding-004",
    content=topic,
    task_type="retrieval_query",
)
query_vector = result['embedding']
vector_str = '[' + ','.join(map(str, query_vector)) + ']'

conn = psycopg2.connect('host=localhost port=5432 dbname=cortex_db user=cortex password=cortex_password')
cur = conn.cursor()
cur.execute('''
    SELECT c."Title", p."GeminiEmbedding" <=> %s::vector AS distance
    FROM "ContentPayloads" p
    JOIN "ContentItems" c ON c."Id" = p."ContentItemId"
    ORDER BY distance ASC
    LIMIT 5
''', (vector_str,))

for row in cur.fetchall():
    print(f"Distance: {row[1]:.4f} - Title: {row[0]}")
