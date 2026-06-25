import psycopg2
import json
import uuid
import random
from datetime import datetime, timedelta

def run():
    conn = psycopg2.connect("host=localhost port=5432 dbname=cortex_db user=cortex password=cortex_password")
    cur = conn.cursor()

    try:
        # Get the primary user
        cur.execute('SELECT "Id" FROM "Users" LIMIT 1')
        user_row = cur.fetchone()
        if not user_row:
            print("No users found.")
            return
        user_id = user_row[0]
        print(f"Backfilling data for User: {user_id}")

        # 1. User Interests
        interests = [
            ("Machine Learning", 85, "Stable"),
            ("React Ecosystem", 92, "Emerging"),
            ("Cybersecurity", 65, "Declining"),
            ("Productivity Tips", 78, "Stable"),
            ("Indie Hacking", 88, "Emerging")
        ]
        
        cur.execute('DELETE FROM "UserInterests" WHERE "UserId" = %s', (user_id,))
        for cat, score, trend in interests:
            cur.execute("""
                INSERT INTO "UserInterests" ("Id", "UserId", "Category", "Score", "Trend", "LastCalculated", "CreatedAt", "IsDeleted")
                VALUES (%s, %s, %s, %s, %s, NOW(), NOW(), FALSE)
            """, (str(uuid.uuid4()), user_id, cat, score, trend))

        # 2. User Intents (Upcoming Plans)
        intents = [
            ("Trip to Japan 2026", 0.95),
            ("Build SaaS with AI", 0.88),
            ("Learn Rust Programming", 0.72)
        ]
        cur.execute('DELETE FROM "UserIntents" WHERE "UserId" = %s', (user_id,))
        for title, conf in intents:
            cur.execute("""
                INSERT INTO "UserIntents" ("Id", "UserId", "GoalDescription", "Confidence", "IsResolved", "CreatedAt", "IsDeleted")
                VALUES (%s, %s, %s, %s, FALSE, NOW(), FALSE)
            """, (str(uuid.uuid4()), user_id, title, conf))

        # 3. Behavior Events (to populate Activity Graph)
        cur.execute('DELETE FROM "UserBehaviorEvents" WHERE "UserId" = %s', (user_id,))
        
        event_types = [0, 1, 2] # 0: Viewed, 1: Saved, 2: Searched
        
        # Insert 150 random events over the last 14 days
        now = datetime.now()
        for i in range(150):
            days_ago = random.randint(0, 13)
            hours_ago = random.randint(0, 23)
            event_date = now - timedelta(days=days_ago, hours=hours_ago)
            evt_type = random.choice(event_types)
            
            # Weighted random to create some peaks
            if days_ago in [2, 5, 8]:
                evt_type = 1 # More saves on these days
            
            cur.execute("""
                INSERT INTO "UserBehaviorEvents" ("Id", "UserId", "EventType", "CreatedAt", "IsDeleted")
                VALUES (%s, %s, %s, %s, FALSE)
            """, (str(uuid.uuid4()), user_id, evt_type, event_date))

        conn.commit()
        print("Data backfill successful! The profile dashboard will now be fully populated.")

    except Exception as e:
        print(f"Error: {e}")
        conn.rollback()
    finally:
        cur.close()
        conn.close()

if __name__ == '__main__':
    run()
