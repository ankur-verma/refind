import psycopg2
import json

conn = psycopg2.connect("host=localhost port=5432 dbname=cortex_db user=cortex password=cortex_password")
cur = conn.cursor()

print("--- UserBehaviorEvents ---")
cur.execute('SELECT "UserId", "EventType", "CreatedAt" FROM "UserBehaviorEvents" ORDER BY "CreatedAt" DESC LIMIT 10;')
rows = cur.fetchall()
if len(rows) == 0:
    print("No events found!")
else:
    for row in rows:
        print(f"UserId: {row[0]}, EventType: {row[1]}, Date: {row[2]}")

cur.close()
conn.close()
