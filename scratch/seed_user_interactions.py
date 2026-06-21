import psycopg2
from psycopg2.extras import RealDictCursor
import uuid
from datetime import datetime, timedelta

DB_URL = "postgresql://cortex:cortex_password@localhost:5432/cortex_db"
EMAIL = "test@test.com"

def seed():
    conn = psycopg2.connect(DB_URL, cursor_factory=RealDictCursor)
    try:
        with conn.cursor() as cur:
            # Get user ID
            cur.execute('SELECT "Id" FROM "Users" WHERE "Email" = %s', (EMAIL,))
            user = cur.fetchone()
            if not user:
                print(f"User {EMAIL} not found.")
                return
            user_id = user["Id"]
            
            # Fetch ready content items
            cur.execute('SELECT "Id", "Title", "CreatedAt" FROM "ContentItems" WHERE "UserId" = %s AND "Status" = \'Ready\'', (user_id,))
            items = cur.fetchall()
            print(f"Found {len(items)} ready content items for user.")
            
            # Insert telemetry
            now = datetime.utcnow()
            interaction_count = 0
            
            # 1. Log "Save" interaction for each content item
            for i, item in enumerate(items):
                item_id = item["Id"]
                created_at = item["CreatedAt"]
                
                # Check if interaction already exists
                cur.execute('SELECT 1 FROM "UserInteractions" WHERE "UserId" = %s AND "ContentItemId" = %s AND "InteractionType" = \'Save\'', (user_id, item_id))
                if cur.fetchone():
                    continue
                    
                interaction_id = str(uuid.uuid4())
                cur.execute("""
                    INSERT INTO "UserInteractions" ("Id", "UserId", "ContentItemId", "InteractionType", "DurationSeconds", "SearchQuery", "CreatedAt", "IsDeleted")
                    VALUES (%s, %s, %s, 'Save', 0, NULL, %s, FALSE)
                """, (interaction_id, user_id, item_id, created_at))
                interaction_count += 1
                
                # Also log a "View" interaction for some items
                if i % 2 == 0:
                    view_id = str(uuid.uuid4())
                    view_time = created_at + timedelta(minutes=5)
                    cur.execute("""
                        INSERT INTO "UserInteractions" ("Id", "UserId", "ContentItemId", "InteractionType", "DurationSeconds", "SearchQuery", "CreatedAt", "IsDeleted")
                        VALUES (%s, %s, %s, 'View', 180, NULL, %s, FALSE)
                    """, (view_id, user_id, item_id, view_time))
                    interaction_count += 1
            
            # 2. Add search query interactions to enrich the profile with specific keywords
            searches = [
                "prompt engineering techniques and frameworks",
                "LLM performance benchmarks and MMLU evaluation",
                "Model Context Protocol spec kit and tool definition",
                "how to build autonomous orchestrator agents using langgraph or autogen",
                "vector databases and pgvector search optimization in postgreSQL"
            ]
            
            for s in searches:
                # Check if search interaction already exists
                cur.execute('SELECT 1 FROM "UserInteractions" WHERE "UserId" = %s AND "SearchQuery" = %s', (user_id, s))
                if cur.fetchone():
                    continue
                    
                search_id = str(uuid.uuid4())
                search_time = now - timedelta(hours=len(s))
                cur.execute("""
                    INSERT INTO "UserInteractions" ("Id", "UserId", "ContentItemId", "InteractionType", "DurationSeconds", "SearchQuery", "CreatedAt", "IsDeleted")
                    VALUES (%s, %s, NULL, 'Search', 15, %s, %s, FALSE)
                """, (search_id, user_id, s, search_time))
                interaction_count += 1
                
            conn.commit()
            print(f"Successfully seeded {interaction_count} telemetry interaction records for user {EMAIL}.")
    finally:
        conn.close()

if __name__ == "__main__":
    seed()
