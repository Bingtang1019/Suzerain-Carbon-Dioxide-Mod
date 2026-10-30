# Suzerain: Carbon-Dioxide-Mod

Clean source snapshot of two Suzerain mod projects:

- **Oikonomia** — economy / budget related mod and probe projects
- **Suzerain-JSON Injector** — JSON save/data injector GUI and tools

> This is a cleaned copy, not the upstream development repository.
> Docs and code comments were removed deliberately.

---

## English

### Contents

```text
Oikonomia/                  Oikonomia mod source, tests, tools, bin/obj
Suzerain-JSON-Injector/     Injector source, tools, prebuilt exe, bin
_shared/Suzerain.PatchBus/  shared PatchBus project used by OI.Probe
```

Included:

- `bin/` and `obj/` build outputs from the original projects
- prebuilt `.exe` files from the injector project
- source files with comments stripped
- bilingual README

Excluded:

- original `docs/` and documentation files
- NuGet restore caches under `obj/packages`
- temporary `_out/`, `_staging/` folders
- save files and game data

### Build

Oikonomia targets **.NET 6** and expects a Suzerain + BepInEx installation:

```powershell
dotnet build Oikonomia/src/Oikonomia/Oikonomia.csproj -c Release
```

The project properties define a default game path:

```text
D:\Program Files (x86)\Steam\steamapps\common\Suzerain
```

Override it with `-p:GameDir=<path>` if needed.

The injector folder contains source plus prebuilt executables. The original
standalone project file was not present in the source snapshot, so the
executables are the easiest way to run it.

### Notes

- Comments were stripped from C#, PowerShell, JavaScript, batch and project files.
- Build outputs are included as-is; they may contain absolute paths from the
  original build machine.
- No game saves, credentials or personal data are included.
- This repository is a distribution/reference snapshot, not an official release.

---

## 中文

### 内容

```text
Oikonomia/                  Oikonomia 模组源码、测试、工具、bin/obj
Suzerain-JSON-Injector/     修改器源码、工具、预编译 exe、bin
_shared/Suzerain.PatchBus/  OI.Probe 使用的共享 PatchBus 工程
```

包含：

- 原工程的 `bin/`、`obj/` 构建产物
- 修改器的预编译 `.exe`
- 已删除注释的源码
- 中英双语 README

不包含：

- 原工程的 `docs/` 和文档文件
- `obj/packages` 下的 NuGet 还原缓存
- 临时 `_out/`、`_staging/` 目录
- 存档和游戏数据

### 构建

Oikonomia 目标框架是 **.NET 6**，需要 Suzerain + BepInEx 环境：

```powershell
dotnet build Oikonomia/src/Oikonomia/Oikonomia.csproj -c Release
```

工程里默认游戏路径是：

```text
D:\Program Files (x86)\Steam\steamapps\common\Suzerain
```

如果路径不同，用 `-p:GameDir=<路径>` 覆盖。

修改器目录包含源码和预编译 exe。原工程的独立项目文件没有出现在源码快照里，
所以优先直接运行 exe。

### 说明

- 已清除 C#、PowerShell、JavaScript、批处理和工程文件中的注释。
- `bin/obj` 是原样复制，可能包含原构建机器的绝对路径。
- 不包含存档、凭据或个人信息。
- 本仓库是分发/参考快照，不是官方发布仓库。
