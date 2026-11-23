# Clocking Out：AI 助手工作守则

Game Off 2026（主题 SHIFT）参赛作品。死神把你的死亡通知单丢给你自己填，你选死因、地点、班次，让这次死亡恰好完成遗愿。Unity 2022.3.17f1c1 + URP + WebGL，旧版 Input Manager，uGUI + TextMeshPro。设计以 `docs/design.md`、`docs/levels.md`、`docs/level-schema.md` 为准，不要重新选题。

## 目录与程序集
- `Assets/Scripts/Core/`：`DeathNotice.Core`，纯逻辑（LevelData、LevelLoader、Simulator、LevelValidator）。**不得引用场景对象、MonoBehaviour 或 UI。**
- `Assets/Scripts/Game/`：`DeathNotice.Game`，表现层（StageBuilder/StageLayouts 舞台、NoticeForm 表单、BeatPlayer 推演、AudioManager、GameFlow）。可以依赖 Core，Core 不能反过来依赖 Game。
- `Assets/Tests/EditMode/`：`DeathNotice.Tests.EditMode`，枚举每关全部组合，断言批准集合等于 `solutions`。
- `Assets/Editor/`：BuildScript、ProjectSetup。`tools/`：构建脚本和校验器。`docs/`：规格与素材文档。

## 关卡数据
- 关卡是 `Assets/Resources/Levels/level_XX.json`，不是 ScriptableObject。结构见 `docs/level-schema.md`（JsonUtility 不支持 Dictionary，所以 `states`/`solutions` 用数组）。
- **改了任何关卡 JSON 之后必须跑 `python tools\validate_levels.py`**，它会同时重新生成 `docs/combos.md`，不要手改那个文件。
- 规则按文件顺序就是因果顺序：先判可能性，再判打断，再结算死亡，最后算连锁。`show:` 事实只给舞台用，规则不能引用。
- 退单文案要说明原因，但不能泄露解法。

## 构建与运行
- **不能打开 Unity 图形界面。** 一律用：
  - `powershell -ExecutionPolicy Bypass -File tools\compile.ps1`（编译，日志 `Logs/compile.log`）
  - `powershell -ExecutionPolicy Bypass -File tools\test.ps1`（EditMode 测试，结果 `TestResults/EditMode.xml`）
  - `powershell -ExecutionPolicy Bypass -File tools\build-webgl.ps1`（输出 `Builds/WebGL`；路径里有中文导致 IL2CPP 报错时加 `-Subst`）
- 同一时间只能有一个 Unity 实例（脚本会检查 `Temp\UnityLockfile`），别和其他 Agent 抢着跑。
- 改完代码至少保证 compile 通过；动了 Core 或关卡就要跑 test。

## 运行时搭建
- 场景、相机、灯光、UI 全部由代码在运行时搭建（GameRoot → GameFlow → StageBuilder / UIFactory）。不要手工往 .unity 场景或 prefab 里摆东西。
- 正式模型按锚点名替换灰盒：`Assets/Resources/Models/<levelId>/<anchor>.fbx`（见 `docs/art-brief.md`）。替换时会清掉该锚点的旋转、开关、点灯回调，带动画的锚点要另行处理。
- **构建里 `Shader.Find` 会返回 null**（没被引用的 shader 会被裁掉）。材质一律通过 `Mats` 从 `Resources/Materials` 里的模板复制；新增 shader 时也得在 ProjectSetup 里生成模板材质放进 Resources，或者加到 Always Included Shaders。
- 音频按名字从 `Resources/Audio/Sfx|Bgm` 加载，缺了就静默跳过。

## 文本
- 游戏内一切文本用英文（UI、线索、字幕、退单原因）。代码注释和 docs 用中文、英文都行。
- 字体是 TMP 自带的 LiberationSans SDF，不要用它没有的字符（中文、生僻符号）。

## WebGL 限制
- 没有多线程：不要用 `System.Threading`、`Task.Run`、`Thread`，要异步就用协程。
- 不要用 `System.IO` 读资源，一律走 `Resources.Load` / `TextAsset`。
- 浏览器要求用户点击后才能出声：在第一次点击之前不要播 BGM，AudioManager 已经这样处理。
- Shift 键**只做按住检测**（`Input.GetKey`），不要设计成连按或计次：Windows 连按 5 次 Shift 会弹出粘滞键对话框，浏览器里也可能误触。
- 不能依赖右键菜单、F 键（浏览器会拦截）。体积越小越好，Development Build 保持关闭。

## 提交规范
- 不提交 `Library/`、`Temp/`、`Logs/`、`Builds/`、`UserSettings/`、`TestResults/`（`.gitignore` 已覆盖）。`.meta` 必须和资源一起提交。
- 二进制（fbx/png/wav/ogg/mp3/ttf 等）走 Git LFS，`.gitattributes` 已配置。新类型先补规则再提交。
- 提交信息写清楚改了什么，**不写任何 AI 模型名称或版本号**，作者署名只用 `vvmm0612`，不出现真实姓名或邮箱。
- 每加一份外部或生成的素材，就在 `docs/asset-ledger.csv` 登记一行。

## 范围与优先级
按顺序做，上一项没稳定之前不要碰下一项：
1. 主线 6 关可以通关（灰盒也算）
2. 推演演出、死亡证明、评价印章
3. Shift 预览
4. 音频（BGM + 事件音效）
5. 正式美术替换灰盒
6. 支线"重审"：只有前面全部完成才考虑

退路：11/28 时第 4、5 关还没完成，就砍成 4 关（1、2、3 + 终局）。**11/29 功能冻结**，之后只修 bug、调文本、换素材、做 itch 页面。
