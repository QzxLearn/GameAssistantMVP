# Claude Code Task Brief — Phase 1

> Based on the Phase 1 plan in `/repo/GameAssistance/docs/REQUIRMENTS.md`

---

## Task Overview

| # | Task | Layer | Priority |
|---|------|-------|----------|
| T1 | Python Brain HTTP Service (Phase 1 complete) | Python | P0 |
| T2 | .NET AdviceClient + Worker Modification | .NET C# | P0 |

---

## T1: Python Brain HTTP Service (Phase 1)

### Working Directory
```
/repo/GameAssistance/GameAssistant/python/brain/
```

### Context Files (Required Reading)
- `/repo/GameAssistance/docs/REQUIRMENTS.md` — full document, focus on Chapters 2, 3, 5, 7
- `/repo/GameAssistance/GameAssistant/src/csharp/Core/Models/SlayTheSpireGameState.cs` — game state JSON structure
- `/repo/GameAssistance/GameAssistant/src/csharp/Core/Models/GameState.cs` — base class

### Task Description

Implement the Python Brain Phase 1 minimum viable closed loop — 5 files total.

**Phase 1 does NOT require RAG, sqlite-vss, or feedback collection.** The only job is:

> Receive GameState JSON → call llama-server → return card-playing advice

### Detailed Requirements

#### 1. `requirements.txt`

```
fastapi>=0.115.0
uvicorn>=0.30.0
httpx>=0.27.0
pydantic>=2.0.0
```

#### 2. `config.py` — Configuration

```python
import os

LLAMA_SERVER_URL = os.getenv("LLAMA_SERVER_URL", "http://localhost:8080")
LLM_MODEL_PATH = os.getenv(
    "LLM_MODEL_PATH",
    "/home/qzx/models/Qwen_Qwen3-8B-GGUF_Qwen3-8B-Q4_K_M.gguf"
)
EMBEDDING_MODEL_PATH = os.getenv(
    "EMBEDDING_MODEL_PATH",
    "/home/qzx/models/Qwen_Qwen3-Embedding-0.6B-GGUF_Qwen3-Embedding-0.6B-Q8_0.gguf"
)
BRAIN_DB = os.getenv("BRAIN_DB", "./brain.db")
BRAIN_PORT = int(os.getenv("BRAIN_PORT", "8000"))
```

#### 3. `schema.sql` — Database Schema

**Phase 1: create tables only, no vector index** (Phase 2 adds that).

```sql
-- Game round records (one every 2 seconds in Phase 1)
CREATE TABLE IF NOT EXISTS game_rounds (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    state_json  TEXT    NOT NULL,
    created_at  DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

#### 4. `db.py` — Database Management

```python
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
```

#### 5. `llm.py` — llama-server Client Wrapper (Core)

**This is the most critical file.** Encapsulates HTTP calls to llama-server.

**llama-server HTTP API:**

```python
# Completion endpoint
POST http://localhost:8080/completion
{
  "prompt": "...",
  "n_predict": 512,
  "temperature": 0.7,
  "stop": ["</s>", "USER:", "ASSISTANT:"]
}

# Embedding endpoint
POST http://localhost:8080/embedding
{
  "content": "text to embed"
}
```

**Requirements:**
- `LLMClient` class wraps the `/completion` endpoint
- `EmbeddingClient` class wraps the `/embedding` endpoint (used in Phase 2; stub it out in Phase 1)
- Timeout: `httpx` timeout=60 seconds
- Use Qwen3 chat template format for inference:

```python
# Qwen3 chat template (no thinking mode, Phase 1)
<|im_start|>system
You are a Slay the Spire game assistant...
<|im_end|>
<|im_start|>user
Current game state: {game_state_json}
Please provide card-playing advice.
<|im_end|>
<|im_start|>assistant
```

**Prompt Template (Phase 1):**

```python
SYSTEM_PROMPT = """You are a Slay the Spire game assistant.
Based on the current game state, provide the optimal card-playing advice.

The game state may include:
- Player HP / max HP / block / gold
- Current energy (current / max)
- Hand cards (name, cost, type)
- Enemy list (name, HP, intent)
- Current combat phase

Please respond in the following format:
Suggestion: <brief advice>
Reasoning: <why you recommend this>
"""

def build_prompt(game_state_json: str) -> str:
    return f"""Current game state:
{game_state_json}

Please provide card-playing advice:"""
```

#### 6. `app.py` — FastAPI Entry Point

```python
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from llm import LLMClient
from db import BrainDB

app = FastAPI(title="GameAssistant Brain")
llm = LLMClient()
db = BrainDB()

class GameStateRequest(BaseModel):
    game_state: dict  # SlayTheSpireGameState JSON object

class AdviceResponse(BaseModel):
    suggestion: str
    reasoning: str

class FeedbackRequest(BaseModel):
    game_state: dict
    suggestion: str
    result: str  # "win" or "loss"

@app.post("/advice", response_model=AdviceResponse)
async def get_advice(req: GameStateRequest):
    """Receive game state, return card-playing advice."""
    import json
    state_json = json.dumps(req.game_state, ensure_ascii=False)

    # Call LLM
    suggestion_text = llm.get_suggestion(state_json)

    # Simple parse: assumes format "Suggestion: ... Reasoning: ..."
    suggestion = ""
    reasoning = ""
    if "Suggestion:" in suggestion_text:
        parts = suggestion_text.split("Reasoning:")
        suggestion = parts[0].replace("Suggestion:", "").strip()
        reasoning = parts[1].strip() if len(parts) > 1 else ""
    else:
        suggestion = suggestion_text.strip()
        reasoning = ""

    # Store in database (Phase 1 stores, Phase 2 adds vectors)
    db.insert_round(state_json)

    return AdviceResponse(suggestion=suggestion, reasoning=reasoning)

@app.post("/feedback")
async def post_feedback(req: FeedbackRequest):
    """Phase 1 placeholder: records only, no feedback collection (Phase 2)."""
    return {"recorded": True}

@app.get("/health")
async def health():
    return {"status": "ok", "llama_server": "connected"}
```

**Startup:**
```bash
cd /repo/GameAssistance/GameAssistant/python/brain
pip install -r requirements.txt
uvicorn app:app --host 0.0.0.0 --port 8000
```

### Acceptance Criteria (T1)

1. `uvicorn app:app --port 8000` starts without ImportError or syntax errors
2. `curl http://localhost:8000/health` returns `{"status": "ok"}`
3. Manual POST test:
   ```bash
   curl -X POST http://localhost:8000/advice \
     -H "Content-Type: application/json" \
     -d '{"game_state": {"GameName":"SlayTheSpire","Floor":3,"Act":1,"Player":{"CurrentHp":65,"MaxHp":80,"Gold":25,"Block":0},"Energy":[3,3],"Hand":[{"Name":"Strike","Cost":1,"Type":"Attack"},{"Name":"Defend","Cost":1,"Type":"Skill"},{"Name":"Bash","Cost":2,"Type":"Attack"}],"Enemies":[{"Name":"Slime B.","CurrentHp":28,"MaxHp":28,"Intent":"Attack","IntentDamage":8}],"Phase":"Combat"}}'
   ```
   Returns non-empty `suggestion` and `reasoning`
4. Database `game_rounds` table has data written

### Notes

- **Do NOT add sqlite-vss in Phase 1** — vector features are Phase 2
- **Do NOT add feedback collection** — `/feedback` in Phase 1 is just a placeholder
- **Stub out embedding in `llm.py`** — write the method signatures, implement in Phase 2
- Code style: use type hints, async/await, and `httpx` instead of `requests`
- All paths via `config.py` constants, no hardcoding

---

## T2: .NET AdviceClient + Worker Modification

### Working Directory
```
/repo/GameAssistance/GameAssistant/src/csharp/
```

### Context Files (Required Reading)
- `/repo/GameAssistance/docs/REQUIRMENTS.md`
- `/repo/GameAssistance/GameAssistant/src/csharp/Background/Worker.cs` — existing Worker
- `/repo/GameAssistance/GameAssistant/src/csharp/Core/Models/SlayTheSpireGameState.cs` — data model
- `/repo/GameAssistance/GameAssistant/src/csharp/Background/GameAssistant.Worker.csproj`

### Task Description

Modify the existing Worker to call the Python Brain `/advice` endpoint after each screenshot-parse cycle.

### Detailed Requirements

#### 1. New file: `Infrastructure/AI/AdviceClient.cs`

```csharp
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameAssistant.Core.Models;

namespace GameAssistant.Infrastructure.AI;

public class AdviceRequest
{
    [JsonPropertyName("game_state")]
    public GameStateDto GameState { get; set; } = null!;
}

public class GameStateDto
{
    // Flattened SlayTheSpireGameState — only essential fields
    [JsonPropertyName("game_name")]
    public string GameName { get; set; } = "";

    [JsonPropertyName("floor")]
    public int Floor { get; set; }

    [JsonPropertyName("act")]
    public int Act { get; set; }

    [JsonPropertyName("player_hp")]
    public int PlayerHp { get; set; }

    [JsonPropertyName("player_max_hp")]
    public int PlayerMaxHp { get; set; }

    [JsonPropertyName("player_block")]
    public int PlayerBlock { get; set; }

    [JsonPropertyName("player_gold")]
    public int PlayerGold { get; set; }

    [JsonPropertyName("energy_current")]
    public int EnergyCurrent { get; set; }

    [JsonPropertyName("energy_max")]
    public int EnergyMax { get; set; }

    [JsonPropertyName("hand")]
    public List<CardDto> Hand { get; set; } = new();

    [JsonPropertyName("enemies")]
    public List<EnemyDto> Enemies { get; set; } = new();

    [JsonPropertyName("phase")]
    public string Phase { get; set; } = "";
}

public class CardDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("cost")]
    public int Cost { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";
}

public class EnemyDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("hp")]
    public int CurrentHp { get; set; }

    [JsonPropertyName("intent")]
    public string? Intent { get; set; }

    [JsonPropertyName("intent_damage")]
    public int? IntentDamage { get; set; }
}

public class AdviceResponse
{
    [JsonPropertyName("suggestion")]
    public string Suggestion { get; set; } = "";

    [JsonPropertyName("reasoning")]
    public string Reasoning { get; set; } = "";
}

public class AdviceClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly ILogger<AdviceClient> _logger;

    public AdviceClient(string baseUrl, ILogger<AdviceClient> logger)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _logger = logger;
    }

    public async Task<AdviceResponse?> GetAdviceAsync(SlayTheSpireGameState state, CancellationToken ct = default)
    {
        try
        {
            var dto = MapToDto(state);
            var request = new AdviceRequest { GameState = dto };

            var response = await _http.PostAsJsonAsync($"{_baseUrl}/advice", request, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<AdviceResponse>(ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("Python Brain HTTP call failed: {Message}", ex.Message);
            return null;
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Python Brain request timed out: {Message}", ex.Message);
            return null;
        }
    }

    public void Dispose() => _http.Dispose();

    private static GameStateDto MapToDto(SlayTheSpireGameState state) => new()
    {
        GameName = state.GameName,
        Floor = state.Floor,
        Act = state.Act,
        PlayerHp = state.Player.CurrentHp,
        PlayerMaxHp = state.Player.MaxHp,
        PlayerBlock = state.Player.Block,
        PlayerGold = state.Player.Gold,
        EnergyCurrent = state.Energy.current,
        EnergyMax = state.Energy.max,
        Phase = state.Phase.ToString(),
        Hand = state.Hand.Select(c => new CardDto
        {
            Name = c.Name,
            Cost = c.Cost,
            Type = c.Type.ToString()
        }).ToList(),
        Enemies = state.Enemies.Select(e => new EnemyDto
        {
            Name = e.Name,
            CurrentHp = e.CurrentHp,
            Intent = e.Intent?.ToString(),
            IntentDamage = e.IntentDamage
        }).ToList()
    };
}
```

#### 2. Modify `Background/Worker.cs`

Changes:
1. Register `AdviceClient` in the DI container (as Singleton)
2. After each parse cycle, call `adviceClient.GetAdviceAsync(gameState)`
3. Log advice via `LogInformation`

```csharp
// In ExecuteAsync main loop, after each gameState is parsed:
var advice = await _adviceClient.GetAdviceAsync(gameState);
if (advice != null)
{
    _logger.LogInformation("💡 Card advice: {Suggestion} | Reasoning: {Reasoning}",
        advice.Suggestion, advice.Reasoning);
}
```

**Notes:**
- `AdviceClient` is registered as **Singleton** — reuse the same instance across cycles
- HTTP call failures do NOT crash the main loop (only log warning, continue)
- Advice uses `LogInformation`, not `LogDebug` (easier to debug)

### Acceptance Criteria (T2)

1. `dotnet build` passes with no compilation errors
2. At runtime, `LogInformation` shows HTTP requests going out (confirms call chain connectivity)
3. When Python Brain is unavailable, Worker does not crash — only logs a warning

---

## Parallel Execution

- T1 and T2 can be developed in parallel — they are independent
- T1 must be completed before T2 can be end-to-end tested
- During development, each side can mock the other: Python uses hardcoded JSON responses, .NET uses a mock AdviceClient

---

## Quality Requirements

1. **Type safety**: .NET code must use strong types — no `dynamic` or anonymous objects
2. **Fault isolation**: HTTP call failures do not affect the Worker main loop
3. **Configurable**: all URLs, ports, and timeouts via constructor/configuration injection — no hardcoding
4. **Logging**: all critical paths must log (request received, LLM called, advice returned)
5. **No phantom dependencies**: only use packages explicitly declared in `requirements.txt` / `csproj`
