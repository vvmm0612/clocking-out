# Clocking Out

*Death is overworked, so he hands you your own death notice. Fill it in: how, where, and on which shift. Make it count.*

A small paperwork-puzzle game made for **GitHub Game Off 2026** (theme: **SHIFT**). Built with Unity 2022.3 for WebGL.

## How to play

- Each level is a tiny diorama plus a death notice. Your name and your **Last Wish** are already printed on it.
- Hover over and click objects in the scene to read clues: rosters, timetables, signs, gauges.
- Fill in **Cause**, **Place** and (from level 2) **Shift** from the drop-downs, then submit.
- Before stamping, Death plays out what your choice would cause. If your death makes the wish come true, the form is **APPROVED**. If not, it is **REJECTED** with a reason, time rewinds, and you try again.
- There is no fail state. Getting it right on the first try earns a gold stamp.

## How we interpreted SHIFT

We read the theme four ways. **Work shifts:** the time field on the form is a shift, and NPCs follow rosters you find in the scene. **The Shift key:** hold it to look across to the other side and preview the world on the shift you selected (who is where, how high the water is), with your ghost standing at the chosen place. **Shifting the blame:** Death shifts his paperwork onto the dying, and in the end he shifts the whole job onto you. **End of Shift:** the last level is solved by an option that only appears while you hold Shift.

## Controls

| Input | Action |
|---|---|
| Mouse hover / click | Read clues, use the form |
| Hold **Shift** | Peek at the other side (preview the selected shift) |
| Click / Space | Speed up the playback |
| Esc | Skip the playback / close a dialog |

Desktop browsers only, mouse and keyboard required.

## Building

Requirements: Unity **2022.3.17f1c1** (any 2022.3 LTS editor with WebGL support should work) and Python 3 for the level validator. All scripts run Unity in batch mode, so the project must not be open in the editor at the same time. Set `$env:UNITY_EXE` if your editor is installed somewhere other than the default path in `tools/_unity.ps1`.

```powershell
# Compile all scripts (exit code 0 = no errors). Log: Logs/compile.log
powershell -ExecutionPolicy Bypass -File tools\compile.ps1

# Run EditMode tests. Results: TestResults/EditMode.xml
powershell -ExecutionPolicy Bypass -File tools\test.ps1

# Build WebGL to Builds/WebGL. Add -Subst if IL2CPP chokes on a non-ASCII repo path.
powershell -ExecutionPolicy Bypass -File tools\build-webgl.ps1

# Check every level file (solutions, verdicts, anchors). Also regenerates docs/combos.md
python tools\validate_levels.py
```

## Repository layout

```
Assets/
  Editor/               BuildScript (WebGL build), ProjectSetup (scenes, template materials)
  Resources/Levels/     level_01.json ... level_06.json (all puzzle data)
  Resources/Materials/  template materials so shaders ship in the build
  Scripts/Core/         DeathNotice.Core: level model, JSON loader, simulator (pure logic)
  Scripts/Game/         DeathNotice.Game: stage builder, notice form UI, playback, audio, flow
  Tests/EditMode/       DeathNotice.Tests.EditMode: brute-forces every level's combinations
docs/                   design, level specs, JSON schema, art/audio briefs, asset ledger
tools/                  compile / test / build scripts, level validator
```

Scenes and UI are built from code at runtime. Level content lives entirely in the JSON files.

## Credits

- Design, code and writing: [vvmm0612](https://github.com/vvmm0612)
- Art, music and sound: see [`docs/asset-ledger.csv`](docs/asset-ledger.csv) for each file's source, author and license.

## Use of AI tools

We used generative AI tools while making this game: for code, for in-game text, and for some of the images and music. Every asset made with an AI tool is marked in [`docs/asset-ledger.csv`](docs/asset-ledger.csv) (`ai_generated` column), with the tool used and the prompt or source where we have it. All of it was reviewed, edited and put together by us.

## License

- **Code** (everything under `Assets/Scripts`, `Assets/Editor`, `Assets/Tests` and `tools/`): MIT, see [LICENSE](LICENSE).
- **Art and audio assets** are *not* covered by the MIT license. Each one uses the license listed for it in [`docs/asset-ledger.csv`](docs/asset-ledger.csv).
