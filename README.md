# Suzerain: Carbon-Dioxide-Mod

A source-and-build snapshot of two Suzerain mod projects:

- **Oikonomia** — economy / budget / decree related BepInEx mod, probes and tests
- **Suzerain-JSON Injector** — JSON save/data injector GUI and its helper tools

This repository is a **distribution/reference snapshot**, not the upstream
development repository. Source comments were stripped and original docs were
not copied.

---

## English

### What is in this repository

```text
.
├─ README.md
├─ Oikonomia/
│  ├─ src/
│  │  ├─ Oikonomia/          main BepInEx plugin (Oikonomia.dll)
│  │  ├─ OI.Probe/           runtime probe / diagnostics plugin
│  │  └─ Oi.Verify/          verification helper plugin
│  ├─ tests/                 test projects for economy, budget, approvals, etc.
│  ├─ tools/                 build, packaging, checking and verification scripts
│  ├─ bin/ obj/              build outputs copied from the original projects
│  ├─ .gitignore
│  └─ .gitattributes
├─ Suzerain-JSON-Injector/
│  ├─ src/                   injector source: core, bridges, GUI pages
│  ├─ tools/                 JS/PowerShell helper and self-check tools
│  ├─ bin/                   trainer executables
│  ├─ SuzerainGUI*.exe       prebuilt injector GUI executables
│  ├─ .gitignore
│  └─ .gitattributes
└─ _shared/
   └─ Suzerain.PatchBus/     shared Harmony patch bus used by OI.Probe
```

### Oikonomia

The main project is `Oikonomia/src/Oikonomia/`.

- `Oikonomia.dll` is the BepInEx plugin.
- `OI.Probe` is a diagnostic / probe plugin; it observes game methods and
  economy values without taking over normal gameplay.
- `Oi.Verify` contains verification helpers.
- `tests/` contains standalone test projects for the economy and budget logic.
- `tools/` contains build, deploy, release, log-watching and verification
  scripts.
- `bin/` and `obj/` are included build artifacts. Restore caches under
  `obj/packages` were intentionally excluded.

Typical build:

```powershell
dotnet build Oikonomia/src/Oikonomia/Oikonomia.csproj -c Release
```

The default game path is set inside the project file. Override it with:

```powershell
-p:GameDir="D:\path\to\Suzerain"
```

### Suzerain-JSON Injector

This is the source and prebuilt form of the JSON injector / trainer GUI.

- `src/SuzerainGUI.*.cs` contains the GUI, core logic and bridge integrations.
- `src/SuzerainTrainer.cs` and `TranslateDict.cs` contain the trainer logic and
  translation dictionary.
- `tools/` contains JavaScript and PowerShell helpers used for self-checks,
  bridge contracts, path checks and test fixtures.
- `bin/SuzerainTrainer.exe` and the root `SuzerainGUI*.exe` files are prebuilt
  executables.
- The original standalone project file was not present in the source snapshot;
  use the included executables if you only need to run the tool.

### _shared/Suzerain.PatchBus

Small shared library used by `Oikonomia/src/OI.Probe`.

- `PatchBus.cs` provides the patch bus implementation.
- `Suzerain.PatchBus.csproj` is the project file.
- The OI.Probe project references this directory.

### What is intentionally excluded

- original `docs/` directories and documentation files;
- NuGet restore caches under `obj/packages`;
- temporary `_out/`, `_staging/` and brainstorming directories;
- game saves, credentials and personal data;
- source comments (C#, PowerShell, JavaScript, batch and project files).

### Notes

- `bin/obj` files are copied as-is and may contain absolute paths from the
  original build machine.
- The repository is a snapshot for reference and distribution; it is not an
  official release channel.
- No game files are redistributed here.

---

## 中文

### 这个仓库里有什么

```text
.
├─ README.md
├─ Oikonomia/
│  ├─ src/
│  │  ├─ Oikonomia/          主 BepInEx 插件（Oikonomia.dll）
│  │  ├─ OI.Probe/           运行时探针 / 诊断插件
│  │  └─ Oi.Verify/          验证辅助插件
│  ├─ tests/                 经济、预算、审批等逻辑的测试工程
│  ├─ tools/                 构建、打包、检查、日志与验证脚本
│  ├─ bin/ obj/              从原工程复制的构建产物
│  ├─ .gitignore
│  └─ .gitattributes
├─ Suzerain-JSON-Injector/
│  ├─ src/                   修改器源码：核心、桥接、GUI 页面
│  ├─ tools/                 JS / PowerShell 辅助与自检工具
│  ├─ bin/                   trainer 可执行文件
│  ├─ SuzerainGUI*.exe       预编译修改器 GUI
│  ├─ .gitignore
│  └─ .gitattributes
└─ _shared/
   └─ Suzerain.PatchBus/     OI.Probe 使用的共享 Harmony PatchBus
```

### Oikonomia

主工程在 `Oikonomia/src/Oikonomia/`。

- `Oikonomia.dll` 是 BepInEx 插件本体；
- `OI.Probe` 是运行时探针/诊断插件，只观察游戏方法和经济数值，不接管正常玩法；
- `Oi.Verify` 是验证辅助工程；
- `tests/` 是经济、预算等逻辑的独立测试工程；
- `tools/` 包含构建、部署、打包、日志观察和验证脚本；
- `bin/`、`obj/` 是构建产物；`obj/packages` 里的 NuGet 还原缓存已排除。

常用构建命令：

```powershell
dotnet build Oikonomia/src/Oikonomia/Oikonomia.csproj -c Release
```

工程文件里写了默认游戏路径，路径不同可以覆盖：

```powershell
-p:GameDir="D:\你的\Suzerain\路径"
```

### Suzerain-JSON Injector

这是 JSON 修改器 / trainer GUI 的源码与预编译版本。

- `src/SuzerainGUI.*.cs` 是 GUI、核心逻辑和桥接集成；
- `src/SuzerainTrainer.cs`、`TranslateDict.cs` 是修改器逻辑与翻译词典；
- `tools/` 是自检、桥协议、路径检查和测试夹具用的 JS / PowerShell 工具；
- `bin/SuzerainTrainer.exe` 和根目录的 `SuzerainGUI*.exe` 是预编译程序；
- 原工程的独立项目文件没有出现在源码快照中；只需要运行工具时直接用 exe。

### _shared/Suzerain.PatchBus

`Oikonomia/src/OI.Probe` 使用的小型共享库。

- `PatchBus.cs` 是 PatchBus 实现；
- `Suzerain.PatchBus.csproj` 是工程文件；
- OI.Probe 通过项目引用使用它。

### 有意排除的内容

- 原工程的 `docs/` 目录和文档文件；
- `obj/packages` 下的 NuGet 还原缓存；
- 临时 `_out/`、`_staging/`、头脑风暴目录；
- 游戏存档、凭据和个人数据；
- C#、PowerShell、JavaScript、批处理和工程文件中的注释。

### 说明

- `bin/obj` 是原样复制，可能保留原构建机器的绝对路径；
- 本仓库是参考/分发快照，不是官方发布渠道；
- 仓库不包含游戏本体文件。
