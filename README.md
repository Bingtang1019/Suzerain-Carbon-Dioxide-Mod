# Suzerain: Carbon-Dioxide-Mod

A source-and-build snapshot of two Suzerain projects:

- **Oikonomia** — an in-game BepInEx gameplay mod (economy rules + decree changes)
- **Suzerain-JSON Injector** — an external save editor / trainer GUI and CLI

This is a distribution/reference snapshot, not the upstream development
repository. Source comments were stripped and the original docs were not
copied. This README is the repository guide.

---

## English

## What these projects actually do

### Oikonomia — in-game gameplay mod

Oikonomia is a **BepInEx 6 (IL2CPP) plugin** for Suzerain. It changes rules
through the game's own variable system; it does not replace game files.

**1. Dynamic presidential decree limit (administrative capacity)**

Vanilla limits a run to **3 decrees**. Since 2.4.0 Oikonomia no longer just
raises that number to a fixed value: the limit is calculated from the
government's administrative capacity when the panel opens, then cached for
that panel session.

```text
limit = base 4
      + 1 per ministry with an increased budget          (up to +4)
      + 2 when all four ministries are funded            (+2)
      + 1 when the economy is Growing (from turn 8)
      + 1 when the constitution reform passed with 225 votes
      + 2 during a state of emergency
      - 2 when any major crisis is active
      - 1 while the Corruption situation is active
      - 1 while nationwide peaceful protests are active  (from turn 8)
      - 2 while the nationwide armed revolt is active    (from turn 8)
      floor 2, no upper limit
```

The exact formula, thresholds and even the Lua variable names are configurable
under `[Decrees]` / `[Decrees.Vars]`. Set `[Decrees] DynamicLimit = false` to
fall back to the fixed `[General] MaxDecrees` value (default 500).

It also applies to the parliamentary acts panel, with an optional exclusion
list, and hides the now-meaningless page dots and the hard-coded “you may
choose 3” subtitle. Config changes hot-reload in about one second.

**2. Adds end-of-turn economy settlements**

Every turn, Oikonomia adjusts the government budget from the current economy
state and situation tags:

| Mechanism | Effect |
|---|---|
| Economy indicator (ED) | Dangerous **−2**, Unstable **−1**, Stable **0**, Growing **+1** |
| Regional situation tiers | Each region tier up/down gives **±1** (two tiers = ±2) |
| Tourism | Booming **+1**, Average **0**, Severe recession **−1** |
| Tax | Tax efficiency **+1 per turn**; tax avoidance turning on gives a one-time **−1**; tax evasion has no penalty |
| Growth situations | Each growth-related situation turning on gives **+1 once** |

It refreshes the budget HUD after writing, and rebuilds its baseline after
loading a save/new campaign so tier changes are not double-counted.

**3. Optional Turn 1 “both projects” feature**

There is an optional `[T1]` feature that adds a third choice to the Turn 1
infrastructure decision: fund **both** the H-3 highway and the L-1 railway.
It is disabled by default in the shipped config.

### Suzerain-JSON Injector — external save editor

This is **not an in-game mod**. It is a Windows GUI/CLI trainer that edits a
Suzerain save file and writes it back, with automatic `.bak` backups.

Main capabilities:

- Base-game resources, political spectrum, turn/vote values and custom variables;
- 370 diplomacy variables (relations, trade, alliances, sanctions, war);
- 559 character relationship/opinion values;
- Event-chain flags and presidential-decree conditions;
- Rizia DLC resources (Authority / Budget / Energy), HUD caps and 110+ modifiers;
- Browse all variables and entity fields present in the selected save;
- Search, favorites, edit history and automatic Active-save synchronization.

Typical flow: run `SuzerainGUI.exe` → choose save → edit values → **Save
changes** (automatic `.bak`) → reload the save in game.

## Repository contents

```text
.
├─ README.md
├─ Oikonomia/
│  ├─ src/
│  │  ├─ Oikonomia/          main BepInEx plugin (Oikonomia.dll)
│  │  ├─ OI.Probe/           runtime probe / diagnostics plugin
│  │  └─ Oi.Verify/          verification helper plugin
│  ├─ tests/                 economy, budget, approval and format tests
│  ├─ tools/                 build, deploy, packaging and checking scripts
│  ├─ bin/ obj/              build outputs copied from the original projects
│  ├─ .gitignore
│  └─ .gitattributes
├─ Suzerain-JSON-Injector/
│  ├─ src/                   GUI, core logic and bridge integrations
│  ├─ tools/                 JS/PowerShell self-check and contract tools
│  ├─ bin/                   trainer executables
│  ├─ SuzerainGUI*.exe       prebuilt GUI executables
│  ├─ .gitignore
│  └─ .gitattributes
└─ _shared/
   └─ Suzerain.PatchBus/     shared Harmony patch bus used by OI.Probe
```

## How to use

### Oikonomia

1. Install BepInEx 6 (IL2CPP) for Suzerain.
2. Copy `Oikonomia.dll` to `<game>\BepInEx\plugins\`.
3. Launch the game once; edit
   `<game>\BepInEx\config\com.dell.suzerain.oikonomia.cfg`.
4. Key defaults:

```ini
[General]
Enabled = true
MaxDecrees = 500          # fallback / 回退值；DynamicLimit=false 时使用

[Decrees]
DynamicLimit = true       # administrative capacity / 行政能力
LimitBase = 4
LimitFloor = 2

[Economy]
Enabled = true

[T1]
Enabled = false
```

### JSON Injector

- Run `Suzerain-JSON-Injector\SuzerainGUI.exe`, or
  `Suzerain-JSON-Injector\bin\SuzerainTrainer.exe` for the CLI.
- Save location is normally:

```text
C:\Users\<user>\AppData\LocalLow\Torpor Games\Suzerain\
```

- The editor creates a `.bak` next to the save before writing.

### Build Oikonomia

```powershell
dotnet build Oikonomia/src/Oikonomia/Oikonomia.csproj -c Release
```

Default game path in the project file:

```text
D:\Program Files (x86)\Steam\steamapps\common\Suzerain
```

Override with:

```powershell
-p:GameDir="D:\path\to\Suzerain"
```

The injector folder contains source and prebuilt executables. Its original
standalone project file was not present in the source snapshot.

## Included / excluded

Included:

- source with comments stripped;
- `bin/` and `obj/` build outputs;
- prebuilt injector executables;
- this bilingual README.

Excluded:

- original `docs/` and documentation files;
- NuGet restore caches under `obj/packages`;
- temporary `_out/`, `_staging/` folders;
- game saves, credentials and personal data.

## Notes

- `bin/obj` files are copied as-is and may contain absolute paths from the
  original build machine.
- This repository is a reference/distribution snapshot, not an official
  release channel.
- No game files are redistributed here.

---

## 中文

## 这两个项目实际是做什么的

### Oikonomia —— 游戏内玩法模组

Oikonomia 是 Suzerain 的 **BepInEx 6（IL2CPP）插件**，通过游戏自己的变量系统
改规则，不替换游戏文件。

**1. 动态总统令上限（行政能力）**

原版一局最多签 **3** 个总统令。自 2.4.0 起，Oikonomia 不是简单把上限改成
固定值，而是按**政府行政能力**在面板打开时计算一次并缓存：

```text
上限 = 基数 4
     + 每增加一个部门的预算 ×1            （最多 +4）
     + 四个部门预算全部增加                （额外 +2）
     + T8 起经济状态为「增长」             （+1）
     + 宪法改革以 225 票通过议会           （+1）
     + 开启紧急状态                       （+2）
     − 触发任一重大危机                   （−2）
     − 存在「腐败」词条                   （−1）
     − T8 起仍有全国和平抗议              （−1）
     − T8 起仍有全国武装起义              （−2）
     下限 2，上限不限
```

公式、门槛和每一项的 Lua 变量名都在 cfg 的 `[Decrees]` / `[Decrees.Vars]`
里，可热改。把 `[Decrees] DynamicLimit = false` 才会退回固定的
`[General] MaxDecrees`（默认 500）。

它同样作用于法案面板，可配置排除；并隐藏原版底部“上限数量”的页码点位和
写死的“最多选择 3 个”副标题。改配置约 1 秒热重载。

**2. 每回合经济结算**

每回合结束时，按当前经济状态和词条自动调整政府预算：

| 机制 | 效果 |
|---|---|
| 经济指标 ED | 危险 **−2**、不稳定 **−1**、稳定 **0**、增长 **+1** |
| 地区三档词条 | 每升/降一档 **±1**（跨两档 ±2） |
| 旅游业 | 蓬勃发展 **+1**、平均 **0**、严重衰退 **−1** |
| 税务 | 税务效率每回合 **+1**；避税由假变真一次性 **−1**；逃税不惩罚 |
| 增长类词条 | 每个增长词条由假变真各 **+1**，只触发一次 |

写入后会刷新预算 HUD；读档/新战役后会重建基线，避免跨存档误判升降。

**3. 可选的 Turn 1“两个项目同时开工”**

`[T1]` 是可选功能：在第一回合基础设施决策里增加第三项，让 H-3 公路和 L-1 高铁
**同时开工**。发行配置里默认关闭。

### Suzerain-JSON Injector —— 外部存档修改器

它**不是游戏内模组**，而是一个 Windows GUI/CLI 修改器：读取 Suzerain 存档、修改
数值后写回，写入前自动备份 `.bak`。

主要能力：

- 本体资源、政治光谱、回合/投票值、自定义变量；
- 370 项外交变量（国家关系、贸易、联盟、制裁、战争）；
- 559 项人物好感/舆论；
- 事件链开关、总统令条件；
- Rizia DLC 三大资源（权威/预算/能源）、HUD 上下限与 110+ 修正项；
- 浏览当前存档里真实存在的全部变量和实体字段；
- 搜索、收藏、修改历史、Active 存档自动同步。

典型流程：运行 `SuzerainGUI.exe` → 选存档 → 改数值 → 点“保存修改”（自动 `.bak`）
→ 回游戏重新读档。

## 仓库内容

```text
.
├─ README.md
├─ Oikonomia/
│  ├─ src/
│  │  ├─ Oikonomia/          主 BepInEx 插件（Oikonomia.dll）
│  │  ├─ OI.Probe/           运行时探针 / 诊断插件
│  │  └─ Oi.Verify/          验证辅助插件
│  ├─ tests/                 经济、预算、审批、格式测试
│  ├─ tools/                 构建、部署、打包、检查脚本
│  ├─ bin/ obj/              从原工程复制的构建产物
│  ├─ .gitignore
│  └─ .gitattributes
├─ Suzerain-JSON-Injector/
│  ├─ src/                   GUI、核心逻辑与桥接
│  ├─ tools/                 JS/PowerShell 自检与契约工具
│  ├─ bin/                   trainer 可执行文件
│  ├─ SuzerainGUI*.exe       预编译 GUI
│  ├─ .gitignore
│  └─ .gitattributes
└─ _shared/
   └─ Suzerain.PatchBus/     OI.Probe 使用的共享 Harmony PatchBus
```

## 怎么用

### Oikonomia

1. 给 Suzerain 安装 BepInEx 6（IL2CPP）；
2. 把 `Oikonomia.dll` 放进 `<游戏目录>\BepInEx\plugins\`；
3. 启动一次游戏，编辑
   `<游戏目录>\BepInEx\config\com.dell.suzerain.oikonomia.cfg`；
4. 关键默认值：

```ini
[General]
Enabled = true
MaxDecrees = 500          # fallback / 回退值；DynamicLimit=false 时使用

[Decrees]
DynamicLimit = true       # administrative capacity / 行政能力
LimitBase = 4
LimitFloor = 2

[Economy]
Enabled = true

[T1]
Enabled = false
```

### JSON Injector

- 运行 `Suzerain-JSON-Injector\SuzerainGUI.exe`；
- 或用命令行版 `Suzerain-JSON-Injector\bin\SuzerainTrainer.exe`；
- 存档通常在：

```text
C:\Users\<用户名>\AppData\LocalLow\Torpor Games\Suzerain\
```

- 写入前会自动生成同名 `.bak`。

### 构建 Oikonomia

```powershell
dotnet build Oikonomia/src/Oikonomia/Oikonomia.csproj -c Release
```

工程默认游戏路径：

```text
D:\Program Files (x86)\Steam\steamapps\common\Suzerain
```

路径不同用：

```powershell
-p:GameDir="D:\你的\Suzerain\路径"
```

修改器目录是源码 + 预编译 exe；原工程的独立项目文件不在源码快照里。

## 包含与排除

包含：

- 已删注释的源码；
- `bin/`、`obj/` 构建产物；
- 修改器预编译 exe；
- 本双语 README。

不包含：

- 原工程 `docs/` 和文档文件；
- `obj/packages` 下的 NuGet 还原缓存；
- 临时 `_out/`、`_staging/` 目录；
- 游戏存档、凭据和个人数据。

## 说明

- `bin/obj` 是原样复制，可能保留原构建机器的绝对路径；
- 本仓库是参考/分发快照，不是官方发布渠道；
- 仓库不包含游戏本体文件。
