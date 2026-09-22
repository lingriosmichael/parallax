# PAX-A06 — AutoSprite REST art pipeline foundation

**Status:** Approved by the developer on 2026-09-22
**Phase:** Art pipeline test (D-031); solo Reality A first

## Objective

Connect the local art tooling to AutoSprite's REST API without placing a secret in the repo or
shipping API code in the Unity player. Given a prompt and a target slot, Codex can generate a
candidate, download it, check its size and silhouette against the game scale, and keep a record
of attempts. The developer's maximum spend is 20 credits per asset, including retries.

This ticket creates the reusable tooling and tests it offline. Live generation and approval of
a new Unity asset require the developer's API key and an asset-specific art ticket. The later
agent skill is written after a live asset has proved the full loop.

## Context and constraints

- D-031 allows a small placeholder-quality art pipeline test; full art production remains gated.
- Existing gameplay/object art uses 196.667 PPU; the A02 manifest defines varied pixel targets,
  pivots, and source provenance. Cat sheets use 256 px cells and the existing CatSpriteImporter.
- `Docs/Art/Reference/` and the existing `Assets/_Game/Art/` images are visual references.
- The old PAX-A02 Stage 2c statement that AutoSprite had no API described that session. Its REST
  API now documents static assets, characters, spritesheets, jobs, and sheet regeneration.
- Do not change scenes, prefabs, `.meta` files, gameplay code, or Unity packages.

## Allowed files

- Create `Docs/A_TASKS/PAX-A06.md`.
- Modify `CLAUDE.md` to record this workflow (developer follow-up, 2026-09-22).
- Create `Docs/Art/AutoSprite_workflow.md`.
- Create `Tools/Art/autosprite_client.py`.
- Create `Tools/Art/autosprite_pipeline.py`.
- Create `Tools/Art/tests/test_autosprite_pipeline.py`.
- Generated diagnostic files only under `/private/tmp` during this ticket.

## Requirements

1. Read the API key only from `AUTOSPRITE_API_KEY`; never print, persist, or commit it. Use
   `x-api-key` authentication. A placeholder is documented but no credential is needed for dry
   runs and tests.
2. Support account balance, prompt-based static asset generation, character creation, animation
   generation, job polling, spritesheet download, and free sheet regeneration. Respect 429
   `Retry-After` and report API errors with their code and message. Avoid logging signed URLs.
3. A target spec records asset type, prompt, output dimensions, PPU, pivot, path, and any existing
   manifest slot. Derive the target from the A02 manifest when possible; require dimensions for
   new slots. Keep generated raw and candidate files outside `Assets/`.
4. A local per-asset ledger caps estimated paid calls at 20 credits across retries. Estimate
   conservatively before any paid POST, record the result, and stop if the next attempt would
   exceed the cap. Do not use redos with ambiguous pricing automatically.
5. Validate image format, dimensions, nonempty alpha, foreground aspect, and phone-scale fill.
   Make a contact sheet for review. Never overwrite a Unity asset from this ticket.
6. Cover request shape, cost gates, API error handling, download/validation, and resumption with
   offline tests using a fake HTTP transport. Run those tests and a dry run against existing art.

## Manual acceptance

- **Editor:** No scene or Unity package change. In a later art ticket, import the approved PNG
  with the existing importer and inspect it at phone scale in Play mode.
- **Device:** Later ticket only; confirm silhouette and legibility on Android.
- **Two devices:** Not applicable to solo v1 art tooling.

## Deliverable

Report the local test result, a sample dry run, exact file changes, and what remains unverified
without a real key. Use the required `CLAUDE.md` output format.
