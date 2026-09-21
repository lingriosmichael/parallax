#!/usr/bin/env python3
"""PAX-A05 one-off: trim A_Door_Exit raw to its opaque bounds and scale it to fit W x H, bottom-centre.
Default 177x295 (0.9 x 1.5 u at PPU 196.667); pass `W H` to override.

prepare_slot.py has no manifest slot for the door, so this reuses its object-mode
steps (bounds at alpha 25, uniform LANCZOS resize, bottom-centre canvas) without the manifest.
"""
import sys
from pathlib import Path
import numpy as np
from PIL import Image
sys.path.insert(0, str(Path(__file__).parent))
from prepare_slot import bounds, resize_uniform

ROOT = Path(__file__).resolve().parents[2]
RAW = ROOT / "Art_Source/Doors/A_Door_Exit__raw_v1.png"
OUT = ROOT / "Assets/_Game/Art/Doors/A_Door_Exit.png"
W, H = (int(sys.argv[1]), int(sys.argv[2])) if len(sys.argv) == 3 else (177, 295)

a = np.asarray(Image.open(RAW).convert("RGBA")).copy()
x0, y0, x1, y1 = bounds(a, 25)
scaled = resize_uniform(a[y0:y1, x0:x1], W, H)
canvas = np.zeros((H, W, 4), np.uint8)
x, y = (W - scaled.shape[1]) // 2, H - scaled.shape[0]
canvas[y:y + scaled.shape[0], x:x + scaled.shape[1]] = scaled
Image.fromarray(canvas, "RGBA").save(OUT)
print(f"raw={a.shape[1]}x{a.shape[0]} bbox=({x0},{y0},{x1},{y1}) opaque={x1-x0}x{y1-y0} "
      f"scale={min(W/(x1-x0), H/(y1-y0)):.6f} content={scaled.shape[1]}x{scaled.shape[0]} final={W}x{H}")
