# CLOCKING OUT — 设计文档（Game Off 2026，主题 SHIFT）

> 工作标题 **Clocking Out**（英语里 "clock out" 既是下班打卡，也是"去世"的俚语）。界面与游戏内文本一律英文。

## 1. 一句话

死神太忙，把你的死亡通知单丢给你自己填：你躲不掉这场死，但可以决定**怎么死、死在哪、死在哪一班**，让这次死亡恰好完成你的遗愿。

## 2. 主题诠释（Shift 的四层）

1. **班次**：时间栏的选项就是班次（shift）。NPC 按排班表行动，排班表是场景里的线索。
2. **按住 Shift 键**：切换到"彼岸视角"，预览世界在所选班次的状态（谁在哪、水位多高），并显示你的幽灵站在所选地点。
3. **甩锅**：死神把文书工作甩给将死之人；终局里他把整个岗位甩给你。
4. **终局**：只有按住 Shift 才出现的选项（"End of Shift"）是最后一关的解。

itch 页面阐释草稿："Death is overworked, so he shifts the paperwork to you. Pick the shift you die on, hold Shift to peek at the other side, and help Death finally end his shift."

## 3. 核心循环

1. 进入关卡：一个静止的小场景（diorama）+ 一张死亡通知单。通知单上已印好姓名和 **Last Wish（遗愿）**。
2. 观察：鼠标悬停/点击场景里的物件读线索（短句）。按住 Shift 预览所选班次的世界状态。
3. 填单：下拉选择 Cause（死因）/ Place（地点）/ Shift（班次）。
4. 提交：死神盖章前先"推演"——场景按事件时间线播放你的死亡与后果，每个事件配一句字幕。
5. 结果：遗愿达成 → 盖 **APPROVED**，弹出死亡证明（Death Certificate），进入下一关；否则盖 **REJECTED**，给出一句原因（说明为什么，不泄露解法），时间倒回，重填。
6. 评价：按提交次数给印章——1 次 = Gold "Filed without correction"，2–3 次 = Silver，更多 = Paper。无失败状态，不卡关。

玩家可随时跳过/加速推演（点击或空格），可一键清空表单。

## 4. 规则体系（所有关卡通用，玩家需要逐步学会）

按判定顺序：

1. **可能性**：死因需要介质。Burn 需要该地点该班次有火源；Drown 需要足够深的水；Fall 需要高度。不满足 → 退单（"There is no fire in the storeroom."）。
2. **不被打断**：该地点该班次有清醒的人在场 → 你被救下 → 退单。
3. **死亡的直接后果**（每种死因留下不同的东西）：Burn 留下火（点燃同地点的可燃物，可能蔓延）；Fall 留下冲击（砸中正下方的东西）；Drown 留下水里的身体（抬高水位、堵塞、漂流）。
4. **连锁**：后果与世界状态（班次决定）作用，产生后续事件。
5. **遗愿判定**：最终事实满足遗愿 → 批准；否则退单，原因取自最后一个带 verdict 的事件。

教学节奏：第 1 关只教规则 1 和 3；第 2 关加班次（世界状态随时间变）；第 3 关加规则 2（目击者）；第 4 关三条诱人路线各被一种线索否掉；第 5 关 NPC 的行动改变世界状态，且 NPC 出现在"后果地点"会撤销后果；第 6 关打破规则。

## 5. 技术架构

- Unity 2022.3 LTS，URP，WebGL。旧版 Input Manager，uGUI + TextMeshPro。
- **Core（纯逻辑，程序集 `DeathNotice.Core`）**：关卡数据模型、JSON 解析、模拟器。不引用场景对象。
- **Game（表现，程序集 `DeathNotice.Game`）**：通知单 UI、场景舞台、推演播放器、流程、音频。
- **关卡数据是 JSON**（`Assets/Resources/Levels/level_01.json` …），不是 ScriptableObject——便于版本对比和脚本校验。测试与游戏读同一份文件。
- **EditMode 测试**：对每关枚举全部组合，断言"被批准的组合集合 == 文件里声明的 solutions"，且每个退单都有非空原因。

### 5.1 模拟器

```
SimResult Simulate(LevelData level, Dictionary<string,string> choices)
SimResult { bool approved; string verdict; List<SimEvent> events; }
SimEvent  { string ruleId; string caption; List<Beat> beats; }
```

算法（确定性，单遍）：

1. facts = { "cause:burn", "place:top", "shift:night" …（玩家选择）} ∪ level.always ∪ level.states[所选班次]
2. 按文件顺序遍历 rules。规则触发条件：`when` 全部在 facts 中，且 `unless` 无一在 facts 中。
3. 触发时：追加事件（caption + beats）；把 `add` 加入 facts，把 `remove` 移出 facts；若有 `verdict` 则更新当前 verdict；若 `reject: true` 则立即结束，结果为退单。
4. 遍历完：若 `wish.all` 全在 facts 且 `wish.none` 无一在 facts → approved；否则退单，verdict 为最后记录的 verdict，没有则用 `level.defaultVerdict`。

规则顺序即因果顺序，作者负责排好（先可能性，再打断，再死亡，再连锁）。

### 5.2 关卡 JSON 结构

```json
{
  "id": "level_01",
  "title": "The Lighthouse",
  "deceased": "Tom Wick, keeper's apprentice",
  "wishText": "Let the great lamp burn tonight.",
  "fields": [
    { "id": "cause", "label": "Cause", "options": [ { "id": "burn", "label": "Burned" } ] },
    { "id": "place", "label": "Place", "options": [ { "id": "lamp", "label": "Lamp Room" } ] }
  ],
  "always": ["fire@lamp"],
  "states": { "noon": ["water_high"] },
  "clues": [ { "anchor": "lamp", "text": "The wick is dry and ready. All it needs is a flame." } ],
  "rules": [
    { "id": "no_fire", "when": ["cause:burn"], "unless": ["fire@here"], "reject": true,
      "verdict": "Nothing here could set you alight.", "caption": "...", "beats": [] }
  ],
  "wish": { "all": ["lamp_lit"], "none": [] },
  "defaultVerdict": "You died. The wish did not come true.",
  "solutions": [ { "cause": "burn", "place": "lamp" } ]
}
```

为了少写规则，引擎在第 1 步额外生成派生事实：对每个形如 `X@<place>` 的事实，若 `<place>` 等于玩家所选地点，则加入 `X@here`。

### 5.3 Beat（表现指令）

推演播放器按顺序执行每个事件的 beats，同时显示 caption。Beat 是 `{ "do": "...", "target": "锚点名", "arg": "..." }`，最小集合：

`ghost_to`（主角移动到锚点）、`die_burn` / `die_fall` / `die_drown`（三种死法演出）、`fx`（在锚点播放 fire / water / smoke / sparks / splash）、`move`（把某物体移到另一锚点）、`rotate`（持续旋转，如水车）、`toggle`（显示/隐藏物体）、`light`（开灯）、`npc_to`（NPC 走到锚点）、`sfx`、`wait`、`shake`（镜头震动）。

演出粗糙没关系：**字幕负责把因果讲清楚**，动画只是点缀。

### 5.4 舞台

每关的 diorama 由代码搭建（`StageBuilder`），先用基础几何体灰盒，每个可交互/可被 beat 引用的物体有锚点名。正式模型（fbx）到位后按锚点名替换。按住 Shift 时，舞台读取 `states[所选班次]` 里以 `show:` 开头的事实来摆放 NPC 和水位等可见状态。

## 6. 范围与优先级

1. 主线 6 关可通关（灰盒也算）。
2. 推演演出、死亡证明、评价印章。
3. Shift 预览。
4. 音频（BGM + 事件音效）。
5. 正式美术替换灰盒。
6. 支线"重审"（责任栏）——只在全部完成后考虑。

退路：若 11/28 时第 4、5 关未完成，砍成 4 关（1、2、3 + 终局）。11/29 功能冻结。
