# itch.io page: Clocking Out

## Title

**Clocking Out**

## Tagline candidates

1. Death is swamped. Fill in your own death notice.
2. You can't dodge your death. You can choose your shift.
3. Pick the shift you die on. Make it count.

## Short description (< 140 characters)

> Death hands you your own death notice. Choose how, where and on which shift you go, so your last wish comes true.

(115 characters)

## Full page description

---

**Death is overworked, so he shifts the paperwork to you.**

You're due today. That part is settled. What's still blank on the form is *how*, *where*, and *on which shift*. Every notice comes with a Last Wish already printed on it: light the great lamp, turn the old mill wheel, make sure nobody ever reads the ledger. Pick a death that makes the wish come true and Death will stamp it **APPROVED**. Pick wrong and the form comes back **REJECTED**, with a note on why.

Six little dioramas. Rosters, timetables, gauges and grumpy night watchmen. One morbidly cheerful clerk who would really like to go home.

### How to play

1. Hover over and click objects in the scene to read clues.
2. Fill in **Cause**, **Place** and **Shift** on the notice.
3. Submit. Death plays out what your choice sets in motion.
4. If the wish comes true, you're approved. If not, time rewinds and you can try again. There's no game over.
5. Get it right first time for a gold "Filed without correction" stamp.

### Controls

- **Mouse**: read clues, fill in the form
- **Hold Shift**: peek at the other side and see the world on the shift you selected
- **Click / Space**: speed up the playback · **Esc**: skip it

Desktop browser with mouse and keyboard required.

### On the theme: SHIFT

We read the theme four ways. The time field on the form is a **work shift**, and the people in each scene follow their rosters. Holding the **Shift key** lets you look across to the other side and see who will be where on the shift you picked. Death is **shifting the blame**, passing his paperwork on to the dying. And the last puzzle is solved by an option that only appears while you hold Shift: **End of Shift**.

### Credits

- Design, code and writing: vvmm0612
- Art, music and sound effects: see the asset ledger in the source repository (`docs/asset-ledger.csv`) for every file's author, source and license.
- Source code (MIT): https://github.com/vvmm0612/clocking-out

### AI disclosure

We used generative AI tools while making this game: for code, for in-game text, and for some of the images and music. The asset ledger in the repository marks every AI-assisted asset and names the tool used. Everything was reviewed, edited and assembled by hand.

Made for GitHub Game Off 2026.

---

## Suggested tags (10)

`puzzle`, `logic`, `dark-humor`, `death`, `deduction`, `short`, `diorama`, `low-poly`, `point-and-click`, `game-off-2026`

(Also add the ones itch suggests automatically, e.g. `Singleplayer`, `Funny`, and set Genre to *Puzzle*.)

## Embed settings

| Setting | Value |
|---|---|
| Kind of project | HTML |
| Upload | zip of the contents of `Builds/WebGL` (`index.html` at the root of the zip), tick "This file will be played in the browser" |
| Embed options | Embed in page, manually set size |
| Viewport | **1280 × 720** |
| Fullscreen button | On |
| Automatically start on page load | Off (click to launch, so audio can start) |
| Mobile friendly | Off |
| Scrollbars | Off |
| SharedArrayBuffer support | Off (not needed) |
| Platforms note in description | "Desktop browsers only" |

Cover image: 630 × 500 (see `docs/art-brief.md`). Add 3–5 screenshots at 1280 × 720 and, if possible, a short GIF of a playback.

## 提交前检查清单

- [ ] 跑 `python tools\validate_levels.py` 全部通过，`tools\test.ps1` 全绿
- [ ] `tools\build-webgl.ps1` 成功，`Builds/WebGL` 里有 `index.html` 和 `Build/` 下的 data/wasm/framework/loader 四类文件
- [ ] 本地用静态服务器打开构建（不能双击 index.html），从头玩到制作名单
- [ ] 上传到 itch 后，在 Chrome、Firefox、Edge 里各通关一次第 1 关，确认 gzip 资源能加载（decompressionFallback 已开）
- [ ] 第一次点击后才有声音，BGM 循环没有断点，音量设置可用
- [ ] 按住 Shift 预览正常，连按 Shift 不会触发粘滞键干扰游戏（只在 Windows 上测一次）
- [ ] 第 6 关按住 Shift 能看到 "End of Shift"，结尾出现 "Your shift begins."
- [ ] 全屏按钮可用，退出全屏后画面比例正常
- [ ] 游戏内没有中文或缺字方块，没有调试信息（F1 调试解法在正式构建里关闭或隐藏）
- [ ] 页面：标题、标语、短简介、完整描述、标签、封面 630×500、截图 3–5 张都已填
- [ ] 页面勾选了 "仅桌面浏览器"，Mobile friendly 未勾选
- [ ] `docs/asset-ledger.csv` 每一个素材都有来源、授权、是否 AI 生成，空白行已补齐或删除
- [ ] README 和 itch 页面里的 AI 披露段落和台账一致
- [ ] GitHub 仓库是公开的，README、LICENSE 在根目录，Game Off 要求的仓库链接已填到 itch 提交表单
- [ ] 在 itch 的 Game Off 2026 jam 页面完成提交（不仅仅是发布游戏页面），截止前至少留 2 小时余量
- [ ] 页面和仓库里没有真实姓名或邮箱，作者统一写 vvmm0612
