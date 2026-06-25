import psycopg2
import json
conn = psycopg2.connect("host=localhost port=5432 dbname=cortex_db user=cortex password=cortex_password")
cur = conn.cursor()

print("--- ContentItems ---")
cur.execute('SELECT "UserId", COUNT(*) as ItemCount, SUM("ConsumeTimeMins") as TotalMins FROM "ContentItems" GROUP BY "UserId" ORDER BY ItemCount DESC;')
for row in cur.fetchall():
    print(f"UserId: {row[0]}, Count: {row[1]}, Total Mins: {row[2]}")

print("\n--- UserBehaviorEvents ---")
cur.execute('SELECT "UserId", COUNT(*) as EventCount FROM "UserBehaviorEvents" GROUP BY "UserId" ORDER BY EventCount DESC;')
for row in cur.fetchall():
    print(f"UserId: {row[0]}, Count: {row[1]}")

print("\n--- ChatSessions ---")
cur.execute('SELECT "UserId", COUNT(*) as ChatCount FROM "ChatSessions" GROUP BY "UserId" ORDER BY ChatCount DESC;')
for row in cur.fetchall():
    print(f"UserId: {row[0]}, Count: {row[1]}")

cur.close()
conn.close()
