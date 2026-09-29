"""PAX-V06 effect sheets as AutoSprite statics (ultra, 1 credit, no prompt template), on black like A13's T06.
Shares ../ledger.json and its budget with cats.py. Writes to ../_fx/<id>/.
  python3 fx.py <id>            generate one sheet (one retry allowed: run again)
"""
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
from autosprite_client import AutoSpriteClient  # noqa: E402
from cats import ROOT, load_ledger, save_ledger, reserve, now  # noqa: E402

TAIL = "Hand-painted 2D game effect, strict side view. Warm gold and cream, soft edges. No cyan, no text. On pure black #000000."

FX = {
    "FX01_dust_puffs": "Sheet of 6 separate soft dust puffs in one row, spaced apart: from a tiny puff growing to a big round cloud, then thinning into wisps. Seen from the side.",
    "FX02_skid_dust": "Sheet of 4 separate low dust streaks kicked back along the ground, spaced apart, from a short small streak to a long wide one. Soft and wispy.",
    "FX03_landing_ring": "Sheet of 4 separate flat ground dust clouds seen from the side, spaced apart: a low puff spreading out left and right, growing wider and fading.",
    "FX04_wall_dust": "Sheet of 4 separate vertical dust plumes, spaced apart, trailing downward as if brushed off a wall, from small to large. Soft and wispy.",
    "FX05_checkpoint_burst": "Sheet of 4 separate bursts of warm golden light, spaced apart: a small bright spark, a round flare with soft rays, a ring of sparks and tiny leaves flying out, fading sparkles.",
}


def main():
    fx_id = sys.argv[1]
    prompt = FX[fx_id] + " " + TAIL
    if len(prompt) > 300:
        raise SystemExit(f"{fx_id}: prompt is {len(prompt)} characters (max 300)")
    d = ROOT / "_fx" / fx_id
    d.mkdir(parents=True, exist_ok=True)
    ledger = load_ledger()
    key = f"FX/{fx_id}"
    tries = len(ledger["clips"].get(key, {}).get("calls", []))
    if tries >= 2:
        raise SystemExit(f"{key}: already generated twice (one retry per item).")
    reserve(ledger, key, 1, f"static v{tries + 1} (ultra)")
    client = AutoSpriteClient()
    result = client.create_asset(f"PARALLAX {fx_id}", prompt, quality="ultra", use_prompt_template=False)
    url = result.get("baseImageUrl") or client.asset(result["id"]).get("baseImageUrl")
    v = tries + 1
    (d / f"raw_v{v}.png").write_bytes(client.download(url))
    ledger = load_ledger()
    ledger["clips"][key]["calls"][-1].update({"status": "received", "assetId": result.get("id")})
    save_ledger(ledger)
    side_path = d / "sidecar.json"
    side = json.loads(side_path.read_text()) if side_path.exists() else {"id": fx_id, "kind": "static", "ticket": "PAX-V06"}
    side[f"v{v}"] = {"prompt": prompt, "endpoint": "POST /assets", "quality": "ultra", "usePromptTemplate": False,
                     "assetId": result.get("id"), "credits": 1, "created": now()}
    side_path.write_text(json.dumps(side, indent=2) + "\n")
    print(d / f"raw_v{v}.png", "session spent", ledger["spent"])


if __name__ == "__main__":
    main()
