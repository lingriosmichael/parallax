TASK: PAX-003b
TITLE: Code folder structure + assembly definitions
PHASE / GATE: Phase 1 (Foundation) · after Gate 1

OBJECTIVE
Create the code folders under `Assets/_Game` and one assembly definition (.asmdef) per code area,
so that Unity's compiler enforces the dependency rules from 02_ARCHITECTURE.md and CLAUDE.md.

CONTEXT
- Plan §5.1 (repository structure), CLAUDE.md "Assembly rules", D-010.
- Unity 6.3 LTS (6000.3.24f1), D-017. The project already has `Assets/_Game/Art`, `Core`, `Player`, `Scenes`.
- No C# scripts exist yet. This ticket creates structure only.

CLAUDE — CODE
Create these folders (if missing):
- `Assets/_Game/Gameplay/`
- `Assets/_Game/App/`
- `Assets/_Game/Editor/`
- `Assets/_Game/Data/`
- `Assets/_Game/Tests/EditMode/`

Create exactly these five files (allowed list; nothing else):

1. `Assets/_Game/Core/Parallax.Core.asmdef`
   - name `Parallax.Core`, rootNamespace `Parallax.Core`
   - references: none
   - all platforms, autoReferenced true

2. `Assets/_Game/Gameplay/Parallax.Gameplay.asmdef`
   - name `Parallax.Gameplay`, rootNamespace `Parallax.Gameplay`
   - references: `Parallax.Core` only
   - all platforms, autoReferenced true

3. `Assets/_Game/App/Parallax.App.asmdef`
   - name `Parallax.App`, rootNamespace `Parallax.App`
   - references: `Parallax.Core`, `Parallax.Gameplay`
   - all platforms, autoReferenced true

4. `Assets/_Game/Editor/Parallax.Editor.asmdef`
   - name `Parallax.Editor`, rootNamespace `Parallax.Editor`
   - references: `Parallax.Core`, `Parallax.Gameplay`
   - includePlatforms: `["Editor"]`, autoReferenced false

5. `Assets/_Game/Tests/EditMode/Parallax.Tests.EditMode.asmdef`
   - name `Parallax.Tests.EditMode`, rootNamespace `Parallax.Tests`
   - references: `UnityEngine.TestRunner`, `UnityEditor.TestRunner`, `Parallax.Core`, `Parallax.Gameplay`
   - includePlatforms: `["Editor"]`
   - overrideReferences: true, precompiledReferences: `["nunit.framework.dll"]`
   - defineConstraints: `["UNITY_INCLUDE_TESTS"]`, autoReferenced false

Use assembly **names** (not GUIDs) in `references`. Standard Unity .asmdef JSON format.

YOU — UNITY EDITOR
1. Before running Claude Code: the project is committed and `git status` is clean.
2. After Claude Code finishes: switch to Unity and wait for it to import (Unity creates the `.meta` files).
3. In Unity's Project panel, drag `Assets/_Game/Player` into `Assets/_Game/Gameplay`
   (must be done in Unity so its `.meta` file moves with it).
4. Click each .asmdef and check its references in the Inspector match this ticket.

DO NOT
- Do not create, edit, rename, move, or delete any `.meta` file.
- Do not move the `Player` folder (the developer does that in Unity).
- Do not create any `.cs` files, `Net/Fusion`, `DebugTools`, or any folder not listed.
- Do not reference Photon, Fusion, or any package not listed.
- Do not touch `.unity`, `.prefab`, `.asset`, `ProjectSettings/`, `Packages/`, or `Docs/` (other than nothing).
- Do not run git commands (the developer commits).

REQUIREMENTS
- Five .asmdef files exist at the paths above with exactly the listed references.
- `Parallax.Core` references nothing. `Parallax.Gameplay` references only `Parallax.Core`.
- Unity compiles with no red Console errors.

ACCEPTANCE TEST (Editor only)
1. Unity finishes importing; Console shows no red errors.
2. Project panel shows App, Art, Core, Data, Editor, Gameplay/Player, Scenes, Tests/EditMode under `_Game`.
3. Each .asmdef's Inspector shows the references listed above.
4. `git status` shows the new folders, the five .asmdef files, and their `.meta` files, and nothing else
   (plus the moved `Player` folder).

DELIVERABLE (from Claude)
Changed files (complete list) · one-sentence explanation per .asmdef · test steps ·
state clearly that compilation was not verified in Unity by Claude.
