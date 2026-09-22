# AutoSprite art workflow (PAX-A06)

This is local art tooling, not game runtime code. The developer supplies the art prompt. Codex
reads the relevant ticket, compares `Docs/Art/Reference/` and existing in-game art, derives
target size and pivot, runs the generator, inspects the contact sheet, and adjusts the prompt or
source until the candidate fits. A candidate is copied into `Assets/` only in an approved
asset-specific ticket and then imported through the existing Unity art setup.

The reference for warm solo art is `Docs/Art/Reference/styleframe_A_v1.png`, alongside the
door reference and existing `Assets/_Game/Art/RealityA/` assets. A02 environment slots have
exact targets in `Docs/Art/A02_asset_manifest.md`: gameplay objects use 196.667 PPU and
backgrounds use 98.333 PPU. New slots need an explicit world-space size, PPU, pixel size,
pivot, and output path in their ticket. The image processor never changes collider sizes.

## Setup

Use Python 3 with the existing `Tools/Art/requirements.txt` environment (Pillow is required).
Set `AUTOSPRITE_API_KEY` in the local shell or secret manager. The placeholder is
`your_key_here`; it is not a working key. Never put the real key in a spec, log, or git commit.
The [REST API](https://www.autosprite.io/docs/api) uses `x-api-key` and requires a paid plan
for generation. The [credit reference](https://www.autosprite.io/docs/api-overview) currently
lists one credit for a static asset, one or three for a prompt-based character, and at least
five per animation. The tool reserves credits before each paid request and refuses to pass
20 reserved credits for a single target, including uncertain requests and retries.

## Static object spec

Codex writes a temporary JSON spec for each asset. For an existing A02 slot, `slot` supplies
the size, PPU, pivot, and proposed Unity path:

```json
{
  "type": "static",
  "slot": "A_OBJ_Checkpoint.png",
  "name": "Warm checkpoint post",
  "prompt": "Side-view sandstone checkpoint post, simple readable silhouette, warm ochre stone, small rust-red banner, single carved leaf emblem, isolated on white, no ground or text.",
  "quality": "turbo"
}
```

This example describes an existing asset for tooling demonstrations; PAX-A06 does not replace
it. For a new slot, replace `slot` with `target`:

```json
"target": {
  "width": 236,
  "height": 315,
  "ppu": 196.667,
  "pivot": "bottom centre",
  "path": "Assets/_Game/Art/RealityA/Environment/A_OBJ_New.png"
}
```

The static API prompt has a **300-character limit**. Codex compresses the style direction to fit
while preserving the requested object, view, proportions, palette, and clean background. The
API's default object template uses white; the processor also accepts transparent or flat
magenta backgrounds. An irregular opaque background fails processing and calls for a revised
prompt or manual review.

```sh
python3 Tools/Art/autosprite_pipeline.py plan --spec /private/tmp/asset-spec.json
python3 Tools/Art/autosprite_pipeline.py account --spec /private/tmp/asset-spec.json
python3 Tools/Art/autosprite_pipeline.py static --spec /private/tmp/asset-spec.json --execute
```

Candidates, raw images, contact sheets, and the credit ledger land under
`Art_Source/AutoSprite/<target-name>_<path-hash>/`, outside Unity's `Assets/`. Running `static`
again creates another version and spends another credit. If an API creation succeeded but its
download failed, the next run retrieves that saved asset and does not create a paid duplicate.
`prepare --spec ... --raw /path/to/existing.png` runs the same size checks offline and costs
no credits.

The checks require a readable image, visible foreground, a plausible aspect ratio, at most
2× upscaling, exact target pixel dimensions, and nonempty alpha. The output prints target
world units and flags moderate aspect differences for review. Codex checks the contact sheet
against the references, the object silhouette at phone scale, orientation, palette, fringe,
and whether the pivot puts it on the intended surface. These visual checks still need human
judgment. The tool does not copy the candidate into Unity or replace an existing asset.

## Animated character spec

An existing AutoSprite character ID can be used with a spec such as:

```json
{
  "type": "animation",
  "character_id": "char_example",
  "animations": [{"kind": "walk"}],
  "video_tier": "turbo",
  "frame_count": 10,
  "frame_size": 256,
  "target": {
    "width": 256,
    "height": 256,
    "ppu": 196.667,
    "pivot": "bottom centre",
    "path": "Assets/_Game/Art/Cats/CatA/CatA_Walk.png"
  }
}
```

`character --execute` creates a prompt-based character from a `type: character` spec with
`name`, `prompt`, and the same target block. `animation --execute` requests animation jobs,
polls them, downloads PNG sheets plus JSON atlases, and verifies the cell grid. To resume a
completed job or fetch a sheet after free regeneration, run `sheet --sheet-id ss_...`.
`regenerate --sheet-id ss_... --execute` changes frame size/count from the same source video
without another generation charge, according to the
[spritesheet API](https://www.autosprite.io/docs/api-spritesheets). Only soundless `turbo` and
`pro` animation tiers are budgeted. Redo operations are excluded because their price can
depend on the plan's remaining free allowance.

For Cat A, keep the existing code-driven flipbook: no Animator or `.anim` files. The
`CatSpriteImporter` owns 256 px slicing and shared Walk PPU/pivot. A later art ticket must
verify frame count, clipped tails, paws, and in-game readability before importing a new sheet.

## Live validation and skill handoff

PAX-A06 validates the client with offline fake responses and existing images. After the real
key is supplied, run one approved solo Reality A asset through create, download, processing,
contact-sheet review, Unity import, and Editor/device checks. Record actual prompt edits,
dimensions, failures, and credit use. Once that loop works, turn it into a Codex skill with
those tested steps.
