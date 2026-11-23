# 关卡 JSON 结构（最终版）

文件：`Assets/Resources/Levels/level_XX.json`，一关一个文件，UTF-8。由 `DeathNotice.Core.LevelLoader.FromJson` 用 `UnityEngine.JsonUtility` 解析到 `LevelData`（`Assets/Scripts/Core/LevelData.cs`）。`tools/validate_levels.py` 与 EditMode 测试读同一份文件。

相对 design.md 5.2 示例的调整，原因都是 JsonUtility 不支持 Dictionary：

- `states` 从 `{ "noon": [...] }` 改为数组 `[{ "shift": "noon", "facts": [...] }]`。
- `solutions` 从 `[{ "cause": "burn", "place": "lamp" }]` 改为 `[{ "choices": [{ "field": "cause", "option": "burn" }, ...] }]`。
- 新增 `anchors`、`intro`、`outro`、`clues[].minRejects`、`rules[].unreachable`、`fields[].options[].shiftOnly`。
- beat 的 `target` 可以写保留字 `"here"`（`npc_to` / `move` 的 `arg` 也可以），表示玩家所选地点；`Simulator` 返回事件时已替换成真实地点 id。

## 顶层

| 键 | 类型 | 说明 |
|---|---|---|
| `id` | string | `level_01` …，与文件名一致 |
| `title` | string | 关卡名 |
| `deceased` | string | 通知单上的姓名 |
| `wishText` | string | Last Wish 文本 |
| `intro` | string | 关卡开始前死神的台词，可为空（第 1 关是开场白） |
| `outro` | string | 批准后的结尾字幕，可为空（第 6 关是 "Your shift begins."） |
| `fields` | Field[] | 表单栏，顺序就是显示顺序。id 必须有 `cause`、`place`；`shift` 可选（第 1 关没有） |
| `always` | string[] | 始终为真的事实 |
| `states` | State[] | 每个班次的额外事实；没有 shift 栏时为 `[]` |
| `anchors` | string[] | 本关舞台必须提供的全部锚点名（地点 id、线索物件、NPC、beat 引用的物体） |
| `clues` | Clue[] | 悬停线索 |
| `rules` | Rule[] | 按文件顺序判定，顺序就是因果顺序 |
| `wish` | `{ "all": string[], "none": string[] }` | 遗愿判定 |
| `defaultVerdict` | string | 退单时没有任何规则写 verdict 才用它（当前 6 关都用不到） |
| `solutions` | Solution[] | 声明的全部解，校验要求 == 模拟器批准的集合 |

## Field / Option

```json
{ "id": "cause", "label": "Cause", "options": [
  { "id": "burn", "label": "Burned", "shiftOnly": false },
  { "id": "retire", "label": "End of Shift", "shiftOnly": true }
] }
```

`shiftOnly: true` 的选项只在按住 Shift 时出现在下拉里，模拟和全组合枚举照常包含它。

## State

```json
{ "shift": "four", "facts": ["witness@oven", "apprentice_below", "show:baker@oven", "show:apprentice@store"] }
```

`shift` 是 shift 栏的 option id。以 `show:` 开头的事实只给舞台用（Shift 预览），规则不得引用（校验器会检查）。两种写法：

- `show:<npc或物体>@<锚点>`：把某物摆到某锚点，两边都必须在 `anchors` 里。
- `show:<键>=<值>`：可见状态，例如 `show:water_level=high`、`show:tank_level=almost_full`、`show:hatch=open`。

`LevelData.GetShowFacts(shift)` 返回去掉前缀的列表。

## Clue

```json
{ "anchor": "keyboard_plaque", "text": "He glances at the oldest key on your keyboard.", "minRejects": 2 }
```

`minRejects`：退单达到这么多次后才出现，默认 0。

## Rule

```json
{
  "id": "through",
  "when": ["cause:fall", "place:belfry", "hatch_open"],
  "unless": [],
  "add": ["body_in_gears"],
  "remove": [],
  "reject": false,
  "verdict": "",
  "caption": "You drop through the open hatch, straight into the gear train.",
  "beats": [ {"do": "ghost_to", "target": "belfry", "arg": ""}, ... ],
  "unreachable": false
}
```

- 触发：`when` 全在 facts 中且 `unless` 无一在 facts 中。
- 触发后：记事件（caption + beats）→ 加 `add` → 删 `remove` → `verdict` 非空则覆盖当前 verdict → `reject` 为真则立即退单结束。
- 玩家选择生成的事实是 `<field>:<option>`，如 `cause:burn`、`place:lamp`、`shift:noon`。
- 派生事实：第 1 步结束时，对每个 `X@<所选地点>` 加入 `X@here`（之后规则 add 的 `X@地点` 不再派生）。
- 只带 verdict 的规则，caption 与 verdict 文本相同；reject 规则也一样。播放器在最后一个事件的字幕与退单原因相同时可以只显示一次。
- `unreachable: true` 只是给校验器的说明，模拟器忽略它；标了却能触发也会报错。

## Beat

`{ "do": "...", "target": "锚点", "arg": "..." }`，三个键都是字符串（数字也写成字符串）。

| do | target | arg |
|---|---|---|
| `ghost_to` | 锚点 / `here` | — |
| `die_burn` `die_drown` `die_fall` | 演出位置的锚点 / `here` | — |
| `fx` | 锚点 / `here` | `fire` `water` `smoke` `sparks` `splash` |
| `move` | 被移动的物体锚点 | 目的地锚点 / `here` |
| `npc_to` | NPC 锚点 | 目的地锚点 / `here` |
| `rotate` | 锚点 | 速度，`"0"` 表示停转 |
| `toggle` | 锚点 | `on` / `off` |
| `light` | 锚点 | — |
| `sfx` | 空 | 音效名（`roar` `creak` `thump` `crackle` `clunk` `brakes` `crash` `whistle` `oof` `swear` `grab` `tick` `grind` `punch_clock`） |
| `wait` | 空 | 秒数 |
| `shake` | 空 | 强度 0–1 |

每条规则 2–5 个 beat。所有以玩家死亡结束（没有被 reject 规则截停）的 burn/drown/fall 组合，事件里一定有 `ghost_to` 和对应的 `die_<cause>`。

## Solution

```json
{ "choices": [ {"field": "cause", "option": "fall"}, {"field": "place", "option": "sluice"}, {"field": "shift", "option": "noon"} ] }
```

每个解必须恰好覆盖本关全部栏。`SolutionData.ToDictionary()` 直接得到可传给 `Simulator.Simulate` 的字典。
