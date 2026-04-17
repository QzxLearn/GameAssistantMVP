# Claude Code 任务提示词 — Phase 1

> 基于 `/repo/GameAssistance/docs/REQUIRMENTS.md` 的 Phase 1 规划

---

## 任务概览

| # | 任务 | 负责层 | 优先级 |
|---|------|--------|--------|
| T1 | Python Brain HTTP 服务（Phase 1 全部） | Python | P0 |
| T2 | .NET AdviceClient + Worker 改造 | .NET C# | P0 |

---

## T1：Python Brain HTTP 服务（Phase 1）

### 工作目录
```
/repo/GameAssistance/GameAssistant/python/brain/
```

### 上下文文件（必读）
- `/repo/GameAssistance/docs/REQUIRMENTS.md` — 全文，重点读第 2、3、5、7 章
- `/repo/GameAssistance/GameAssistant/src/csharp/Core/Models/SlayTheSpireGameState.cs` — 游戏状态 JSON 结构
- `/repo/GameAssistance/GameAssistant/src/csharp/Core/Models/GameState.cs` — 基类

### 任务描述

实现 Python Brain 的 Phase 1 最小闭环 MVP，共 5 个文件。

**Phase 1 不需要 RAG，不需要 sqlite-vss，不需要反馈收集**，只做一件事：

> 接收 GameState JSON → 调用 llama-server → 返回出牌建议

### 详细要求

#### 1. `requirements.txt`

```
fastapi>=0.115.0
uvicorn>=0.30.0
httpx>=0.27.0
pydantic>=2.0.0
```

#### 2. `config.py` — 配置文件

```python
import os

LLAMA_SERVER_URL = os.getenv("LLAMA_SERVER_URL", "http://localhost:8080")
LLM_MODEL_PATH = os.getenv(
    "LLLM_MODEL_PATH",
    "/home/qzx/.cache/llama.cpp/Qwen_Qwen3-8B-GGUF_Qwen3-8B-Q4_K_M.gguf"
)
EMBEDDING_MODEL_PATH = os.getenv(
    "EMBEDDING_MODEL_PATH",
    "/home/qzx/.cache/llama.cpp/Qwen_Qwen3-Embedding-0.6B-GGUF_Qwen3-Embedding-0.6B-Q8_0.gguf"
)
BRAIN_DB = os.getenv("BRAIN_DB", "./brain.db")
BRAIN_PORT = int(os.getenv("BRAIN_PORT", "8000"))
```

#### 3. `schema.sql` — 数据库 Schema

**Phase 1 先建表，不加向量索引**（Phase 2 再加）。

```sql
-- 游戏局记录（Phase 1：每 2 秒一条）
CREATE TABLE IF NOT EXISTS game_rounds (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    state_json  TEXT    NOT NULL,
    created_at  DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

#### 4. `db.py` — 数据库管理

```python
import sqlite3
from pathlib import Path
import schema_sql  # 读取 schema.sql 内容

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

#### 5. `llm.py` — llama-server 调用封装（核心）

**这是最关键的文件**，封装对 llama-server 的 HTTP 调用。

**llama-server 的 HTTP API：**

```python
# 推理接口（Completion）
POST http://localhost:8080/completion
{
  "prompt": "...",
  "n_predict": 512,
  "temperature": 0.7,
  "stop": ["</s>", "USER:", "ASSISTANT:"]
}

# Embedding 接口
POST http://localhost:8080/embedding
{
  "content": "text to embed"
}
```

**要求：**
- `LLMClient` 类封装 `/completion` 接口
- `EmbeddingClient` 类封装 `/embedding` 接口（Phase 2 用，Phase 1 先写好框架）
- 超时时间：`httpx` timeout=60 秒
- 推理用 Qwen3 的 chat template 格式：

```python
# Qwen3 chat template 格式（无 thinking mode，Phase 1 用）
<|im_start|>system
你是一个杀戮尖塔(Slay the Spire)的游戏助手，...
<|im_end|>
<|im_start|>user
当前游戏状态：{game_state_json}
请给出出牌建议。
<|im_end|>
<|im_start|>assistant
```

**Prompt 模板（Phase 1）：**

```python
SYSTEM_PROMPT = """你是一个杀戮尖塔(Slay the Spire)的游戏助手。
根据当前游戏状态，给出最优的出牌建议。

当前支持的游戏信息包括：
- 玩家HP/最大HP/护甲/金币
- 当前能量（当前/最大）
- 手牌列表（卡名、费用、类型）
- 敌人列表（名称、HP、意图）
- 当前战斗阶段

请按以下格式回复：
建议：<简要建议>
理由：<解释为什么这样出牌>
"""

def build_prompt(game_state_json: str) -> str:
    return f"""当前游戏状态：
{game_state_json}

请给出出牌建议："""
```

#### 6. `app.py` — FastAPI 入口

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
    """接收游戏状态，返回出牌建议"""
    # 1. 序列化 game_state
    import json
    state_json = json.dumps(req.game_state, ensure_ascii=False)

    # 2. 调用 LLM
    suggestion_text = llm.get_suggestion(state_json)

    # 3. 简单解析：假设返回格式是 "建议：... 理由：..."
    suggestion = ""
    reasoning = ""
    if "建议：" in suggestion_text:
        parts = suggestion_text.split("理由：")
        suggestion = parts[0].replace("建议：", "").strip()
        reasoning = parts[1].strip() if len(parts) > 1 else ""
    else:
        suggestion = suggestion_text.strip()
        reasoning = ""

    # 4. 存入数据库（Phase 1 先存，Phase 2 再加向量）
    db.insert_round(state_json)

    return AdviceResponse(suggestion=suggestion, reasoning=reasoning)

@app.post("/feedback")
async def post_feedback(req: FeedbackRequest):
    """Phase 1 简单版：只记录，不做反馈收集（Phase 2 再加）"""
    return {"recorded": True}

@app.get("/health")
async def health():
    return {"status": "ok", "llama_server": "connected"}
```

**启动方式：**
```bash
cd /repo/GameAssistance/GameAssistant/python/brain
pip install -r requirements.txt
uvicorn app:app --host 0.0.0.0 --port 8000
```

### 验收标准（T1）

1. `uvicorn app:app --port 8000` 能正常启动（不报 ImportError / 语法错误）
2. `curl http://localhost:8000/health` 返回 `{"status": "ok"}`
3. 手动 POST 测试：
   ```bash
   curl -X POST http://localhost:8000/advice \
     -H "Content-Type: application/json" \
     -d '{"game_state": {"GameName":"SlayTheSpire","Floor":3,"Act":1,"Player":{"CurrentHp":65,"MaxHp":80,"Gold":25,"Block":0},"Energy":[3,3],"Hand":[{"Name":"Strike","Cost":1,"Type":"Attack"},{"Name":"Defend","Cost":1,"Type":"Skill"},{"Name":"Bash","Cost":2,"Type":"Attack"}],"Enemies":[{"Name":"Slime B.","CurrentHp":28,"MaxHp":28,"Intent":"Attack","IntentDamage":8}],"Phase":"Combat"}}'
   ```
   能返回非空的 `suggestion` 和 `reasoning`
4. 数据库 `game_rounds` 表有数据写入

### 注意事项

- **不要在 Phase 1 加 sqlite-vss**，向量功能留到 Phase 2
- **不要加反馈收集**，Phase 1 的 `/feedback` 只是占位
- **llm.py 的 embedding 部分先写框架**，方法名留好，Phase 2 再实现
- 代码风格：类型提示要写，async/await 要用，`httpx` 代替 `requests`
- 所有路径用 `config.py` 里的常量，不硬编码

---

## T2：.NET AdviceClient + Worker 改造

### 工作目录
```
/repo/GameAssistance/GameAssistant/src/csharp/
```

### 上下文文件（必读）
- `/repo/GameAssistance/docs/REQUIRMENTS.md`
- `/repo/GameAssistance/GameAssistant/src/csharp/Background/Worker.cs` — 现有 Worker
- `/repo/GameAssistance/GameAssistant/src/csharp/Core/Models/SlayTheSpireGameState.cs` — 数据模型
- `/repo/GameAssistance/GameAssistant/src/csharp/Background/GameAssistant.Worker.csproj`

### 任务描述

改造现有 Worker，使其在每轮截屏解析后调用 Python Brain 的 `/advice` 接口。

### 详细要求

#### 1. 新增 `Infrastructure/AI/AdviceClient.cs`

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
    // SlayTheSpireGameState 的扁平化版本，只传必要字段
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
            _logger.LogWarning("Python Brain HTTP 调用失败: {Message}", ex.Message);
            return null;
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Python Brain 请求超时: {Message}", ex.Message);
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

#### 2. 改造 `Background/Worker.cs`

改造点：
1. 注册 `IAdviceClient`（或直接 `AdviceClient`）到 DI 容器
2. 在主循环每轮解析完成后，调用 `adviceClient.GetAdviceAsync(gameState)`
3. 将建议输出到日志（`LogInformation`）

```csharp
// 在 ExecuteAsync 的主循环中，每次解析完 gameState 后：
var advice = await _adviceClient.GetAdviceAsync(gameState);
if (advice != null)
{
    _logger.LogInformation("💡 出牌建议: {Suggestion} | 理由: {Reasoning}",
        advice.Suggestion, advice.Reasoning);
}
```

**注意：**
- `AdviceClient` 注册为 **Singleton**，跨循环复用同一个实例
- HTTP 调用失败不影响主循环（只 log warning，继续下一轮）
- 建议输出用 `LogInformation` 而不是 `LogDebug`（方便调试）

### 验收标准（T2）

1. `dotnet build` 通过，无编译错误
2. Worker 运行时，`LogInformation` 能看到 HTTP 请求的发出（确认调用链连通）
3. Python Brain 不可用时，Worker 不崩溃，只是 log warning

---

## 并行执行说明

- T1 和 T2 可以并行开发，互不依赖
- T1 先完成，T2 才能端到端联调
- 两边开发时可以先 mock 对方：Python 用 hardcoded JSON 响应，.NET 用 mock AdviceClient

---

## 质量要求

1. **类型安全**：.NET 代码必须用强类型，不允许 `dynamic` 或匿名对象
2. **错误隔离**：HTTP 调用失败不影响 Worker 主循环
3. **可配置**：所有 URL、端口、超时时间通过构造函数/配置注入，不硬编码
4. **日志**：关键路径必须打日志（收到请求、调用LLM、返回建议）
5. **无幻觉依赖**：只依赖 `requirements.txt` / `csproj` 里显式声明的包
