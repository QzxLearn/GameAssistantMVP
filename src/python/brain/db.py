import sqlite3
from pathlib import Path


class BrainDB:
    def __init__(self, db_path: str = "./brain.db"):
        self.db_path = db_path
        self._init_db()

    def _init_db(self):
        schema = Path("schema.sql").read_text()
        with sqlite3.connect(self.db_path) as conn:
            conn.executescript(schema)

    def insert_round(self, state_json: str) -> int:
        with sqlite3.connect(self.db_path) as conn:
            cur = conn.execute(
                "INSERT INTO game_rounds(state_json) VALUES(?)",
                [state_json]
            )
            conn.commit()
            return cur.lastrowid

    def get_all_rounds(self):
        with sqlite3.connect(self.db_path) as conn:
            return conn.execute(
                "SELECT id, state_json, created_at FROM game_rounds ORDER BY id DESC LIMIT 100"
            ).fetchall()
