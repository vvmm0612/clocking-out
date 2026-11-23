# Clocking Out：发布素材

全部是游戏真实画面，由 PlayMode 工具在 batchmode 下渲染（`Assets/Tests/PlayMode/PressKitCapture.cs`）。重新生成：

```
powershell -ExecutionPolicy Bypass -File tools\test-playmode.ps1 -Filter "DeathNotice.Tests.PlayMode.PressKitCapture"
```

（该测试标了 `[Explicit]`，普通的 `test-playmode.ps1` 不会跑它。）

| 文件 | 尺寸 | 内容 | 用途 |
|---|---|---|---|
| `cover_630x500.png` | 630×500 | 灯塔 diorama + 标题 "CLOCKING OUT" + 标语 + 死神剪影 | itch.io 封面图（Cover image），也可作 Game Off 提交缩略图 |
| `01_title.png` | 1920×1080 | 标题画面：缓慢旋转的灯塔展台、大标题、标语、死神剪影 | itch 页面首图 / 截图第 1 张 |
| `02_case1_filling.png` | 1920×1080 | 第 1 关：死亡通知单填到一半，地点下拉框展开，已记下几条线索 | 截图：说明核心玩法（看线索、填表） |
| `03_shift_preview.png` | 1920×1080 | 第 4 关深夜小站按住 Shift 预览"另一边"：幽灵站在所选地点，NPC 在该班次的位置 | 截图：说明主题 SHIFT 的机制 |
| `04_playback_caption.png` | 1920×1080 | 第 3 关面包房推演中：火从烤炉房烧进仓库，底部字幕条讲述经过 | 截图：推演演出 |
| `05_certificate_gold.png` | 1920×1080 | 第 2 关一次通过后的死亡证明与金色评价章 | 截图：结算 / 评价 |

截图不含 F1 调试信息（只在编辑器 / Development Build 里有，且默认关闭）。
