# 美术需求（Clocking Out）

给负责美术的同学。管线：**概念图（图像生成工具）→ Tripo3D 图生 3D → Blender 精简、修轴心、上色 → 导出 fbx**。
游戏先用灰盒几何体搭好了 6 关的 diorama，正式模型**按锚点名自动替换**灰盒，所以文件名必须和锚点名一字不差。替换规则见第 2 节；每关的灰盒已导出为 `docs/greybox/<levelId>.obj`（Blender 比例和位置参照），每个锚点的原点、尺寸、行为列在 `docs/anchors.md`。

每产出一个文件，在 `docs/asset-ledger.csv` 登记一行（来源、工具、授权、是否 AI 生成、提示词）。

---

## 1. 统一风格规范

- **风格**：低多边形纸模 / 玩具感 diorama。像桌上摆的一块切开的小场景：边缘切得整齐，物件略微 Q 版（头大、比例粗壮），表面平涂，不要写实纹理。
- **着色**：三渲二。整块平涂色或 2–3 级色阶，不要法线贴图、不要 PBR 金属度/粗糙度贴图。优先用**顶点色或一张很小的调色板贴图**（例如 64×64，一个色块一种颜色），整关共用一张。
- **描边**：深色外描边，统一用每关调色板里的 `dark` 色。做法二选一，全项目统一：Blender 里做反向法线外壳（Solidify + Flip Normals，厚度约 0.01–0.015 单位），或者不做描边，等程序在 shader 里加。**开工前先确认用哪种**，默认用反向外壳。
- **面数**：单个物件 **≤ 1500 三角面**，整关所有模型合计 **≤ 20000 三角面**（WebGL 跑在浏览器里，越少越好）。小物件（告示、账本、印章类）控制在 200 面以内。Tripo3D 出来的模型一般要先 Decimate 到这个范围再手修。
- **比例**：按游戏内实际尺寸做，**1 单位 = Blender 里 1 m**。主角身高 **0.7 单位**（下文 H = 0.7）。程序**不做任何自动缩放**，模型多大游戏里就多大。
- **轴心（原点）**：**模型原点 = 锚点原点**。锚点原点是角色站立/特效发生的点，不一定在物件底部中心（例如 `dock` 的原点在栈桥末端，灰盒几何体相对它有偏移）。做法：把 `docs/greybox/<levelId>.obj` 导入 Blender，在对应物件的位置建模，然后把物体原点设到 `docs/anchors.md` 表里该锚点的"原点"坐标（Blender 里 X 取反，见下条），导出时模型相对原点的偏移就会原样保留。
- **朝向**：游戏镜头从 Unity 的 -Z 方向看向 +Z（物件"面向镜头的一面"在 -Z 侧），+Y 朝上。灰盒 OBJ 已按右手系写出（X 取反），Blender 默认设置（Forward -Z、Up Y）导入后，和游戏里看到的一样；模型照 OBJ 摆好朝向即可。导出 fbx：Apply Transform 打勾，缩放 1.0，Forward/Up 保持 Blender 默认（-Z / Y），导出后程序在游戏里检查一遍。
- **不带骨骼、不带动画**，所有物件和角色都是静态网格。会动的部分**拆成单独的子物体并按规则命名**（第 2 节"行为子节点"），程序按名字挂上旋转/开关/发光：`spin`（持续旋转：水车轮、齿轮、指针、摆锤、灯塔光束）、`toggle`（开关或铰链：信号臂、舱门、会被关掉/烧掉的物件）、`light`（点亮时发光：灯塔透镜）。
- **材质**：每个 fbx 最多 2 个材质。不要嵌入贴图到 fbx，贴图单独放 png。游戏会保留材质的颜色和主贴图，但统一换成工程自己的着色器（`Resources/Materials` 里的模板），所以 Blender 里的其他材质参数（金属度、法线、自发光等）不会生效。
- **不要透明玻璃**：灯罩、窗户用实色或留空洞，透明效果由程序加。
- **水面不做模型**：水位会随班次变化，水面由程序生成。只做水槽、池壁、水箱外壳。

### 每关调色板

六关同属一个世界：都用偏暖的纸色做主体（`paper`），同一种深灰紫做描边（`dark` 系），每关只换天色和强调色。下面的值和代码里灰盒用的颜色一致，可以微调，但调完告诉程序同步。

| 关卡 | 时段 / 情绪 | paper（主体） | wood（木） | secondary | accent（强调） | dark（描边/阴影） | glow / 特殊 |
|---|---|---|---|---|---|---|---|
| 1 The Lighthouse | 黄昏海边，暖 | `#F0EBE0` | `#8F694F` | `#9EB285` 苔绿 | `#D16154` 灯塔红 | `#52545C` | `#FFDB73` 灯光，天 `#F5CCAD` |
| 2 The Mill | 白天田野，清爽 | `#C7BDA8` | `#946E4C` | `#73859E` 石蓝 | `#BF734C` 锈橙 | `#4C4C52` | 草地 `#99B880`，天 `#BDDBDB` |
| 3 The Bakery | 凌晨室内，炉火暖 | `#EBDBC2` 面粉白 | `#996B4C` | `#B86B5C` 砖红 | `#F2B266` 炉火 | `#473D3D` | 天 `#4C527A` |
| 4 The Railway Halt | 深夜车站，冷 | `#B2A899` | `#85614C` | `#618C73` 铁路绿 | `#CC4C47` 信号红 | `#47474F` | 天 `#333D5C` |
| 5 The Clock Tower | 夜里钟楼，铜色 | `#B2A899` 石 | `#805C42` | `#D9AD5C` 黄铜 | `#667399` 夜蓝 | `#403D42` | 天 `#2E3352` |
| 6 The Office | 死神办公室，近黑 | `#736B80` | `#66473D` | `#D9D1BD` 文件纸 | `#73A680` 幽绿 | `#29262E` | 天 `#1F1C26` |

---

## 2. 文件命名与放置

```
Assets/Resources/Models/<levelId>/<anchor>.fbx   某个锚点物件（NPC 也是锚点）
Assets/Resources/Models/<levelId>/_set.fbx       整关布景：塔身、墙体、地面底座、树等所有不属于锚点的东西
Assets/Resources/Models/common/player.fbx        主角（Shift 预览里的幽灵用同一个模型，程序加半透明）
Assets/Resources/Models/common/death.fbx         死神（第 6 关死者是 DEATH，此时主角用这个模型；没有就用 player）
例：Assets/Resources/Models/level_01/lamp.fbx
    Assets/Resources/Models/level_02/_set.fbx
```

- `<levelId>` 是 `level_01` … `level_06`，`<anchor>` 是下表的锚点名，全小写、下划线，和 JSON 一字不差。也可以是同名的 prefab。
- 同名锚点在不同关是不同东西（例如 `sign`、`hatch`、`pit`、`lever`、`roster`），各自放在自己关的文件夹里。
- 贴图放同一文件夹，命名 `<levelId>_palette.png`。
- 美术同学不需要打开 Unity。把 fbx 交给程序，或者直接放进对应文件夹提交（fbx/png 走 Git LFS）。

### 模型替换规则（程序已实现，`Assets/Scripts/Game/ModelSwap.cs`）

- **只换外观**：锚点的位置、线索标记、悬停读线索、旋转/开关/点灯行为全部保留。该锚点下的灰盒几何体隐藏，程序按新模型的包围盒在锚点上重算一个盒碰撞体（用于鼠标悬停），模型自带的碰撞体不用。
- **原点对齐、不缩放**：模型原点放在锚点原点上，旋转归零，缩放 1。模型在哪、多大，完全由 fbx 本身决定（见第 1 节"比例""轴心"）。
- **`_set`**：存在时，该关所有不属于锚点的灰盒布景（灯塔塔身、磨坊墙、面包房墙体、钟楼外壳、地面底座、树……）全部隐藏，换成这个模型。它的原点放在世界原点 (0, 0, 0)，所以在 Blender 里直接照 OBJ 的 `_set` 组原位建模即可。主角坠落时会落到它的表面上，所以地面、楼板要是实体。
- **程序画的东西不要做**：OBJ 里 `_program` 组（水面、玻璃、水位刻度上的指针、灯塔光束）由程序绘制，替换后照样显示，模型里留出位置即可。
- **行为子节点**：锚点有旋转/开关/发光行为时（`docs/anchors.md` 的 spin / toggle / light 列），程序在模型里找对应名字的子物体：
  - `spin`：持续旋转的部件，绕**它自己的局部轴**转（水车、齿轮绕 Z，灯塔光束绕 Y，摆锤绕 Z 摆动），所以它的**原点要放在旋转中心**。多个部件用 `spin_1`、`spin_2`、`spin_3`（例如第 5 关三个齿轮，按名字顺序对应大、中、小齿轮的转速）。
  - `toggle`：开关部件。普通开关（`lantern`、`gate`、`ledger` 被关掉/烧掉）= 显示/隐藏它；铰链类（第 4 关 `signal_arm`、第 5 关 `hatch`）= 跟着程序的铰链一起转，所以它的原点要放在转轴上，默认姿势做成"关"（信号臂垂下、舱门合上）。
  - `light`：点亮时变成自发光的部件（第 1 关 `lamp` 的透镜）。
  - 没有对应子物体时，行为作用于整个模型（整个模型绕自己原点转、整个模型显隐、整个模型发光）。
- **主角**：`common/player` 做成单一浅色、站姿中性、脚底在原点上，高 0.7 单位（灰盒同高）。程序按关卡给它染色（保留贴图），死法演出会压扁、翻转、下沉它。

---

## 3. 逐关模型清单

优先级：**P0** = 解谜时玩家必须看清、认得出的物件（地点、线索物件、会动的机关）；**P1** = 氛围。
"地点"类锚点是主角站立/死亡的位置，模型是那一块小场景（平台、房间角落）。
下表的"建议尺寸"是早期按"主角 = 1 单位"写的，只表示比例关系；**实际尺寸以 `docs/anchors.md` 的灰盒包围盒和 OBJ 为准**（主角 0.7 单位）。

除了下表锚点，每关还有**不属于任何锚点的主体建筑**（灯塔塔身、磨坊房子、面包房墙体、站台雨棚、钟楼塔身、办公室墙地面），统一做进该关的 `_set`（见第 2 节）。

### level_01 The Lighthouse（黄昏，海边礁石上的灯塔）

| 锚点 | 是什么 | 建议尺寸 | 优先级 | 备注 |
|---|---|---|---|---|
| lamp | 塔顶大灯：灯座 + 巨大的菲涅尔透镜 | 1.2 × 1.2 × 1.2 | P0 | 点亮时透镜发光：透镜子物体名为 `light`；光束由程序画。若要做会转的灯罩，子物体名 `spin`（原点在转轴上） |
| lantern | 引火小提灯（一直亮着，岛上唯一的火） | 0.3 × 0.45 × 0.3 | P0 | 会被关掉（toggle off），要醒目，暖黄色 |
| gallery | 塔顶外廊：环形铁栏杆平台 | 直径 5 × 0.5 | P0 | 地点。要看得出"很高、能掉下去" |
| dock | 木栈桥码头 + 系缆桩 | 1 × 0.3 × 2.6 | P0 | 地点。伸进深水里 |
| beach | 卵石滩，几块大石头 | 2.4 × 0.4 × 3 | P1 | 地点 |

### level_02 The Mill（白天，水车磨坊和上游池塘）

| 锚点 | 是什么 | 建议尺寸 | 优先级 | 备注 |
|---|---|---|---|---|
| wheel | 老橡木水车（干裂） | 直径 2 × 厚 0.5 | P0 | 会动：轮子绕 Z 轴转，轮子子物体名 `spin`，原点在轮轴中心 |
| gate | 闸门（木板闸） | 0.8 × 1.2 × 0.15 | P0 | 会被打开（toggle off） |
| lever | 锈死的闸杆，在栈道下方 | 0.2 × 0.8 × 0.2 | P0 | 要看出锈、要能看出"上方栈道砸下来能砸到它" |
| sluice | 闸门上方的木栈道 | 1.5 × 0.2 × 0.6 | P0 | 地点。有高度 |
| pit | 水车下方的干涸轮坑 | 1.6 × 0.6 × 1.6 | P0 | 地点 |
| loft | 二楼粮仓，堆面粉袋 | 2 × 1 × 1.5 | P0 | 地点 |
| trapdoor | 粮仓地板上的活板门 | 0.6 × 0.05 × 0.6 | P0 | |
| gauge | 池塘水位标尺（刻度杆） | 0.1 × 1.2 × 0.1 | P0 | 刻度要清楚，水位由程序画 |
| dam_notice | 上游大坝的告示牌 | 0.6 × 1 × 0.1 | P0 | 牌面留空，不要烤文字 |
| sign | "禁止明火"警示牌 | 0.5 × 0.4 × 0.05 | P1 | 牌面画火焰加斜杠图形，不要字 |
| pond | 池塘的岸和池壁 | 3 × 0.4 × 2.5 | P1 | 只做岸，不做水面 |

### level_03 The Bakery（凌晨 4–6 点，面包房剖面）

| 锚点 | 是什么 | 建议尺寸 | 优先级 | 备注 |
|---|---|---|---|---|
| oven | 砖砌大烤炉，炉口发光 | 1.4 × 1.4 × 1 | P0 | 地点。炉门单独子网格 `oven_door` |
| store | 仓库：面粉袋、架子 | 2 × 1.2 × 1.5 | P0 | 地点 |
| attic | 阁楼地板 | 1.8 × 0.3 × 1.5 | P0 | 地点。有高度 |
| ledger | 厚账本，放在仓库桌上 | 0.4 × 0.15 × 0.3 | P0 | 会被烧掉（toggle off），要醒目 |
| vat | 和面水缸 | 0.8 × 0.8 × 0.8 | P0 | 只做缸，不做水面 |
| hatch | 阁楼地板上的舱口（正对仓库上方） | 0.6 × 0.05 × 0.6 | P0 | |
| door | 烤炉房和仓库之间关不严的门 | 0.6 × 1.3 × 0.1 | P0 | 做成半开的样子 |
| roster | 墙上的排班表板 | 0.5 × 0.6 × 0.05 | P0 | 纸面留空 |
| baker | 面包师（NPC） | 高约 1.15H | P0 | 白围裙、厨师帽、胖 |
| apprentice | 学徒（NPC） | 高约 0.9H | P0 | 瘦小，扛面粉袋 |
| sign | "禁止明火越过此处"牌 | 0.5 × 0.4 × 0.05 | P1 | 图形，不要字 |
| shop | 前店一角：柜台、面包篮 | 1.5 × 1 × 1 | P1 | 面包师 5 点在这里 |

### level_04 The Railway Halt（深夜，乡间小站，远处断桥）

| 锚点 | 是什么 | 建议尺寸 | 优先级 | 备注 |
|---|---|---|---|---|
| tower | 水塔（支架 + 木制水箱） | 1.4 × 3 × 1.4 | P0 | 地点。有高度 |
| tank | 水塔的水箱 + 浮子 | 1.2 × 1 × 1.2 | P0 | 浮子单独子网格 `float`；水位由程序画 |
| signal_arm | 水塔边的 WATER STOP 信号臂 | 0.8 × 0.15 × 0.05 | P0 | 会动：臂子物体名 `toggle`，原点在转轴，默认垂下，程序绕 Z 轴摆出 |
| box | 信号楼（高脚小木屋） | 1.2 × 2.3 × 1 | P0 | 地点。有高度 |
| lever | 道岔扳杆 | 0.2 × 0.7 × 0.2 | P0 | |
| platform | 站台（矮） | 3 × 0.6 × 1.2 | P0 | 地点 |
| brazier | 站台上的火盆 | 0.4 × 0.6 × 0.4 | P0 | 火由程序加 |
| express | 午夜快车（车头 + 1 节车厢） | 3 × 1.2 × 0.8 | P0 | 会被程序沿轨道移动 |
| freight | 停在侧线上的货车车厢 | 2 × 1 × 0.8 | P0 | |
| track | 主线轨道一段 | 8 × 0.1 × 0.8 | P0 | 可做成可平铺的一段 |
| siding | 侧线轨道 | 3 × 0.1 × 0.8 | P0 | |
| bridge | 断掉的铁路桥 | 2 × 1 × 1 | P0 | 要一眼看出断了 |
| timetable | 时刻表板 | 0.5 × 0.6 × 0.05 | P0 | 纸面留空 |
| rulebook | 规章手册（放在信号楼窗台） | 0.3 × 0.08 × 0.2 | P0 | |
| telegram | 电报纸条 | 0.25 × 0.02 × 0.15 | P0 | |
| roster | 排班表板 | 0.5 × 0.6 × 0.05 | P0 | |
| signalman | 信号员（NPC） | 高约 1.03H | P0 | 工装、帽子 |
| stationmaster | 站长（NPC） | 高约 1.09H | P0 | 制服、大檐帽、怀表链 |

### level_05 The Clock Tower（夜里 9 点到午夜，钟楼剖面）

| 锚点 | 是什么 | 建议尺寸 | 优先级 | 备注 |
|---|---|---|---|---|
| gears | 黄铜齿轮组 | 2 × 1.5 × 0.6 | P0 | 地点。会动：3 个齿轮各自单独子物体 `spin_1` `spin_2` `spin_3`（大、中、小），绕 Z 轴转，原点在各自轴心 |
| belfry | 钟楼顶层：大钟 + 钟架 | 1.8 × 1.8 × 1.5 | P0 | 地点。有高度 |
| hatch | 钟楼地板舱门（正对齿轮组） | 0.7 × 0.05 × 0.7 | P0 | 会开关：门板子物体名 `toggle`，原点在铰链（靠镜头一侧的边），默认合上 |
| face | 大钟面（外墙） | 直径 2 × 0.15 | P0 | 地点 |
| minute_hand | 细铜分针 | 0.1 × 0.9 × 0.03 | P0 | 会动：指针子物体名 `spin`，原点放在旋转中心，不是底部 |
| pendulum | 摆锤（铅锤 + 长杆） | 0.5 × 2.5 × 0.2 | P0 | 会动：摆锤子物体名 `spin`，原点放在顶部悬挂点 |
| pit | 摆锤坑，积着雨水 | 1.2 × 0.5 × 1.2 | P0 | 地点。只做坑 |
| round_sheet | 巡更表（钉在墙上） | 0.4 × 0.5 × 0.03 | P0 | 纸面留空 |
| watchman | 巡更人（NPC），提灯 | 高约 1.07H | P0 | 长大衣、提灯 |

### level_06 The Office（死神的办公室）

| 锚点 | 是什么 | 建议尺寸 | 优先级 | 备注 |
|---|---|---|---|---|
| desk | 死神的办公桌，文件堆成山 | 1.8 × 1 × 1 | P0 | 地点 |
| time_clock | 打卡钟（老式机械） | 0.5 × 0.8 × 0.3 | P0 | |
| scythe | 镰刀，靠在桌边 | 0.3 × 2 × 0.6 | P0 | |
| archive | 档案室：一排排抽屉柜 | 2 × 2 × 0.8 | P0 | 地点 |
| door | 门（上方挂 EXIT 风格灯箱，不要字） | 0.9 × 2 × 0.15 | P0 | 地点 |
| form_footer | 桌上那张表格的页脚特写物件（一叠表格） | 0.4 × 0.05 × 0.3 | P1 | |
| keyboard_plaque | 键盘形状的小铭牌（第 2 次退单后才出现） | 0.4 × 0.1 × 0.15 | P1 | 上面可以有一个突出的 Shift 键形状 |

---

## 4. 角色清单

全部是**不绑骨骼的静态小人**，一个姿势，Q 版两头身到三头身，没有手指，脸只要眼睛点和帽子能区分就够了。

| 角色 | 用在哪 | 尺寸 | 放置 | 说明 |
|---|---|---|---|---|
| 主角（将死之人） | 每关 | 1H（0.7 单位） | `common/player.fbx` | 通用小人，穿工作服；颜色由程序按关卡染色，所以**做成单一浅色**。同一个模型也用作半透明幽灵。原点在脚底中心 |
| 面包师 baker | 第 3 关 | 1.15H | `level_03/baker.fbx` | |
| 学徒 apprentice | 第 3 关 | 0.9H | `level_03/apprentice.fbx` | |
| 信号员 signalman | 第 4 关 | 1.03H | `level_04/signalman.fbx` | |
| 站长 stationmaster | 第 4 关 | 1.09H | `level_04/stationmaster.fbx` | |
| 巡更人 watchman | 第 5 关 | 1.07H | `level_05/watchman.fbx` | |
| 死神 | 第 6 关（死者是 DEATH 时替代主角） | 1.4H | `common/death.fbx` | 黑袍、骷髅脸、疲惫上班族气质（黑眼圈、领带夹在袍子外面也行）。原点在脚底中心 |

NPC 会被程序在锚点之间瞬移（Shift 预览时摆到不同位置），所以站姿要中性，脚底平。

---

## 5. 概念图提示词（英文，每关一条）

统一用等轴视角整关 diorama，先出概念图定配色和造型，再按物件拆开单独出"单物件、白底、正面三分之四视角"的图喂给 Tripo3D。

**通用后缀**（每条都加上）：
`isometric view, cut-away diorama on a floating square base, low-poly papercraft toy style, flat cel shading, thick dark outlines, chunky stylized proportions, soft studio lighting, clean background, no text, no letters`

1. **level_01**：`A tiny lighthouse on a rocky islet at dusk, red-and-white striped tower with an iron gallery railing at the top, a giant unlit fresnel lamp, a small glowing pilot lantern beside it, a wooden dock reaching into deep blue water, a pebble beach, warm peach sky, palette cream #F0EBE0, lighthouse red #D16154, moss green #9EB285, wood brown #8F694F, lamp yellow #FFDB73`
2. **level_02**：`An old countryside water mill in daylight, a large dry oak water wheel beside a stone wheel pit, a wooden sluice gate with a rusty lever under a narrow catwalk, an upstream pond with a depth gauge pole, a grain loft full of flour sacks with a trapdoor, a notice board by the path, pale teal sky, palette beige #C7BDA8, wood #946E4C, slate blue #73859E, rust orange #BF734C, grass #99B880`
3. **level_03**：`A cut-away village bakery before dawn, a glowing brick oven room with a big dough water vat, a storeroom of flour sacks with a thick ledger on a table, a half-open door between the rooms, an attic with a floor hatch above the storeroom, a staff roster pinned to the wall, a small shop front counter, deep indigo night outside, palette flour white #EBDBC2, brick red #B86B5C, oven glow #F2B266, wood #996B4C`
4. **level_04**：`A lonely railway halt at midnight, a low platform with a burning brazier, a tall signal box on stilts, a wooden water tower with a float and a WATER STOP signal arm, a points lever, a siding with parked freight wagons, a main line leading to a collapsed bridge, a steam express approaching, cold navy sky, palette stone #B2A899, railway green #618C73, signal red #CC4C47, wood #85614C, night #333D5C`
5. **level_05**：`A cut-away stone clock tower at night, a bronze bell in the belfry with a floor hatch, a brass gear train below it, a long lead pendulum swinging over a pit of rainwater, a huge clock face with a thin copper minute hand on the outer wall, a watchman's round sheet on the wall, dark blue sky, palette stone #B2A899, brass #D9AD5C, night blue #667399, wood #805C42`
6. **level_06**：`The cramped office of an overworked Grim Reaper, a desk buried in paperwork, a scythe leaning against it, an old mechanical punch time clock on the wall, rows of archive filing cabinets fading into darkness, a plain door with a glowing sign box, near-black purple room with ghostly green accents, palette grey violet #736B80, paper #D9D1BD, ghost green #73A680, dark #29262E`

单物件提示词模板：`<物件描述>, single object, low-poly papercraft toy style, flat colors, front three-quarter view, plain white background, no text`

---

## 6. 2D UI 贴图清单

全部 **PNG、透明背景**。**不要把文字烤进图里**（游戏里的文字都由程序排版），只有印章上的字是例外。放在 `Assets/Resources/UI/`（目录名待程序确认）。

| 文件名 | 内容 | 尺寸 | 说明 |
|---|---|---|---|
| `notice_paper.png` | 死亡通知单纸张：泛黄公文纸，细边框、表格线、角上一个空白印章区 | 1024 × 1400 | 不要任何字，栏位名由程序写 |
| `stamp_approved.png` | 红色橡皮章 "APPROVED" | 512 × 256 | 带墨迹不均、边缘斑驳，略倾斜 |
| `stamp_rejected.png` | 红色或深紫橡皮章 "REJECTED" | 512 × 256 | 同上 |
| `seal_gold.png` | 金色圆形封印（评价章，1 次通过） | 256 × 256 | 可带 "FILED WITHOUT CORRECTION" 环形字 |
| `seal_silver.png` | 银色圆形封印（2–3 次） | 256 × 256 | 和金章同一造型 |
| `seal_paper.png` | 纸质圆形贴章（更多次） | 256 × 256 | 和金章同一造型，素色 |
| `certificate.png` | 死亡证明底图：比通知单更正式，花边框、顶部骷髅或沙漏徽记 | 1400 × 1000 | 横版，不要字 |
| `cover.png` | itch 封面 | **630 × 500** | 这张不需要透明，可以放游戏名（名字可后期叠字） |

---

## 7. 建议的制作顺序

1. **先只做第 1 关，走通整条管线**：`lantern`（最小）→ `lamp`（带会动的子网格）→ `gallery` / `dock` / `beach`。交给程序放进 `Assets/Resources/Models/level_01/` 跑一次构建，确认比例、朝向、轴心、描边、面数、WebGL 包体都没问题。
2. 根据第 1 关的问题修正规范，然后按优先级批量做：**所有关的 P0 → 角色 → UI 贴图 → P1**。
3. 每关做完先截一张游戏内截图对照概念图，再做下一关。
4. 11/29 功能冻结前优先保证第 1–3 关和第 6 关的 P0 到位；第 4、5 关可能被砍（见 design.md 第 6 节），放最后。

---

## 8. 已确认（原"待程序 / 设计确认"）

- **主角高度**：0.7 单位，模型按实际尺寸做，程序不缩放（第 1 节）。
- **主角和死神**：`Models/common/player`、`Models/common/death`（第 2 节）。
- **主体建筑**：每关 `Models/<levelId>/_set`（第 2 节）。
- **轴心**：模型原点 = 锚点原点，位置参照 `docs/greybox/<levelId>.obj` 和 `docs/anchors.md`（第 1、2 节）。
- **会动的锚点**：替换后行为保留，按子物体名 `spin` / `toggle` / `light` 挂接（第 2 节"行为子节点"）。