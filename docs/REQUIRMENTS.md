# GameAssistance — 自训练游戏助手需求文档

> 编写日期：2026-04-14
> 状态：进行中
> 负责人：小棠 🐱🍡
> 目标：跑通"感知层 → 推理层 → RAG → 反馈闭环"全流程，支持本地开源模型

---

## 1. 项目概述

### 1.1 背景

GameAssistance 项目目前已完成感知层框架（截屏 + OCR + 游戏状态解析），但缺乏 AI 推理能力、历史经验积累和自训练机制。本阶段目标是为项目增加：

- **本地 LLM 推理**：基于开源模型给出游戏建议
- **本地 RAG**：基于向量检索复用历史经验
- **自动反馈闭环**：战斗结果自动判定，数据积累用于后续微调

### 1.2 指导原则

1. **先跑通，再迭代**：MVP 阶段聚焦最小闭环，不追求完美
2. **纯 SQLite**：数据全部存在单一 SQLite 文件（含向量），降低复杂度，便于后续迁移云端
3. **.NET + Python 分离**：.NET 负责感知，Python 负责 Brain，边界清晰便于未来拆分部署
4. **技术债可控**：每个阶段有明确验收标准，技术选型有文档记录

### 1.3 目标游戏

当前阶段聚焦 **《杀戮尖塔》(Slay the Spire)**，后续可扩展其他卡牌游戏。

---

## 2. 技术栈

### 2.1 总体架构

| 层级 | 技术选型 | 说明 |
|------|----------|------|
| **感知层** | .NET 10 Worker Service | 已有基础框架 |
| **通信协议** | HTTP REST（JSON） | .NET → Python 的最小接口 |
| **推理引擎** | llama.cpp server (`llama-server`) | 已编译，直接使用 |
| **LLM 模型** | **Qwen3-8B-Q4_K_M.gguf** | 支持 thinking/non-thinking 双模式 |
| **Embedding 模型** | **Qwen3-Embedding-0.6B-Q8_0.gguf** | Qwen3 原生 embedding，比 nomic 更新 |
| **向量库** | sqlite-vss（SQLite 扩展） | 纯 SQLite，向量 + 业务数据同库 |
| **Python 框架** | FastAPI | Python Brain 的 HTTP 服务 |
| **业务数据库** | SQLite（单一文件） | 包含业务表 + 向量索引 |

### 2.2 模型文件

| 模型 | 文件名 | 路径 |
|------|--------|------|
| LLM | `Qwen_Qwen3-8B-GGUF_Qwen3-8B-Q4_K_M.gguf` | `/home/qzx/models/` |
| Embedding | `Qwen_Qwen3-Embedding-0.6B-GGUF_Qwen3-Embedding-0.6B-Q8_0.gguf` | `/home/qzx/models/` |

### 2.3 依赖环境

- **llama.cpp**：`llama-server` 已编译并加入 PATH，支持 `--embeddings` 参数 ✅
- **Python 3.10+**
- **.NET 10 SDK**
- **Tesseract OCR**（感知层已有）
- **sqlite-vss**：`pip install sqlite-vss`
- **FastAPI**：`pip install fastapi uvicorn httpx`

> ✅ **环境已验证**：llama-server embedding 支持已确认，两个模型均已下载并测试通过。

---

## 3. 系统架构

### 3.1 数据流全景图

```
┌──────────────────────────────────────────────────────────────────┐
│                        游戏客户端                                 │
│                  (Slay the Spire / 其他卡牌游戏)                   │
└────────────────────────────┬─────────────────────────────────────┘
                             │ 屏幕画面
                             ↓
┌──────────────────────────────────────────────────────────────────┐
│                   .NET 感知层 (Background Worker)                 │
│                                                                   │
│  ┌─────────┐   ┌─────────┐   ┌──────────┐   ┌───────────────┐ │
│  │截屏服务  │ → │OCR 识别  │ → │状态解析   │ → │GameState (JSON)│ │
│  │Capture  │   │Tesseract│   │SlayTheSpire│  │  结构化数据    │ │
│  └─────────┘   └─────────┘   └──────────┘   └───────┬───────┘ │
│                                                       │          │
│                              ┌────────────────────────│          │
│                              │ 每 2s 循环执行          │          │
└──────────────────────────────┼────────────────────────┼──────────┘
                               │ HTTP POST              │
                               ↓                        ↓
                    ┌──────────────────────┐  ┌─────────────────────┐
                    │  Python Brain       │  │   SQLite 文件       │
                    │  (FastAPI Server)   │  │  brain.db           │
                    │                     │  │  ┌───────────────┐  │
                    │  /advice  ←─────────┼──┘  │ game_rounds    │  │
                    │        ↓            │      │ training_sample│  │
                    │  ┌─────────────┐    │      │ vss_brain      │  │
                    │  │1.查RAG(历史) │    │      └───────────────┘  │
                    │  │2.调llama.cpp│    │                           │
                    │  │3.返回建议   │    │                           │
                    │  └─────────────┘    │                           │
                    │        ↓            │                           │
                    │  /feedback(战斗结束) │ ←─┐                        │
                    │        ↓            │   │自动判定胜负写入样本库  │
                    │  存入 SQLite        │ ←─┘                        │
                    └──────────────────────┘
```

### 3.2 模块职责

#### .NET 感知层（现有，改造点：调用 HTTP）

| 模块 | 职责 |
|------|------|
| `Background/Worker.cs` | 主循环（截屏→OCR→解析），每轮调用 Python HTTP 接口 |
| `Infrastructure/Ocr/TesseractOcrService.cs` | OCR 识别（已有） |
| `Infrastructure/Ocr/SlayTheSpireParser.cs` | 状态解析（已有） |
| `Core/Interfaces/IScreenCaptureService.cs` | 截屏服务接口（已有） |
| **新增：`Infrastructure/AI/AdviceClient.cs`** | HTTP 调用 Python Brain |

#### Python Brain 层（新增）

| 模块 | 职责 |
|------|------|
| `app.py` | FastAPI 入口，定义 REST 接口 |
| `llm.py` | llama-server 调用封装（completion + embedding） |
| `rag.py` | 向量检索：存入向量、相似查询 |
| `feedback.py` | 反馈收集：判断战斗结束、写入样本库 |
| `db.py` | SQLite 连接管理 + schema 初始化 |
| `schema.sql` | 数据库表结构定义 |

---

## 4. 分阶段规划

### Phase 0 — 环境验证 ✅ 已完成

**目标：** 确认所有底层工具链可用。

- [x] `llama-server --help | grep embed` — `--embeddings` 参数存在
- [x] Qwen3-8B-Q4_K_M 模型测试推理通过
- [x] Qwen3-Embedding-0.6B-Q8_0 测试 embedding 接口通过
- [x] .NET 项目 `dotnet build` 通过

**交付物：**
- `python/validate_env.py` — 环境验证脚本（可选，后续需要时补）

---

### Phase 1 — 最小闭环 MVP（进行中，预计 3~5 天）

**目标：** .NET 感知层 → Python Brain → 建议返回，全程跑通一局。

**接口定义：**

```
POST /advice
  Request:  { "game_state": { ...SlayTheSpireGameState JSON... } }
  Response: { "suggestion": "建议出牌顺序...", "reasoning": "理由..." }

POST /feedback
  Request:  { "game_state": { ... }, "suggestion": "...", "result": "win|loss" }
  Response: { "recorded": true }

GET /health
  Response: { "status": "ok", "llama_server": "connected" }
```

**验收清单：**

- [ ] 一局完整游戏中，.NET 持续向 `/advice` 发请求并收到回复
- [ ] 返回的 `suggestion` 非空，内容可读（不是乱码）
- [ ] `/feedback` 接口能正确接收战斗结果并写入 SQLite
- [ ] `game_rounds` 表有至少 1 条记录

**交付物：**
- `python/brain/app.py` — FastAPI 服务
- `python/brain/llm.py` — llama-server 封装
- `python/brain/db.py` — SQLite 初始化
- `python/brain/schema.sql` — 数据库 schema
- `python/brain/requirements.txt` — Python 依赖
- `dotnet/.../AdviceClient.cs` — HTTP 调用客户端
- `docs/PHASE1_接口文档.md` — 接口说明

**Phase 1 暂不涉及 RAG**（推理时只有当前状态，无历史参考）。

---

### Phase 2 — RAG + 反馈闭环（预计 3~5 天）

**目标：** 加入向量检索和自动反馈收集。

**新增功能：**

1. **RAG 检索**
   - 每局游戏结束时，将 `game_state_json` 用 `Qwen3-Embedding-0.6B` 向量化，存入 `vss_brain`
   - 推理时先用 embedding 查最相似的 N 条历史局，附在 prompt 里

2. **自动反馈判定**
   - 根据 `GamePhase` 变化自动判定战斗结束（`Combat → Map` 或 `CombatResult` 出现）
   - 战胜 → `feedback_score = +1`，战败 → `feedback_score = -1`

3. **样本数据结构**
   - `training_samples(id, game_state_json, suggestion, feedback_score, created_at)`

**验收清单：**

- [ ] 给定当前状态，`/advice` 能返回相似历史局（查询有结果）
- [ ] 战斗结束后反馈自动入库（无需手动调用 `/feedback`）
- [ ] `training_samples` 表有正负样本各至少 1 条

**交付物：**
- `python/brain/rag.py` — 向量存取 + 检索
- `python/brain/feedback.py` — 自动反馈判定逻辑
- `docs/PHASE2_数据字典.md` — 表结构说明

---

### Phase 3 — 微调数据准备（后续迭代）

**目标：** 整理可用于微调的训练数据集。

**验收清单：**

- [ ] 能导出 `training_samples` 为 JSONL 格式
- [ ] 导出格式符合目标微调工具要求（llama.cpp 或 Ollama 微调格式）
- [ ] 有操作文档说明如何做一次完整微调

**交付物：**
- `python/brain/export.py` — 训练数据导出脚本
- `docs/PHASE3_微调指南.md`

---

## 5. 数据库设计

### 5.1 Schema（单一 SQLite 文件）

```sql
-- 游戏局记录（每 2 秒一条）
CREATE TABLE IF NOT EXISTS game_rounds (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    state_json  TEXT    NOT NULL,   -- SlayTheSpireGameState JSON
    created_at  DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 训练样本（反馈数据）
CREATE TABLE IF NOT EXISTS training_samples (
    id               INTEGER PRIMARY KEY AUTOINCREMENT,
    game_round_id    INTEGER REFERENCES game_rounds(id),
    suggestion       TEXT    NOT NULL,   -- 模型给出的建议
    feedback_score   INTEGER NOT NULL,   -- +1 正 / -1 负
    created_at       DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- sqlite-vss 向量索引（关联 game_rounds）
-- vss_brain 由 sqlite-vss 自动管理
```

### 5.2 向量索引使用方式

```python
# 存入（每局结束时）
embeddings = get_embedding(state_json)  # Qwen3-Embedding-0.6B
db.execute("INSERT INTO game_rounds(state_json) VALUES(?)", [state_json])
row_id = db.execute("SELECT last_insert_rowid()").fetchone()[0]
db.execute("SELECT vss_brain_upsert(?)", [{"id": row_id, "embeddings": embeddings}])

# 查询（推理时）
query_emb = get_embedding(current_state_json)
results = db.execute(
    "SELECT * FROM game_rounds JOIN vss_brain ON ... WHERE vss_brain_search(?, ?)..."
)
```

---

## 6. 目录结构

```
/repo/GameAssistance/GameAssistant/
├── src/csharp/
│   ├── Background/
│   │   ├── Worker.cs               # [改造] 主循环，调用 AdviceClient
│   │   └── AdviceClient.cs        # [新增] HTTP 调用 Python Brain
│   ├── Core/                       # （现有，不动）
│   └── Infrastructure/
│       └── Ocr/                    # （现有，不动）
│
├── python/                         # [新增]
│   └── brain/
│       ├── app.py                  # FastAPI 入口
│       ├── llm.py                  # llama-server 调用封装
│       ├── rag.py                  # RAG 向量检索
│       ├── feedback.py             # 反馈收集逻辑
│       ├── db.py                   # SQLite 连接管理
│       ├── schema.sql              # 表结构
│       ├── requirements.txt        # Python 依赖
│       └── export.py               # [Phase 3] 训练数据导出
│
└── docs/
    ├── REQUIRMENTS.md              # 本文档
    ├── PHASE1_接口文档.md
    ├── PHASE2_数据字典.md
    └── PHASE3_微调指南.md
```

---

## 7. 配置参数

> 所有配置通过环境变量或 `config.py` 管理，敏感信息不硬编码。

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `LLAMA_SERVER_URL` | `http://localhost:8080` | llama-server 地址 |
| `LLM_MODEL_PATH` | `/home/qzx/models/Qwen_Qwen3-8B-GGUF_Qwen3-8B-Q4_K_M.gguf` | LLM 模型路径 |
| `EMBEDDING_MODEL_PATH` | `/home/qzx/models/Qwen_Qwen3-Embedding-0.6B-GGUF_Qwen3-Embedding-0.6B-Q8_0.gguf` | Embedding 模型路径 |
| `BRAIN_DB` | `./brain.db` | SQLite 数据库路径 |
| `BRAIN_PORT` | `8000` | FastAPI 服务端口 |

---

## 8. 变更记录

| 日期 | 版本 | 变更内容 | 作者 |
|------|------|----------|------|
| 2026-04-14 | v0.1 | 初始版本，基于 2026-04-14 架构讨论 | 小棠 🐱 |
| 2026-04-14 | v0.2 | 确认模型：Qwen3-8B-Q4_K_M（LLM）+ Qwen3-Embedding-0.6B-Q8_0（Embedding），Phase 0 完成 | 小棠 🐱 |


---

## 9. STS2 代码调研任务

> 编写日期：2026-04-16
> 状态：🔵 进行中
> 目的：通过分析 STS2 反编译源码，设计精确的游戏状态数据库 Schema

### 9.1 背景

当前系统通过 OCR 从截图识别游戏状态，存在以下问题：
- 字段不完整（仅能识别可见文字区域）
- 数据结构靠猜测，与游戏实际逻辑不对齐
- 难以精确建模卡牌/遗物/Power 之间的复杂关系

STS2 的 `sts2.dll` 已反编译至 `/repo/sts2/`（共 3305 个 .cs 文件），可直接从源码提取真实字段定义。

### 9.2 调研范围与优先级

| 模块 | 路径 | 优先级 | 目标 |
|------|------|--------|------|
| 卡牌数据模型 | `MegaCrit.Sts2.Core.Models.Cards` | P0 | 提取卡牌完整字段 |
| 遗物数据模型 | `MegaCrit.Sts2.Core.Models.Relics` | P0 | 遗物完整字段 |
| 角色数据模型 | `MegaCrit.Sts2.Core.Models.Characters` | P0 | 5个角色初始配置 |
| Power 系统 | `MegaCrit.Sts2.Core.Entities.Powers` | P0 | Buff/Debuff 字段与类型 |
| 战斗状态 | `MegaCrit.Sts2.Core.Combat` | P1 | CombatState 字段 |
| 局次状态 | `MegaCrit.Sts2.Core.Runs` | P1 | RunState 完整字段 |
| 怪物数据 | `MegaCrit.Sts2.Core.Models.Monsters` | P1 | 怪物属性+意图状态机 |
| 地图系统 | `MegaCrit.Sts2.Core.Map` | P2 | 地图节点类型与结构 |
| 存档系统 | `MegaCrit.Sts2.Core.Saves` | P2 | 序列化格式参考 |
| 附魔系统 | `MegaCrit.Sts2.Core.Models.Enchantments` | P2 | STS2 新特性 |

### 9.3 分阶段调研计划

**Phase A：静态数据层（P0）**
目标：固定属性字段 → 静态数据表

- [ ] A1：`CardModel` 基类 + 枚举（CardType/CardRarity/TargetType/CardTag）
- [ ] A2：`RelicModel` 基类 + RelicRarity
- [ ] A3：`PowerModel` 基类 + PowerType/PowerStackType
- [ ] A4：`CharacterModel`（5角色：初始HP/金币/能量/起始牌组）
- [ ] A5：`MonsterModel` + `EncounterModel`（HP范围/意图状态机）

**Phase B：运行时状态层（P1）**
目标：实时状态字段 → 状态快照表

- [ ] B1：`CombatState`（战斗快照：双方单位、回合数、当前行动方）
- [ ] B2：`RunState`（局次快照：Acts、地图坐标、玩家列表）
- [ ] B3：`Creature`（单位基类：HP/Block/Powers/Orbs/意图）
- [ ] B4：`Player`（玩家：牌库/手牌/弃牌堆/遗物/药水/金币/能量）

**Phase C：存档与序列化（P2）**
- [ ] C1：`SerializableRun` 结构
- [ ] C2：`Saves.Migrations` 版本演变

**Phase D：STS2 新特性（P2，按需）**
- [ ] D1：Epoch/Timeline 系统
- [ ] D2：Enchantment 附魔系统
- [ ] D3：Multiplayer 联机结构

### 9.4 调研方法

> 核心原则：分阶段单模块精读，每次只加载 1-3 个文件

每次调研步骤：
1. `ls` 目标目录，确认文件列表
2. `cat` 目标基类文件（优先读接口/基类/枚举）
3. 按需追踪引用的其他类（最多往下一层）
4. 提炼字段定义 → 追加到 `STS2_DB_DESIGN.md`
5. 更新本文档对应 Checkbox

### 9.5 输出物

详细数据库设计文档：`/repo/GameAssistance/docs/STS2_DB_DESIGN.md`（随调研持续迭代）

