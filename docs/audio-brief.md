# 音频需求（Clocking Out）

给负责音频的同学。程序那边的加载约定和完整名称表见 `docs/audio-list.md`（以那份为准，本文是制作说明）。
音频全部按**文件名**从 `Resources` 加载，缺哪个就静默跳过，所以可以一个个往里放，不会弄坏游戏。

## 文件放置

```
Assets/Resources/Audio/Sfx/<name>.ogg   （或 .wav）
Assets/Resources/Audio/Bgm/<name>.ogg
```

- `<name>` 必须和下表名称一字不差，全小写。
- 音效：短的（< 1 s）用 wav 或 ogg 都行；长的用 ogg。单声道，44.1 kHz。
- 音乐：ogg，立体声，44.1 kHz，比特率 128–160 kbps。WebGL 包体有限，每首控制在 2 MB 以内。
- 响度：音乐整体比音效低一档（参考音乐 -18 LUFS，音效峰值 -3 dBFS）。所有音效头尾不留静音。
- 提醒：浏览器要求玩家点一下之后才能出声，第一次点击之前不会有任何声音，这是正常的。

**每放进一个文件，都在 `docs/asset-ledger.csv` 登记一行**：文件路径、来源网址、作者、授权（CC0 / CC-BY 需署名）、是否 AI 生成、生成提示词、日期。CC-BY 的素材记得在制作名单里署名，非 CC0 / CC-BY 的不要用。

---

## 1. BGM（2 首）

整体气质：**俏皮的黑色幽默**。像一位疲惫但礼貌的办事员在加班：不吓人、不悲伤，带点滑稽的阴郁。
乐器方向：拨弦（pizzicato 弦乐、曼陀林或尤克里里）、钟表滴答声当节拍、低音单簧管或巴松管当旋律、偶尔一点钢片琴或八音盒。节奏用**华尔兹（3/4）或慢摇摆**。

要求：纯音乐、无人声；**可无缝循环**（结尾和开头能接上，不要淡出淡入）；长度 60–90 秒。

| 名称 | 用途 | 长度 | 说明 |
|---|---|---|---|
| `level` | 关卡内循环 | 60–90 s | 玩家要边听边读线索，旋律稀疏、不抢注意力，中速 |
| `title` | 标题画面和终局 / 制作名单 | 60–90 s | 主题更完整，可以稍微温情一点；结尾"Your shift begins."之后接这首 |

> `docs/audio-list.md` 里还有第 3 首 `credits`。如果只做 2 首，可以把 `title` 复制一份命名为 `credits.ogg`，或请程序让制作名单直接用 `title`。

### 生成提示词（英文）

**level**：
```
Playful dark-comedy instrumental, slow waltz in 3/4 at 84 BPM, pizzicato strings and a plucked mandolin carrying the rhythm, a ticking pocket-watch as percussion, a sly low bass clarinet melody, light celesta accents, sparse and quiet so it sits under gameplay, whimsical and slightly macabre like an overworked undertaker's office, no vocals, seamless loop, 75 seconds
```

**title**：
```
Whimsical macabre instrumental theme, gentle slow swing at 92 BPM, bass clarinet and bassoon duet over pizzicato strings and upright bass, ticking clock and soft music-box glockenspiel, a bittersweet warm ending phrase, dark humor with a touch of tenderness, end of a long work shift, no vocals, seamless loop, 80 seconds
```

---

## 2. 音效清单

建议的 CC0 来源：
- **Kenney**（kenney.nl/assets，全部 CC0）：*Interface Sounds*、*UI Audio*、*Impact Sounds*、*RPG Audio*、*Casino Audio*
- **Freesound**（freesound.org），搜索后把 License 筛选设为 **Creative Commons 0**
- **OpenGameArt**（筛选 CC0）作为补充

### UI 事件

| 名称 | 用途 | 建议时长 | 搜索关键词 |
|---|---|---|---|
| `hover` | 鼠标移到按钮 / 线索物件上 | 0.05–0.1 s | Kenney Interface Sounds "tick"；Freesound "soft ui tick" |
| `click` | 按钮点击 | 0.05–0.15 s | Kenney UI Audio "click"；Freesound "paper click", "typewriter key" |
| `open` | 打开下拉列表 | 0.1 s | Freesound "drawer open short", "paper flick" |
| `page` | 翻纸：记下线索、清空表单、进关、证书出现 | 0.2–0.4 s | Kenney RPG Audio "bookFlip"；Freesound "page turn", "paper rustle" |
| `submit` | 按下 SUBMIT | 0.3 s | Freesound "paper slide", "envelope" |
| `stamp` | 盖章落下（APPROVED / REJECTED 共用） | 0.2–0.3 s | Freesound "rubber stamp", "stamp desk thud" |
| `approve` | 盖完 APPROVED 后的小段好消息 | 0.8–1.5 s | Kenney "jingle"；Freesound "small bell ding", "service bell" |
| `reject` | 盖完 REJECTED 后的小段坏消息 | 0.5–1 s | Freesound "kazoo fail", "bassoon wah", "sad trombone short" |
| `shift_in` | 按下 Shift，进入彼岸视角 | 0.3–0.6 s | Freesound "whoosh reverse", "ghostly swell", "glass harmonica" |
| `shift_out` | 松开 Shift | 0.2–0.4 s | 同上，选更短、往下走的 |
| `tick_ui` | 拖动音量滑条 | 0.03–0.05 s | Kenney Interface Sounds "tick" |

### 死法演出（程序自动播放）

| 名称 | 用途 | 建议时长 | 搜索关键词 |
|---|---|---|---|
| `die_burn` | 主角起火 | 1–1.5 s | Freesound "fire ignite whoosh", "torch light" |
| `die_fall` | 开始坠落（风声 / 一声短促惊叫，滑稽不吓人） | 0.5–1 s | Freesound "cartoon fall whistle", "falling whistle" |
| `impact` | 坠落着地 | 0.2–0.4 s | Kenney Impact Sounds；Freesound "body thud cartoon" |
| `die_drown` | 入水下沉（扑通 + 气泡） | 1–1.5 s | Freesound "splash bubbles", "plop underwater" |

### 关卡事件（来自关卡 JSON 的 `sfx` beat）

| 名称 | 关卡 | 用途 | 建议时长 | 搜索关键词 |
|---|---|---|---|---|
| `roar` | 1 | 大灯轰然点亮 | 1–1.5 s | Freesound "gas burner ignite roar", "fire whoosh big" |
| `creak` | 2 | 老水车吱呀转起来 | 1–2 s | Freesound "wooden wheel creak", "old wood creak" |
| `thump` | 2 | 从活板门掉进面粉袋 | 0.3 s | Kenney Impact Sounds "soft"；Freesound "sack thud" |
| `crackle` | 2 | 水车燃烧 | 1–2 s | Freesound "fire crackle wood" |
| `oof` | 3 | 砸到学徒身上 | 0.3–0.5 s | Freesound "oof grunt cartoon" |
| `swear` | 3 | 面包师骂骂咧咧把你拽出来（含糊不清的嘟囔，不要真脏话） | 0.8–1.2 s | Freesound "gibberish grumble", "muffled angry mumble" |
| `whistle` | 4 | 被在岗的人发现时（警哨或火车汽笛，需和设计确认） | 1–1.5 s | Freesound "steam train whistle" |
| `clunk` | 4 | 道岔扳动 | 0.3 s | Freesound "metal lever clunk", "railway points" |
| `brakes` | 4 | 火车急刹（改道成功、信号臂截停都用） | 1–2 s | Freesound "train brake squeal" |
| `crash` | 4 | 快车撞上货车 | 1–1.5 s | Freesound "metal crash cartoon", Kenney Impact Sounds "heavy" |
| `grab` | 5 | 巡更人揪住衣领 | 0.3 s | Freesound "cloth grab", "fabric grab" |
| `tick` | 5 | 巡更人把你拽出来，钟重新走起 | 0.5–1 s | Freesound "clock tick tock loud", "grandfather clock" |
| `grind` | 5 | 齿轮卡死 | 1–1.5 s | Freesound "gears grinding stop", "machine jam" |
| `punch_clock` | 6 | 打卡钟打卡下班 | 0.5–0.8 s | Freesound "time clock punch", "time card stamp" |

---

## 3. 交付检查

- [ ] 文件名和上表一致，放在正确目录
- [ ] 两首 BGM 在播放器里循环播放，接缝听不出来
- [ ] 音效头尾没有空白，没有爆音
- [ ] 每个文件都登记进 `docs/asset-ledger.csv`，AI 生成的音乐写明工具和提示词
