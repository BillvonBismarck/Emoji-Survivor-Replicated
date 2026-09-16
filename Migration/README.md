# Emoji Survivor — Unity / 原 Lua 复现

## 启动

- Unity 6000.5.5f1，打开 `Assets/Scenes/EmojiSurvivor.unity`，点击 Play。
- Windows 导出入口：`Builds/Windows/EmojiSurvivor.exe`（须保留同目录数据与 DLL）。
- WASD / 方向键移动；鼠标点击菜单、技能卡与按钮，拖动虚拟摇杆移动。攻击按原 Lua 自动执行。昵称输入后回车确认。
- 存档在 `Application.persistentDataPath/EmojiSurvivorLua`；与 UrhoX 原存档分开。

## 实现

原项目：`C:\Test\Emoji_Survivor`。46 个 Lua 文件原样放入 `Assets/StreamingAssets/Emoji/scripts`，没有重写数值、角色、技能或 Boss 行为。

`Assets/StreamingAssets/Emoji/unity_host.lua` 适配原引擎接口；`Assets/EmojiBridge/Runtime/LuaRuntime.cs` 承载原生 Lua 5.4.8；`EmojiGame.cs` 接入 Unity 输入、音频、存档与生命周期；`VectorSurface.cs` 使用 UI Toolkit 网格和文字重现 NanoVG 绘制。

字体采用原作 zpix、UrhoX MiSans 和 Twemoji.Mozilla。`EmojiUnity.ttf` 从本机 Twemoji 字体生成，保留原图形，增加 2,296 个组合序列的私用区映射，避免职业/性别 Emoji 被拆开。18 个 OGG 音频来自原作。

## 验证记录

- `Validation/source-integrity.json`：46 个 Lua（1,161,941 字节）和 18 个音频逐字节一致，全部 Lua 5.4 语法检查通过。
- `Validation/build.json`：Windows x64 Mono 构建成功，147.3 MB，0 错误、491 警告。警告含 AI Inference 着色器变体、TextCore 旧接口，以及构建时的未编译编辑器代码提示；完整内容保存在报告中。
- `Validation/player.log`：独立程序已启动并执行原 Lua 入口，完成本地存档、MockCloud 与图鉴加载。此检查不等于独立程序全流程人工游玩。
- `Validation/acceptance.json`：68/68 项通过。包括 6 角色、37 技能、14 Boss、升级选择、暂停、存档恢复、遗物、图腾、符文、12 皮肤和死亡结算。
- Boss 测试为每个约 6 秒的模拟运行，技能每项约 1.5 秒；不代表每个 Boss 的完整击败流程或长时间性能测试。
- `Captures/Final/manifest.json` 与 21 张 PNG：实际 Game View 帧缓冲截图，包含标题、排行榜、角色、成长系统、战斗、升级、暂停、Boss 和结算。技能选择和组合人物 Emoji 已人工查看截图。
- 键盘移动采用 Unity Input System 注入验证；此项不是人工键鼠游玩证明。
- Editor 菜单 `Emoji Survivor/Validate UI and Capture` 可重跑截图。测试临时用内存文件系统，结束后必须退出再进入 Play 恢复正常存档行为。

## 当前边界

- 当前原生运行库只提供 Windows x64；未提供 WebGL、移动端或 macOS 原生库。
- 排行榜沿用原作 `MockCloud` 本地模拟；广告沿用无 SDK 时的本地奖励分支，没有连接真实广告或在线排行榜服务。
- 复用全部原 Lua 不等于已经证明逐像素一致。Unity 字体栅格化、矢量边缘、文字旋转与边缘裁剪仍可能和 UrhoX 有差异；未完成双端同存档逐帧对照。
- Unity 自带 AI 包可能报告 `NoSubscription`，与游戏桥接层无关。

## 维护 / 重建

验证脚本见 `Tools/acceptance.lua`、`Tools/verify_source.py`。后者只需 Python 标准库与当前 Windows DLL。

Lua 原始发行源码保留在 `Vendor/lua-5.4.8`。DLL 用 x64 MSVC 编译其 `src/*.c`（排除 `lua.c`、`luac.c`）：`cl /O2 /MD /LD /DLUA_BUILD_AS_DLL /DLUA_COMPAT_5_3 ... /link /OUT:emoji_lua54.dll`。需 MSVC include/lib 和 Windows SDK。生成物放入 `Assets/Plugins/x86_64`。

`Tools/prepare_emoji_font.py` 用 fontTools 从本机 UrhoX 字体重建映射与派生字体。脚本中的 UrhoX 版本路径需要与本机一致。

Lua 官方源码：https://www.lua.org/ftp/lua-5.4.8.tar.gz；下载 SHA-256：`4f18ddae154e793e46eeab727c59ef1c0c0c2b744e7b94219710d76f530629ae`。以保存的源包实际哈希为准。

## 资源归属

游戏 Lua、音频和原字体保留原资源归属。本项目中的资源复制用于用户要求的本机复现；没有创建新的商业授权。Twemoji 派生字体仅增加字符映射。Lua 许可证见 `THIRD_PARTY_NOTICES.txt`，完整上游说明保留于 Vendor。
