import json
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from llm import LLMClient
from db import BrainDB

app = FastAPI(title="GameAssistant Brain")
llm = LLMClient()
db = BrainDB()


class GameStateRequest(BaseModel):
    game_state: dict


class AdviceResponse(BaseModel):
    suggestion: str
    reasoning: str


class FeedbackRequest(BaseModel):
    game_state: dict
    suggestion: str
    result: str


@app.post("/advice", response_model=AdviceResponse)
async def get_advice(req: GameStateRequest):
    """Receive game state, return card-playing advice."""
    state_json = json.dumps(req.game_state, ensure_ascii=False)

    suggestion_text = llm.get_suggestion(state_json)

    suggestion = ""
    reasoning = ""
    if "Suggestion:" in suggestion_text:
        parts = suggestion_text.split("Reasoning:")
        suggestion = parts[0].replace("Suggestion:", "").strip()
        reasoning = parts[1].strip() if len(parts) > 1 else ""
    else:
        suggestion = suggestion_text.strip()
        reasoning = ""

    db.insert_round(state_json)

    return AdviceResponse(suggestion=suggestion, reasoning=reasoning)


@app.post("/feedback")
async def post_feedback(req: FeedbackRequest):
    """Phase 1 placeholder: records only, no feedback collection (Phase 2)."""
    return {"recorded": True}


@app.get("/health")
async def health():
    return {"status": "ok", "llama_server": "connected"}
