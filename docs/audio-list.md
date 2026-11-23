# 音频清单

所有音频都按名字从 `Resources` 加载，缺失时静默跳过（`AudioManager`）。
- 音效：`Assets/Resources/Audio/Sfx/<name>.(wav|ogg|mp3)`
- 音乐：`Assets/Resources/Audio/Bgm/<name>.(ogg|mp3)`，循环播放

## UI 音效（代码里写死）

| 名称 | 用途 | 建议时长 |
|---|---|---|
| hover | 鼠标移到按钮 / 线索物体上 | 0.05–0.1 s |
| click | 按钮点击、重复点已记下的线索 | 0.05–0.15 s |
| open | 打开下拉列表 | 0.1 s |
| page | 翻纸：记下线索、Clear、进入关卡、故事卡片、证书出现、重填 | 0.2–0.4 s |
| submit | 按下 SUBMIT | 0.3 s |
| stamp | 盖章落下（APPROVED / REJECTED 都用） | 0.2–0.3 s |
| approve | 盖完 APPROVED 后的小段好消息 | 0.8–1.5 s |
| reject | 盖完 REJECTED 后的小段坏消息 | 0.5–1 s |
| shift_in | 按下 Shift：进入彼岸视角 / 第 6 关表单换挡 | 0.3–0.6 s |
| shift_out | 松开 Shift | 0.2–0.4 s |
| tick_ui | 设置里拖动音量滑条 | 0.03–0.05 s |

## 死法演出（BeatPlayer 自己播放）

| 名称 | 用途 | 建议时长 |
|---|---|---|
| die_burn | 主角起火 | 1–1.5 s |
| die_fall | 主角开始坠落（风声/惊叫） | 0.5–1 s |
| impact | 坠落着地 | 0.2–0.4 s |
| die_drown | 主角入水下沉（扑通 + 气泡） | 1–1.5 s |

## Beat `sfx` 指令（来自关卡 JSON）

| 名称 | 用途 | 建议时长 |
|---|---|---|
| roar | 第 1 关大灯轰然点亮 | 1–1.5 s |
| creak | 第 2 关水车吱呀转起来 | 1–2 s |
| thump | 第 2 关掉进面粉袋 | 0.3 s |
| crackle | 第 2 关水车燃烧 | 1–2 s |
| oof | 第 3 关砸到学徒 | 0.3–0.5 s |
| swear | 第 3 关面包师骂骂咧咧把你拽出来 | 0.8–1.2 s |
| whistle | 第 4 关火车汽笛 | 1–1.5 s |
| clunk | 第 4 关道岔扳动 | 0.3 s |
| brakes | 第 4 关急刹 | 1–2 s |
| crash | 第 4 关撞上货车 | 1–1.5 s |
| grab | 第 5 关巡更人揪住衣领 | 0.3 s |
| tick | 第 5 关钟重新走起 | 0.5–1 s |
| grind | 第 5 关齿轮卡死 | 1–1.5 s |
| punch_clock | 第 6 关打卡下班 | 0.5–0.8 s |

## 音乐

| 名称 | 用途 | 建议时长 |
|---|---|---|
| title | 标题画面（第一次点击后才开始） | 60–120 s 循环 |
| level | 关卡内 | 90–180 s 循环 |
| credits | 制作名单 | 60–90 s 循环 |
