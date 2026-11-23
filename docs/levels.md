# 关卡规格（6 关）

规则按**文件顺序**触发（见 design.md 5.1）。记号：`X@here` 是引擎派生事实（玩家所选地点有 X）。`witness@<place>` 表示该地点有清醒的人。所有英文句子是游戏内正式文本的初稿，可润色但不得改变含义或泄露解法。

通用退单规则（每关都有，文案按关卡替换）：

| id | when | unless | 结果 |
|---|---|---|---|
| no_fire | cause:burn | fire@here | reject |
| no_water | cause:drown | water@here | reject |
| no_height | cause:fall | height@here | reject |
| rescued | witness@here | — | reject |

开场白（第 1 关前，死神）："You're due today. I'm swamped, so fill it in yourself. I'll stamp it if it adds up. Do make it useful — you only get the one."

---

## 第 1 关 The Lighthouse（教学：介质 + 后果）

- Deceased: Tom Wick, keeper's apprentice
- Last Wish: "Let the great lamp burn tonight."
- 栏：Cause {burn Burned, drown Drowned, fall Fell} × Place {dock The Dock, beach The Beach, lamp The Lamp Room}（无班次栏，9 种）
- always: `water@dock` `water@beach` `height@lamp` `fire@lamp`
- 解：burn + lamp

| # | id | when | unless | 效果 | 文案 |
|---|---|---|---|---|---|
| 1 | no_fire | cause:burn | fire@here | reject | "Nothing out here could set you alight. The only flame on this rock is the pilot lantern upstairs." |
| 2 | no_water | cause:drown | water@here | reject | "You cannot drown in a dry room, however hard you try." |
| 3 | no_height | cause:fall | height@here | reject | "You tripped. You got up. Nobody dies of that." |
| 4 | burn_lamp | cause:burn | — | add lamp_lit | caption "The lantern tips. Your sleeve catches — then the wick. The great lamp roars awake." |
| 5 | drown_any | cause:drown | — | verdict | "The sea took you quietly. The lamp stayed dark." |
| 6 | fall_lamp | cause:fall | — | verdict | "A long drop from the gallery. The lamp stayed dark, and your lantern went down with you." |

- wish.all: `lamp_lit`
- 线索：lamp「The wick is dry and ready. All it needs is a flame.」；lantern「A pilot lantern, always lit, hanging beside the great lamp. The only fire on the rock.」；dock「Deep water. Cold.」；beach「The tide comes right up to the stones.」；gallery「A sixty-foot drop to the rocks.」

## 第 2 关 The Mill（加入班次：世界状态随时间变）

- Deceased: Ned Grist, miller's hand
- Last Wish: "Let the old wheel turn again."
- 栏：Cause × Place {sluice The Sluice Catwalk, pit The Wheel Pit, loft The Grain Loft} × Shift {dawn Dawn Shift, noon Noon Shift, dusk Dusk Shift}（27 种）
- always: `height@sluice` `height@loft` `fire@sluice` `fire@pit` `fire@loft`（你随身带着提灯）
- states: dawn `[]`；noon `[pond_deep, water@sluice]`；dusk `[]`
- 解：fall + sluice + noon

| # | id | when | unless | 效果 | 文案 |
|---|---|---|---|---|---|
| 1 | dry_pit | cause:drown, place:pit | — | reject | "The pit is bone dry while the gate is shut." |
| 2 | dry_loft | cause:drown, place:loft | — | reject | "Flour is not a liquid. You checked." |
| 3 | shallow | cause:drown, place:sluice | pond_deep | reject | "On this shift the pond is ankle-deep. You got wet socks." |
| 4 | bottom | cause:fall, place:pit | — | reject | "You are already at the bottom." |
| 5 | soft | cause:fall, place:loft | — | reject | "You fell through the trapdoor into a heap of flour sacks. Embarrassing. Not fatal." |
| 6 | drown_sluice | cause:drown, place:sluice | — | verdict | "You sank beside the gate. The gate stayed shut. The wheel stayed still." |
| 7 | burn_loft | cause:burn, place:loft | — | add mill_burned；verdict | "Lantern, meet flour dust. The mill is now a memory. So is the wheel." |
| 8 | burn_pit | cause:burn, place:pit | — | add wheel_burned；verdict | "Ten years of dry oak. The wheel burned beautifully and will never turn again." |
| 9 | burn_sluice | cause:burn, place:sluice | — | verdict | "You burned on wet planks above the water. Nothing else caught." |
| 10 | fall_sluice | cause:fall, place:sluice | — | add gate_open | caption "You drop from the catwalk onto the rusted lever. Something gives. Mostly the lever." |
| 11 | trickle | gate_open | pond_deep | verdict | "The gate groaned open. A trickle ran through. The wheel did not care." |
| 12 | rush | gate_open, pond_deep | — | add wheel_turning | caption "The whole pond comes through at once. The old wheel shudders, complains, and turns." |

- wish.all: `wheel_turning`
- 线索：dam notice「Upstream dam opens its gates for the Noon Shift only.」；gauge「Pond depth gauge. It reads differently on every shift.」（Shift 预览时显示水位）；lever「Seized solid since spring. Needs one good whack from above.」；trapdoor「Opens onto the flour sacks below.」；sign「NO NAKED FLAMES. Flour dust.」；wheel「Dry oak. Ten years without water.」

## 第 3 关 The Bakery（加入目击者）

- Deceased: Agnes Crumb, bookkeeper
- Last Wish: "Let no one ever read the ledger."
- 栏：Cause × Place {oven The Oven Room, store The Storeroom, attic The Attic} × Shift {four 4 AM, five 5 AM, six 6 AM}（27 种）
- always: `fire@oven` `water@oven` `height@attic`
- states: four `[witness@oven, apprentice_below]`；five `[]`；six `[witness@oven]`
  - 可见状态：面包师 4 点、6 点在烤炉房，5 点把第一炉送去前店；学徒 4 点在仓库取面粉，4:30 下班回家。
- 解：burn + oven + five

| # | id | when | unless | 效果 | 文案 |
|---|---|---|---|---|---|
| 1 | no_fire | cause:burn | fire@here | reject | "No flame is allowed near the flour. The oven is the only fire in the building." |
| 2 | no_water | cause:drown | water@here | reject | "Nothing here is deep enough. The dough vat is in the Oven Room." |
| 3 | no_height | cause:fall | height@here | reject | "The floor is right there. You are standing on it." |
| 4 | caught | cause:fall, place:attic, apprentice_below | — | reject | "The apprentice was fetching flour right under the hatch. He broke your fall. He would like an apology." |
| 5 | rescued | witness@here | — | reject | "The baker was right there. He hauled you out, swearing magnificently." |
| 6 | drown_vat | cause:drown | — | verdict | "You drowned in the dough vat. The ledger sits untouched next door." |
| 7 | fall_attic | cause:fall | — | verdict | "You landed in the storeroom, right beside the ledger. It is still perfectly legible." |
| 8 | burn_oven | cause:burn | — | add fire_loose | caption "The oven door swings wide. Nobody is here to shut it." |
| 9 | spread | fire_loose | — | add ledger_burned | caption "The fire walks through the open door into the storeroom. Paper burns first." |

- wish.all: `ledger_burned`
- 线索：roster「BAKER — 4 AM: oven, first batch. 5 AM: carry bread to the shop front. 6 AM: oven, second batch. APPRENTICE — fetch flour at 4, home by half past.」；oven「The oven. The baker never lets it go out between batches.」；ledger「The accounts. Every page of them. Shelved in the storeroom, where nothing ever burns.」；vat「Dough water. Deep enough to bathe in.」；hatch「Attic hatch. Opens above the storeroom.」；door「The door between oven and storeroom never shuts properly.」；sign「NO FLAME BEYOND THIS POINT.」

## 第 4 关 The Railway Halt（三条诱人路线，各被一种线索否掉）

- Deceased: Wilf Sidings, lamp boy
- Last Wish: "Stop the midnight express before the broken bridge."
- 栏：Cause × Place {box The Signal Box, tower The Water Tower, platform The Platform} × Shift {evening Evening Shift, night Night Shift, graveyard Graveyard Shift}（27 种）
- always: `height@box` `height@tower` `water@tower` `fire@platform`
- states: evening `[witness@box, witness@platform, siding_free]`；night `[witness@box, witness@platform]`；graveyard `[tank_brim]`
  - 可见状态：水箱水位 evening 一半、night 四分之三、graveyard 差一点满；货车 9:30 后停在侧线；信号员（box）和站长（platform）只在 evening、night 两班在岗。
- 解：drown + tower + graveyard

| # | id | when | unless | 效果 | 文案 |
|---|---|---|---|---|---|
| 1 | no_fire | cause:burn | fire@here | reject | "The brazier on the platform is the only fire at this halt." |
| 2 | no_water | cause:drown | water@here | reject | "The only deep water here is inside the tower tank." |
| 3 | no_height | cause:fall | height@here | reject | "The platform is knee-high. Try harder." |
| 4 | rescued | witness@here | — | reject | "Someone on shift saw you and dragged you clear. Staffed hours are so inconvenient." |
| 5 | fall_box | cause:fall, place:box | — | add points_shifted | caption "You drop from the gallery onto the points lever. It shifts." |
| 6 | diverted | points_shifted, siding_free | — | add express_safe | caption "The express swings onto the empty siding and rolls to a stop." |
| 7 | crash | points_shifted | siding_free | verdict | "The express swung onto the siding — straight into the parked freight wagons. That was not the wish." |
| 8 | fall_tower | cause:fall, place:tower | — | verdict | "You fell from the tank ladder onto the track. In the dark, the driver never saw you." |
| 9 | burn_platform | cause:burn | — | verdict | "The express thundered past a small fire on the platform. The driver took it for the brazier." |
| 10 | drown_tank | cause:drown | — | — | caption "You slip into the tank. The water rises by exactly one of you." |
| 11 | float_no | cause:drown | tank_brim | verdict | "The water rose, but not to the brim. The float never lifted the signal arm." |
| 12 | float_yes | cause:drown, tank_brim | — | add express_safe | caption "The float lifts. The WATER STOP arm swings out. The driver, cursing, obeys the rulebook and brakes." |

- wish.all: `express_safe`
- 说明：规则 6 在现有数据下不可达（evening 班 box 有人），保留它是为了规则自洽。
- 线索：timetable「9:30 PM freight — holds on the siding until dawn. 11:59 PM express — does not stop.」；rulebook「Rule 14: a driver MUST stop when the WATER STOP arm is out.」；tank「The float raises the WATER STOP arm when the tank is brim-full. The pump tops it up through the night, but never quite to the brim.」；roster「Evening & Night shifts: signalman (box), stationmaster (platform). Graveyard shift, 11 PM till dawn: unstaffed since the budget cuts.」；lever「Points lever. Shifts the express onto the siding.」；brazier「Lit every evening. Every driver knows it.」；telegram「BRIDGE OUT. Nobody has told the express.」

## 第 5 关 The Clock Tower（NPC 的行动改变世界；NPC 在后果地点会撤销后果）

- Deceased: Tobias Pell, clock-winder
- Last Wish: "Stop the clock. Let this shift never end."
- 栏：Cause × Place {belfry The Belfry, gears The Gear Room, pit The Pendulum Pit, face The Clock Face} × Shift {nine 9 PM, ten 10 PM, eleven 11 PM, midnight Midnight}（48 种）
- always: `height@belfry` `height@face` `fire@gears` `water@pit`
- states: nine `[witness@gears]`；ten `[witness@belfry, hatch_open]`（巡更人 10 点在钟楼打开舱门）；eleven `[witness@pit, hatch_open]`；midnight `[witness@gears, hatch_open, watchman_in_gears]`
- 解：fall + belfry + eleven

| # | id | when | unless | 效果 | 文案 |
|---|---|---|---|---|---|
| 1 | no_fire | cause:burn | fire@here | reject | "The only flame in the tower is the oil lamp in the Gear Room." |
| 2 | no_water | cause:drown | water@here | reject | "Only the Pendulum Pit holds water." |
| 3 | no_height | cause:fall | height@here | reject | "Not enough of a drop to matter." |
| 4 | rescued | witness@here | — | reject | "The night watchman was on his round. He caught you by the collar." |
| 5 | fall_face | cause:fall, place:face | — | verdict | "You clung to the minute hand. It bent. You fell. The clock now runs five minutes slow — but it runs." |
| 6 | burn_gears | cause:burn | — | verdict | "Brass does not burn. The gears glowed red and kept turning." |
| 7 | drown_pit | cause:drown | — | verdict | "The pendulum brushed you aside on every swing and never missed a beat." |
| 8 | hatch_shut | cause:fall, place:belfry | hatch_open | verdict | "You fell from the bell frame onto the closed hatch. The gears below never noticed." |
| 9 | through | cause:fall, place:belfry, hatch_open | — | add body_in_gears | caption "You drop through the open hatch, straight into the gear train." |
| 10 | pulled_out | body_in_gears, watchman_in_gears | — | remove body_in_gears；verdict | "The watchman was right there, winding the clock. He pulled you out of the gears and set it going again." |
| 11 | jam | body_in_gears | — | add clock_stopped | caption "Brass teeth meet something that will not give. The great clock stops." |

- wish.all: `clock_stopped`
- 线索：round sheet「WATCHMAN'S ROUND — 9 PM Gear Room: check the lamp. 10 PM Belfry: open the hatch, grease the bell. 11 PM Pit: bail out the rainwater. Midnight Gear Room: wind the clock. Leave the hatch open till morning.」；gears「Solid brass. A dropped spanner once stopped it for a week.」；pendulum「Two hundred pounds of lead. It stops for nothing.」；minute hand「Thin copper. Not built to hold a man.」；hatch「Opens straight onto the gear train below.」

## 第 6 关 The Office（终局：打破规则）

- Deceased: DEATH
- Last Wish: "Find someone to cover my shift."
- 栏：Cause {burn, drown, fall, **retire "End of Shift"（shiftOnly）**} × Place {desk At His Desk, archive The Archive, door The Door} × Shift {day Day Shift, night Night Shift, graveyard Graveyard Shift}
- `shiftOnly: true` 的选项只在玩家**按住 Shift 键**时出现在下拉里。本关 Shift 键不做预览，而是让整张表"换挡"（纸张泛光，多出隐藏选项）。
- 解：retire + 任意地点 + 任意班次（9 种）

| # | id | when | 效果 | 文案 |
|---|---|---|---|---|
| 1 | no_burn | cause:burn | reject | "He has no flesh to burn. The robe is flame-retardant; he had it checked in 1666." |
| 2 | no_drown | cause:drown | reject | "No lungs. He once walked the Atlantic floor to deliver a notice." |
| 3 | no_fall | cause:fall | reject | "He does not fall. Things fall towards him." |
| 4 | retire | cause:retire | add shift_covered | caption "He reads the form twice. Then, very slowly, he hangs up the scythe." |

- wish.all: `shift_covered`
- 线索：time clock「Punched in: the beginning. Punched out: —. He has worked every shift there is.」；form footer「Form 1-D. Some options are reserved for shift supervisors.」；scythe「It has never once been put down.」；keyboard plaque（第 2 次退单后出现）「He glances at the oldest key on your keyboard.」
- 结尾：通知单 Successor 一栏自动写上 "YOU"，盖 APPROVED，字幕 "Your shift begins."，进入制作名单。
