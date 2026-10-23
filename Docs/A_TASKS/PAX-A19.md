# PAX-A19 · Geyser thermal-vent steam (D-162)

**Status:** Approved by the developer's pick (2026-10-22: "I like the 2D thermal Vent, so use this for all geyser vents from
now on"). Built on a batch-mode clone; the developer's menus on main are pending.
**Folder:** `Docs/A_TASKS/` · **Phase:** E, look and feel · **Implementer:** Claude Code (batch-mode clone)
**Phase 1 size:** Lite (one presenter, its builder step and importer, generated art; no gameplay change).
**Depends on:** PAX-A13 (trap art, parity), PAX-V08 (the hazards' unlit material), D-101 (body growth).

## 0. Why

The developer, 2026-10-21: "the art for the geyser air is also ugly and undeserving for a prmium game". Round 1 offered a water
jet, a steam plume and a hot spring over real captures. The developer liked the steam's idea but not its finish. Round 2 offered
2A sunlit billow, 2B pressure jet, 2C painted and 2D thermal vent, and they picked 2D.

## 1. Scope

- `Tools/Art/geyser_steam.py` (new): renders the steam, a sprite per tick, for Up and Down, into `Assets/_Game/Art/Geyser/` with
  `geyser_steam.json`. Deterministic, with `--check`.
- `Assets/_Game/Art/Geyser/**` (new, generated): 254 PNGs and the manifest. Unity writes their `.meta` files on import, and the
  import menu creates `GeyserSteam.spriteatlasv2`.
- `Assets/_Game/Presentation/GeyserArt.cs`: draws the vent, the jet (the column's body) and the tell, plume and fade
  frames from `TicksSinceFire`.
- `Assets/_Game/Presentation/TrapArtConfig.cs`: `GeyserSteamUp`/`GeyserSteamDown` (`GeyserSteamFrames`). `HasGeyser` requires them.
- `Assets/_Game/Core/Presentation/TrapArtMath.cs`: `GeyserFadeTicks` = 22.
- `Assets/_Game/Editor/Art/TrapArtSetup.cs` (`BuildGeyser`): builds the steam renderers (unlit), wires the frames, and removes
  PAX-A13's TellGlow, Tell_, Steam_, Burst_ and Spray_ children.
- `Assets/_Game/Editor/Art/TrapKitImport.cs`: imports the steam sprites, builds their own atlas and fills the config.
- `Assets/_Game/Tests/EditMode/TrapArtParityTests.cs`: the fade group's parity rule.
- `Assets/_Game/Tests/EditMode/TrapArtBudgetTests.cs`: steam complete, in its own atlas of at most 2048, and the body inside the
  column's box.
- `Docs/07_DECISIONS.md` (D-162), this ticket.

## 2. Out of scope

- The heat shimmer (it needs a 2D renderer setting and a distortion shader, a separate decision).
- The vent stone itself (the kit's carved vent stays).
- Geyser gameplay, timing and layouts.
- The Trap Lab scene (the developer's menu).

## 3. YOU — UNITY EDITOR (main)

1. Open the project and let it import `Assets/_Game/Art/Geyser/`.
2. `PARALLAX/Art/Import Trap Kit`.
3. `PARALLAX/Setup/Levels/Rebuild All Levels`, then save.
4. Optional: the Trap Lab setup menu, then save.
5. Play L018 (G_1 and the ceiling vent G_D) and L013.

Run Import Trap Kit **before** Rebuild All Levels. Until the config has the steam, `HasGeyser` is false and geysers build as
grey-box.

## 4. Done when

- Touched fixtures pass on the clone: TrapArtParityTests and TrapArtRevealFrameTests on L013, L018, L020, TrapLab8, 9 and 11;
  TrapArtBudgetTests; TrapArtBuildTests; TrapArtMathTests; GeyserRuntimeTests; GeyserMathTests.
- In-engine captures of every geyser match the approved mock-up.
- The developer has played it in the Editor and on the phone.
