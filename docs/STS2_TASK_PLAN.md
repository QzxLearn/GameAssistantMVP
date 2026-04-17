# STS2 模块化整合任务规划书

> 编写日期：2026-04-16
> 更新日期：2026-04-17
> 负责人：小棠 🐱🍡
> 项目：GameAssistance
> 目标：基于 STS2 反编译源码（`/repo/sts2/`，577 张卡牌，3305 个 .cs 文件），重构游戏状态数据模型，完善感知层 + Brain 层全链路

---

## 一、现有项目状态

> ⚠️ **最后更新：2026-04-17** — 标注基于 `scripts/verify_sts2_data.py` 实际运行结果

### 1.1 ✅ 已完成

| 模块 | 状态 | 说明 |
|------|------|------|
| STS2 枚举层提取 | ✅ | `Core/Enums/Sts2Enums.cs`（8个枚举与源码完全对齐，verify 8/8 PASS） |
| 卡牌原型数据库 | ✅ | `Data/sts2_cards.json`（577张卡牌，verify PASS） |
| 角色原型数据库 | ✅ | `Data/sts2_characters.json`（5角色，HP/Energy/Gold/起始遗物均验证通过） |
| 遗物原型数据库 | ✅ | `Data/sts2_relics.json`（290个遗物，verify PASS） |
| 充能球原型数据库 | ✅ | `Data/sts2_orbs.json`（5种Orb） |
| 验证脚本 | ✅ | `scripts/verify_sts2_data.py`（8/8 检查项全 PASS，支持 BASE 路径配置） |
| 感知层骨架 | ✅ | 截屏 → OCR → 解析主循环（Worker.cs） |
| OCR 服务 | ✅ | Tesseract + ImagePreprocessor |
| AdviceClient | ✅ | HTTP 调用 Python Brain |
| Python Brain 骨架 | ⚠️ | FastAPI + llama-server，`/advice` 接口通，但 prompt 简陋、logic 未实现 |

### 1.2 ⚠️ 待完成（下一步重点）

| 模块 | 状态 | 说明 |
|------|------|------|
| 状态解析 | ⚠️ | `SlayTheSpireGameState.cs` 仅用简化枚举，与源码不对齐，需升级为 STS2 模型（B1） |
| Brain Phase 1 完善 | ⚠️ | llm.py prompt 简陋，advice.py 逻辑未实现，C1 未真正闭环 |
| schema.sql 升级 | ⚠️ | 只有 `game_rounds`/`training_samples`，未对齐 STS2 模型（D2） |
| 数据库设计文档 | 🔴 未开始 | `docs/STS2_DB_SCHEMA.md`（D1） |
| 卡牌名称映射表 | 🔴 未开始 | `Data/sts2_card_name_map.json`（B2） |
| C# 模型定义类 | 🔴 未开始 | `CardDefinition.cs`/`CharacterDefinition.cs`/`RelicDefinition.cs`（A2-A4 仅产出 JSON，缺少 C# 模型类） |
| DbSeeder | 🔴 未开始 | `Sts2DbSeeder.cs`（D3） |
| RAG 模块 | 🔴 未开始 | `brain/rag.py`（C2） |
| 反馈闭环 | 🔴 未开始 | `brain/feedback.py`（C3） |
| 模块 E（视觉训练） | 🔴 未开始 | E1-E6 全部待启动 |

### 1.3 当前感知层的问题（OCR 困境）

**⚠️ OCR 准确率问题——Tesseract 的结构性局限**

当前 `SlayTheSpireParser.cs` 依赖 Tesseract OCR 识别卡牌文字，但准确率极差。这**不是调参问题，是架构问题**：

| 文字环境 | Tesseract 表现 | 原因 |
|---------|--------------|------|
| 扫描文档 | ✅ 优秀 | 高对比度、标准字体、无干扰 |
| 网页截图 | 🟡 尚可 | 字体较大、背景相对干净 |
| **游戏卡牌文字** | ❌ **极差** | 艺术字体、小字号、彩色背景、斜体、描边、装饰图案混在一起 |

Tesseract 基于模板匹配，天然不适合游戏 UI 艺术字。无论怎么调阈值、预处理，准确率上限都很低。

**因此，OCR 在本项目中的最终定位是：数据采集阶段的辅助工具，而非生产级感知层。**

| 阶段 | OCR 的作用 |
|------|-----------|
| 当前（未训好模型） | 主要感知手段，勉强够用（E1 采集） |
| 过渡阶段（模型训练中） | 自动标注辅助：生成初版标注，人工复核 |
| 最终阶段（模型上线后） | 删除或降级保留，视觉模型直接输出结构化 JSON |

**结论：不要花时间优化 Tesseract，应该把资源投入 E1-E5。**

### 1.4 当前 `SlayTheSpireGameState.cs` 的问题

| 问题 | 说明 |
|------|------|
| 枚举不完整 | 只有 `CardType.Attack/Skill/Power/Status/Curse`，缺少 STS2 真实枚举 |
| 缺少 Orb 系统 | Defect 核心机制，完全没有 |
| 缺少 Enchantment | STS2 新增的卡牌附魔系统 |
| 缺少 Affliction | 中毒等持续伤害机制 |
| 缺少 Epoch/Timeline | STS2 新增的轮次时间轴系统 |
| `CurrentIntent` 粗糙 | 只有一个全局 intent，实际每个敌人有独立 IntentNode |
| 卡牌识别靠 OCR | 没有建立卡牌名称 → `CardModel` 的映射表（见上方 OCR 困境） |

### 1.5 STS2 源码结构概览

```
/repo/sts2/                          # 反编译 DLL，577 卡牌 + 完整运行时
├── MegaCrit.Sts2.Core/
│   ├── Models/
│   │   ├── Cards/                  # 577 个 CardModel 子类
│   │   ├── Characters/             # 5 个 CharacterModel 子类
│   │   ├── Relics/                 # 遗物原型
│   │   ├── Monsters/               # 怪物原型
│   │   ├── Powers/                 # PowerModel 子类
│   │   ├── Orbs/                   # OrbModel 子类
│   │   ├── Enchantments/           # 附魔原型
│   │   ├── Afflictions/            # 中毒等减益原型
│   │   └── CardPools/              # 卡牌池定义
│   ├── Entities/
│   │   ├── Cards/                  # CardType/CardRarity/TargetType 等枚举
│   │   ├── Characters/            # 角色运行时
│   │   ├── Creatures/              # Creature 基类（HP/Block/Powers）
│   │   ├── Players/                # Player 类
│   │   ├── Powers/                 # PowerType/PowerStackType 枚举
│   │   ├── Orbs/                   # OrbSlot/OrbType
│   │   └── Intents/                # IntentNode 怪物意图
│   ├── Combat/                     # CombatState
│   ├── Runs/                       # RunState
│   ├── Map/                        # 地图节点
│   └── Saves/                      # 存档序列化
```

### 1.6 PCK 资源包状态

| 文件 | 大小 | 状态 |
|------|------|------|
| `SlayTheSpire2.pck` | 1.7GB | ⚠️ **已加密**（PACK_ENCRYPTED=1） |
| 主纹理/音频 | - | 明文存储，但文件路径被加密 |
| Mod .pck | - | 不加密，可正常解包 |

---

## 二、总体架构规划

### 2.1 核心决策：两层解耦

```
游戏截图 → 本地微调视觉模型 → 结构化 JSON → API+RAG 推理层 → 出牌建议
```

| 层级 | 技术选型 | 理由 |
|------|----------|------|
| **视觉理解（感知层）** | 本地微调视觉模型 | 实时性高（每2秒一帧），视觉模式固定不复杂，轻量模型足够 |
| **出牌建议（推理层）** | API + RAG | 需要全局策略/伤害计算，本地小模型 context / math 能力不足；RAG 可引入领域知识 |

---

## 三、模块划分与执行状态（v0.4）

### 模块 A：STS2 静态数据层 ✅ 主要完成

| 任务 | 状态 | 产出文件 |
|------|------|---------|
| A1. 枚举层提取 | ✅ 完成 | `Core/Enums/Sts2Enums.cs`（8枚举，verify PASS） |
| A2. 卡牌原型数据库 | ✅ 完成 | `Data/sts2_cards.json`（577张） |
| A3. 角色原型数据库 | ✅ 完成 | `Data/sts2_characters.json`（5角色） |
| A4. 遗物原型数据库 | ✅ 完成 | `Data/sts2_relics.json`（290个） |
| A5. Orb 数据库 | ✅ 完成 | `Data/sts2_orbs.json`（5种） |
| A5. Power/Affliction/Enchantment | 🔴 未开始 | — |

> ⚠️ A2-A4 **仅产出 JSON**，缺少对应的 C# 模型类（CardDefinition.cs / CharacterDefinition.cs / RelicDefinition.cs）

### 模块 B：感知层重构 ⚠️ 待完成

| 任务 | 状态 | 说明 |
|------|------|------|
| B1. 重构 `SlayTheSpireGameState.cs` | ⚠️ 待完成 | 当前用简化枚举，需升级为 STS2 模型 |
| B2. OCR 卡牌名称映射表 | 🔴 未开始 | `Data/sts2_card_name_map.json` |
| B3. 优化 Parser | 🔴 P2 低优先 | OCR 提升有限，视觉模型上线后替换 |
| B4. 完善 CaptureRegions | 🔴 未开始 | P2 低优先 |

### 模块 C：Brain 层（API + RAG）

| 任务 | 状态 | 说明 |
|------|------|------|
| C1. Phase 1 闭环完善 | ⚠️ 骨架通，logic 未实现 | FastAPI 骨架 /advice 通，但 prompt 简陋 |
| C2. RAG 模块 | 🔴 未开始 | `brain/rag.py` |
| C3. 反馈闭环 | 🔴 未开始 | `brain/feedback.py` |
| C4. 训练数据导出 | 🔴 未开始 | `brain/export.py` |
| C5. 策略知识库建设 | 🔴 未开始 | — |

### 模块 D：数据库层

| 任务 | 状态 | 说明 |
|------|------|------|
| D1. 设计文档 | 🔴 未开始 | `docs/STS2_DB_SCHEMA.md` |
| D2. schema.sql 升级 | ⚠️ 基础表存在 | 需对齐 STS2 模型 |
| D3. DbSeeder | 🔴 未开始 | — |

### 模块 E：训练数据集 🔴 全部未开始

> ⚠️ 重要前置：E2 标注规范需 B1 + B2 完成才能启动

---

## 四、当前阶段与下一步行动

### 4.1 当前阶段判定

> ⚠️ **当前处于：第一波 → 第二波过渡阶段**
>
> ✅ 已完成：A1 枚举、A2 卡牌 JSON、A3 角色 JSON、A4 遗物 JSON、A5 Orb JSON、验证脚本
> ⚠️ 当前瓶颈：C1 Brain Phase1 完善、B1 GameState 重构、D1/D2 数据库设计
> 🔴 未开始：D3、C2-C5、E1-E6

### 4.2 推荐下一步（可并行）

**第一优先（解开 B/C/D 瓶颈）：**
1. **D1** — `docs/STS2_DB_SCHEMA.md` 数据库设计（无依赖，最干净切入点）
2. **C1** — Brain Phase1 完善（补完 prompt + advice 逻辑，让 /advice 真正闭环）
3. **B1** — `SlayTheSpireGameState.cs` 重构（对齐 Sts2Enums，加入 Orb/IntentNodes 等）

**次优先（依赖上述完成后）：**
4. **D2** — schema.sql 升级（依赖 D1）
5. **B2** — 卡牌名称映射表（依赖 A2）
6. **D3** — DbSeeder（依赖 D2）

---

## 五、任务清单总表

### 模块 A：STS2 静态数据层

| 任务 | 状态 | 优先级 | 产出文件 |
|------|------|--------|---------|
| A1. 枚举层提取 | ✅ 完成 | P0 | `Core/Enums/Sts2Enums.cs` |
| A2. 卡牌原型数据库 | ✅ 完成（缺 C# 类） | P0 | `Data/sts2_cards.json` |
| A3. 角色原型数据库 | ✅ 完成（缺 C# 类） | P0 | `Data/sts2_characters.json` |
| A4. 遗物原型数据库 | ✅ 完成（缺 C# 类） | P1 | `Data/sts2_relics.json` |
| A5. Orb 数据库 | ✅ 完成 | P1 | `Data/sts2_orbs.json` |
| A5. Power/Affliction/Enchantment | 🔴 未开始 | P1 | — |

### 模块 B：感知层重构

| 任务 | 状态 | 优先级 | 产出文件 |
|------|------|--------|---------|
| B1. 重构 `SlayTheSpireGameState.cs` | ⚠️ 待完成 | P0 | `Core/Models/Sts2/` |
| B2. OCR 卡牌名称映射表 | 🔴 未开始 | P0 | `Data/sts2_card_name_map.json` |
| B3. 优化 Parser | 🔴 P2 低优先 | P2 | `Infrastructure/Ocr/` |
| B4. 完善 CaptureRegions | 🔴 未开始 | P2 | `Core/Models/SlayTheSpireCaptureRegions.cs` |

### 模块 C：Brain 层（API + RAG）

| 任务 | 状态 | 优先级 | 产出文件 |
|------|------|--------|---------|
| C1. Phase 1 闭环完善 | ⚠️ 骨架通，logic 未实现 | P0 | `brain/advice.py` |
| C2. RAG 模块 | 🔴 未开始 | P0 | `brain/rag.py` |
| C3. 反馈闭环 | 🔴 未开始 | P0 | `brain/feedback.py` |
| C4. 训练数据导出 | 🔴 未开始 | P2 | `brain/export.py` |
| C5. 策略知识库建设 | 🔴 未开始 | P2 | `brain/knowledge_base/` |

### 模块 D：数据库层

| 任务 | 状态 | 优先级 | 产出文件 |
|------|------|--------|---------|
| D1. 设计文档 | 🔴 未开始 | P0 | `docs/STS2_DB_SCHEMA.md` |
| D2. schema.sql 升级 | ⚠️ 基础表存在，需升级 | P0 | `brain/schema.sql` |
| D3. DbSeeder | 🔴 未开始 | P1 | `Infrastructure/Storage/Sts2DbSeeder.cs` |

### 模块 E：训练数据集

| 任务 | 状态 | 优先级 | 产出文件 |
|------|------|--------|---------|
| E1. 数据采集框架 | 🔴 未开始 | P0 | `dataset/raw/` + Worker.cs 扩展 |
| E2. 标注规范制定 | 🔴 未开始（需 B1+B2） | P0 | `docs/E2_标注规范.md` |
| E3. 半自动标注工具 | 🔴 未开始 | P1 | `python/annotator/` |
| E4. 格式转换 | 🔴 未开始 | P1 | `dataset/train.jsonl` 等 |
| E5. 模型微调 | 🔴 未开始 | P1 | `models/sts2_vision_finetuned/` |
| E6. 质量评估 | 🔴 未开始 | P1 | `docs/E6_评估报告.md` |

---

## 六、模块 E 展开（供参考）

### E1：数据采集框架

触发条件：每 2 秒截一帧，关键帧（战斗开始/结束/Boss/HP变化>20%）标记待复核。

### E2：标注规范制定（关键前置）

**E2 执行前必须完成：**
1. 读 `CombatState.cs` — 理解战斗中哪些信息是视觉可感的
2. 读 `Creature.cs` — HP/Block/Power 视觉表达
3. 读 `CardModel.cs` — 手牌 UI 位置/费用显示/升级标记
4. 读 `Player.cs` — 牌堆/药水/遗物 UI 区域
5. 读 `MegaCrit.Sts2.Core.Nodes/` — UI 节点结构，确定 ROI

### E5：模型训练路线

| 路线 | 模型 | 显存 | 训练时间（3000图） |
|------|------|------|---------|
| A（先验证） | Florence-2-base (~0.7B) | RTX 3060 12GB | 1-2h |
| B（数据够时） | Qwen2-VL-7B-Instruct | RTX 3090/4090 24GB | 4-6h |

详见第七节完整训练手册。

---

## 七、本地视觉模型训练手册（节选）

> 完整内容见原始文档 v0.3，此处保留目录结构。

### 7.1 技术路线对比（见上表）

### 7.2 前置条件清单
- GPU: RTX 3090/4090 24GB（路线B）
- Python ≥ 3.10, CUDA 11.8/12.x, PyTorch ≥ 2.0
- axolotl, transformers, peft
- 模型文件（Florence-2-base 或 Qwen2-VL-7B-Instruct）
- 数据集（E1-E4 产出，3000+ 张）

### 7.3-7.9 训练步骤、检查清单、问题排查

详见原始文档 v0.3（第三节：模块 E 展开说明）。

---

## 八、变更记录

| 日期 | 版本 | 变更内容 | 作者 |
|------|------|----------|------|
| 2026-04-16 | v0.1 | 初始版本 | 小棠 🐱 |
| 2026-04-16 | v0.2 | 两层架构决策、模块 E 完整规划、训练手册 | 小棠 🐱 |
| 2026-04-16 | v0.3 | OCR 困境分析、B3 降优先、第七/八节新增 | 小棠 🐱 |
| 2026-04-17 | v0.4 | 全面更新：标注所有模块完成状态，验证脚本 8/8 PASS，第二波阶段判定，下一步行动路线 | 小棠 🐱 |
