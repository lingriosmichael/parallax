#!/usr/bin/env python3
"""PAX-A02 deterministic art slot processor."""
import argparse, datetime, re
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage
from scipy.spatial import cKDTree

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "Docs/Art/A02_asset_manifest.md"
KEYED = {"strip", "edge", "hazard", "object"}

def info(slot):
    lines = MANIFEST.read_text().splitlines()
    for i, line in enumerate(lines):
        if line.startswith(f"| {slot} |"):
            fields = [x.strip() for x in line.strip().strip("|").split("|")]
            m = re.search(r"(\d+) × (\d+)", fields[6]); return lines, i, fields, int(m.group(1)), int(m.group(2))
    raise ValueError(f"slot not in manifest: {slot}")

def key_colour(a):
    h,w=a.shape[:2]; border=np.concatenate((a[:8,:,:3].reshape(-1,3),a[-8:,:,:3].reshape(-1,3),a[:, :8,:3].reshape(-1,3),a[:, -8:,:3].reshape(-1,3)))
    near=np.linalg.norm(border.astype(float)-[255,0,255],axis=1)<90
    if near.mean()<.60: raise ValueError("fewer than 60% of border pixels qualify as key colour")
    return np.median(border[near],axis=0)

def key_foreground(a):
    colour=key_colour(a); rgb=a[...,:3].astype(float); distance=np.linalg.norm(rgb-colour,axis=2); m=np.minimum(rgb[...,0],rgb[...,2])-rgb[...,1]
    background=distance<18; core=(distance>120)&(m<=60)&~background; fringe=~background&~core
    if not core.any(): raise ValueError("no core foreground pixels")
    coordinates=np.argwhere(core); tree=cKDTree(coordinates); fringe_coordinates=np.argwhere(fringe); alpha=np.zeros(distance.shape,float); alpha[core]=1
    global_median=np.median(rgb[core],axis=0)
    nearest_distance, nearest_indices=ndimage.distance_transform_edt(~core, return_indices=True)
    for y,x in fringe_coordinates:
        nearby=tree.query_ball_point((y,x),32); foreground=rgb[coordinates[nearby,0],coordinates[nearby,1]] if nearby else global_median[None,:]
        nearest_y,nearest_x=nearest_indices[:,y,x]
        f=rgb[nearest_y,nearest_x] if nearest_distance[y,x]<=8 else np.median(foreground,axis=0)
        vector=f-colour; alpha[y,x]=np.clip(np.dot(rgb[y,x]-colour,vector)/max(np.dot(vector,vector),1e-6),0,1)
    alpha*=a[...,3]/255; unmixed=np.where(alpha[...,None]>.02,(rgb-(1-alpha[...,None])*colour)/np.maximum(alpha[...,None],.02),0)
    a[...,:3]=unmixed.clip(0,255).astype(np.uint8);a[...,3]=np.round(alpha*255).astype(np.uint8);a[a[...,3]<=5,:3]=0
    return a,colour,tuple(100*mask.mean() for mask in (background,core,fringe))

def hazard_fade(a):
    start=round(a.shape[0]*.55); count=a.shape[0]-start
    t=np.linspace(0,1,count); smooth=t*t*(3-2*t)
    alpha=a[start:,:,3].astype(float)*(1-smooth[:,None]); a[start:,:,3]=np.round(alpha).astype(np.uint8)
    return a

def bounds(a, threshold=0):
    y,x = np.where(a[...,3] > threshold)
    if not len(x): raise ValueError("raw has no opaque content")
    return x.min(),y.min(),x.max()+1,y.max()+1

def surface_row(a):
    found = np.where((a[...,3] > 127).mean(axis=1) >= .60)[0]
    if not len(found): raise ValueError("no surface row with 60% opacity")
    return int(found[0])

def base_row(a):
    rows = np.where((a[..., 3] > 127).mean(axis=1) >= .05)[0]
    if not len(rows):
        raise ValueError("no base row with 5% opacity")
    return int(rows[-1])

def aspect_crop(a, aspect, bottom=False):
    h,w = a.shape[:2]; cw,ch = (round(h*aspect),h) if w/h > aspect else (w,round(w/aspect))
    x=(w-cw)//2; y=(h-ch)//2
    if bottom: y=min(max(0, base_row(a) + 1 - ch),h-ch)
    return a[y:y+ch,x:x+cw]

def ground_strip(a):
    """Extend each visible base column to the bottom without changing its alpha."""
    result = a.copy()
    visible = result[..., 3] > 127
    for x in range(result.shape[1]):
        ys = np.where(visible[:, x])[0]
        if len(ys):
            result[ys[-1] + 1:, x] = result[ys[-1], x]
    return result

def resize_uniform(a, maxw, maxh):
    image=Image.fromarray(a,"RGBA"); scale=min(maxw/image.width,maxh/image.height)
    return np.asarray(image.resize((round(image.width*scale),round(image.height*scale)),Image.Resampling.LANCZOS)).copy()

def seam(src, width, axis="x"):
    band=round(.12*width)
    if (src.shape[1] if axis=="x" else src.shape[0]) < width+band: raise ValueError("raw band too short")
    src=src.astype(float); src[...,:3] *= src[...,3:4]/255
    out=(src[:,:width] if axis=="x" else src[:width,:]).copy()
    for i in range(band):
        t=i/max(1,band-1)
        if axis=="x": out[:,i]=np.round(src[:,width+i]*(1-t)+src[:,i]*t)
        else: out[i]=np.round(src[width+i]*(1-t)+src[i]*t)
    alpha=out[...,3:4]; out[...,:3]=np.where(alpha>0,out[...,:3]*255/np.maximum(alpha,1),0)
    return out.clip(0,255).astype(np.uint8)

def seam_ratio(a, axis="x"):
    signed=a.astype(np.int16); alpha=a[...,3]
    if axis=="x": mask=(alpha[:,:-1]>25)|(alpha[:,1:]>25); dif=np.abs(signed[:,1:]-signed[:,:-1]).mean(axis=2); wrap_mask=(alpha[:,-1]>25)|(alpha[:,0]>25); wrap_dif=np.abs(signed[:,-1]-signed[:,0]).mean(axis=1)
    else: mask=(alpha[:-1,:]>25)|(alpha[1:,:]>25); dif=np.abs(signed[1:]-signed[:-1]).mean(axis=2); wrap_mask=(alpha[-1,:]>25)|(alpha[0,:]>25); wrap_dif=np.abs(signed[-1]-signed[0]).mean(axis=1)
    if mask.mean()<.01 or wrap_mask.mean()<.01:return None,0,1
    interior=max(float(np.percentile(dif[mask],95)),1.0); wrap=float(np.mean(wrap_dif[wrap_mask])); return wrap/interior,wrap,interior

def process(a, mode, w, h, pivot, grounded=False):
    band=round(.12*w)
    if mode=="sky": crop=aspect_crop(a,(w+band)/h)
    elif mode=="strip": crop=aspect_crop(a,(w+band)/h,True)
    elif mode=="material":
        side=min(a.shape[:2]); crop=a[(a.shape[0]-side)//2:(a.shape[0]+side)//2,(a.shape[1]-side)//2:(a.shape[1]+side)//2]
    elif mode in {"edge","hazard"}:
        first=surface_row(a); rows=np.where((a[...,3]>12).mean(axis=1)>=.05)[0]; x0,_,x1,_=bounds(a,12); crop=a[first:rows[-1]+1,x0:x1]
        scaled=resize_uniform(crop,100000,h)
        if scaled.shape[1]<w+band: raise ValueError("raw band too short")
        start=(scaled.shape[1]-w-band)//2; result=seam(scaled[:,start:start+w+band].astype(float),w); return hazard_fade(result) if mode=="hazard" else result
    elif mode=="object":
        # The keyer can leave sub-visible alpha in a colour-gradient backdrop.
        # Object crops use the same visible-alpha threshold as validation so that
        # this residue never expands an object's content bounds.
        x0,y0,x1,y1=bounds(a,25); scaled=resize_uniform(a[y0:y1,x0:x1],w,h); canvas=np.zeros((h,w,4),np.uint8); x=(w-scaled.shape[1])//2; y=0 if pivot=="top centre" else h-scaled.shape[0] if pivot=="bottom centre" else (h-scaled.shape[0])//2; canvas[y:y+scaled.shape[0],x:x+scaled.shape[1]]=scaled; return canvas
    else: raise ValueError("unknown mode")
    if mode=="material":
        source=np.asarray(Image.fromarray(crop,"RGBA").resize((w+band,h+round(.12*h)),Image.Resampling.LANCZOS)); return seam(seam(source.astype(float),w),h,"y")
    source=np.asarray(Image.fromarray(crop,"RGBA").resize((w+band,h),Image.Resampling.LANCZOS))
    if mode == "strip" and grounded:
        source = ground_strip(source)
    return seam(source.astype(float),w)

def validate(a, mode, w, h, key=None):
    if a.shape[:2] != (h,w): raise ValueError("wrong output size")
    ratio=seam_ratio(a)[0]
    if mode in {"sky","strip","edge","hazard","material"} and ratio is not None and ratio>1.25: raise ValueError("horizontal seam wrap exceeds 1.25 × p95")
    ratio_y=seam_ratio(a,"y")[0]
    if mode=="material" and ratio_y is not None and ratio_y>1.25: raise ValueError("vertical seam wrap exceeds 1.25 × p95")
    if mode in {"edge","hazard"} and (a[0,:,3]>127).mean()<.60: raise ValueError("surface row is not 60% opaque")
    if mode in KEYED:
      visible=a[...,3]>25; transparent=a[...,3]<25; edge=np.zeros_like(visible)
      for dy in range(-3,4):
       for dx in range(-3,4):
        if dx or dy: edge[max(0,dy):a.shape[0]+min(0,dy),max(0,dx):a.shape[1]+min(0,dx)] |= transparent[max(0,-dy):a.shape[0]-max(0,dy),max(0,-dx):a.shape[1]-max(0,dx)]
      if key is not None:
       rgb=a[...,:3].astype(float); selected=visible&edge
       if selected.any() and (np.linalg.norm(rgb-key,axis=2)[selected]<18).any(): raise ValueError("magenta residue remains")

def processing_metadata(a, mode, w, h):
    """Return the crop and uniform scale used by the mode, for dry-run evidence."""
    band=round(.12*w); source_h,source_w=a.shape[:2]
    if mode in {"sky","strip"}:
        aspect=(w+band)/h
        crop_w,crop_h=(round(source_h*aspect),source_h) if source_w/source_h>aspect else (source_w,round(source_w/aspect))
        crop_x=(source_w-crop_w)//2; crop_y=(source_h-crop_h)//2
        if mode=="strip": crop_y=min(max(0,base_row(a)+1-crop_h),source_h-crop_h)
        return f"({crop_x},{crop_y},{crop_x+crop_w},{crop_y+crop_h})",(w+band)/crop_w,None
    if mode=="material":
        side=min(source_w,source_h); x=(source_w-side)//2; y=(source_h-side)//2
        return f"({x},{y},{x+side},{y+side})",w/side,None
    if mode in {"edge","hazard"}:
        row=surface_row(a); rows=np.where((a[...,3]>12).mean(axis=1)>=.05)[0]; x0,_,x1,_=bounds(a,12)
        return f"({x0},{row},{x1},{rows[-1]+1})",h/(rows[-1]+1-row),row
    x0,y0,x1,y1=bounds(a,25)
    return f"({x0},{y0},{x1},{y1})",min(w/(x1-x0),h/(y1-y0)),None

def seam_text(a, axis):
    ratio,wrap,p95=seam_ratio(a,axis)
    return "skipped (edges transparent)" if ratio is None else f"wrap={wrap:.3f} p95={p95:.3f} ratio={ratio:.3f}"

def sheet(raw, processed, slot, mode):
    def preview(image): image=image.copy(); image.thumbnail((260,180),Image.Resampling.LANCZOS); return image
    raw,processed=preview(raw),preview(processed); out=Image.new("RGBA",(1120,620),(120,120,120,255)); out.alpha_composite(raw,(20,50)); out.alpha_composite(processed,(310,50))
    checker=Image.new("RGBA",(260,180),(170,170,170,255)); d=ImageDraw.Draw(checker)
    for y in range(0,180,16):
      for x in range(0,260,16):
       if (x//16+y//16)%2:d.rectangle((x,y,x+15,y+15),fill=(110,110,110,255))
    checker.alpha_composite(processed,((260-processed.width)//2,(180-processed.height)//2)); out.alpha_composite(checker,(600,50)); tile=Image.new("RGBA",(780,330),(120,120,120,255))
    for y in range(3 if mode=="material" else 1):
      for x in range(3): tile.alpha_composite(processed,(x*processed.width,y*processed.height))
    out.alpha_composite(tile,(20,270)); d=ImageDraw.Draw(out); d.text((20,15),f"{slot}: raw | grey | checker | tiled",fill="white")
    if mode in {"edge","hazard"}:d.line((310,50,310+processed.width,50),fill="red")
    path=Path.home()/"Desktop/PAX-A02_contact"/slot; path.parent.mkdir(parents=True,exist_ok=True); out.save(path); return path

def write_provenance(slot, raw, prompt, haze, tint, tint_strength):
    lines,row,fields,_,_=info(slot)
    haze_note = f" · haze {haze:g}" if haze else ""
    tint_note = f" · tint {tint.upper()} {tint_strength:g}" if tint else ""
    fields[9]=f"{raw} · {prompt} · {datetime.date.today().isoformat()}{haze_note}{tint_note}"
    lines[row]="| "+" | ".join(fields)+" |"
    MANIFEST.write_text("\n".join(lines)+"\n")

def haze_colour(path):
    sky = np.asarray(Image.open(path).convert("RGBA"))
    opaque = sky[..., 3] > 0
    if not opaque.any():
        raise ValueError("haze source has no opaque pixels")
    return sky[..., :3][opaque].mean(axis=0)

def apply_haze(a, haze, colour):
    if haze == 0:
        return a
    result = a.copy()
    visible = result[..., 3] > 0
    rgb = result[..., :3].astype(float)
    rgb[visible] = rgb[visible] * (1 - haze) + colour * haze
    result[..., :3] = np.round(rgb).clip(0, 255).astype(np.uint8)
    return result

def tint_colour(value):
    if not re.fullmatch(r"#[0-9A-Fa-f]{6}", value):
        raise ValueError("--tint must be #RRGGBB")
    return np.array([int(value[i:i + 2], 16) for i in (1, 3, 5)], dtype=float) / 255

def apply_tint(a, tint, strength):
    if tint is None or strength == 0:
        return a
    result = a.copy()
    visible = result[..., 3] > 0
    rgb = result[..., :3].astype(float)
    multiplied = rgb * tint
    rgb[visible] = rgb[visible] * (1 - strength) + multiplied[visible] * strength
    result[..., :3] = np.round(rgb).clip(0, 255).astype(np.uint8)
    return result

def run(args):
    _,_,fields,w,h=info(args.slot); w=args.width or w; h=args.height or h; raw_image=Image.open(args.raw).convert("RGBA"); a=np.asarray(raw_image).copy(); keyed=0
    colour=None
    background=core=fringe=0
    if args.mode in KEYED:a,colour,(background,core,fringe)=key_foreground(a)
    crop_box,scale,surface=processing_metadata(a,args.mode,w,h)
    if args.haze and not args.haze_from:
        raise ValueError("--haze-from is required when --haze is non-zero")
    if not 0 <= args.haze <= 1:
        raise ValueError("--haze must be in 0..1")
    if not 0 <= args.tint_strength <= 1:
        raise ValueError("--tint-strength must be in 0..1")
    source_tint_colour = tint_colour(args.tint) if args.tint else None
    result=process(a,args.mode,w,h,fields[7],args.grounded)
    source_haze_colour = haze_colour(args.haze_from) if args.haze else None
    result=apply_haze(result,args.haze,source_haze_colour) if source_haze_colour is not None else result
    result=apply_tint(result,source_tint_colour,args.tint_strength)
    path=sheet(raw_image,Image.fromarray(result,"RGBA"),args.slot,args.mode)
    print(f"K={tuple(round(float(x),1) for x in colour) if colour is not None else 'n/a'} background={background:.2f}% core={core:.2f}% fringe={fringe:.2f}% sheet={path}")
    detail=f"raw={args.raw} mode={args.mode} surface_row={surface if surface is not None else 'n/a'} crop_box={crop_box} scale_factor={scale:.6f} seam_x={seam_text(result, 'x')}" + (f" seam_y={seam_text(result, 'y')}" if args.mode=="material" else "")
    try:
      validate(result,args.mode,w,h,colour)
    except Exception as error:
      print(detail+f" FAIL reason={error}")
      raise
    print(detail+" PASS")
    if not args.dry_run:
      if not args.prompt: raise ValueError("--prompt is required unless --dry-run")
      sub="Backgrounds/" if "_BG_" in args.slot or "_MG_" in args.slot else ""; target=ROOT/"Assets/_Game/Art"/f"Reality{fields[1]}"/"Environment"/sub/args.slot; target.parent.mkdir(parents=True,exist_ok=True); Image.fromarray(result,"RGBA").save(target); write_provenance(args.slot,args.raw,args.prompt,args.haze,args.tint,args.tint_strength)

def candidates_main():
    p=argparse.ArgumentParser(); p.add_argument("--slot",required=True);p.add_argument("--raw",action="append",required=True);args=p.parse_args()
    out=Image.new("RGBA",(300*len(args.raw),260),(120,120,120,255)); d=ImageDraw.Draw(out)
    for i,path in enumerate(args.raw):
      image=Image.open(path).convert("RGBA");image.thumbnail((280,210),Image.Resampling.LANCZOS);out.alpha_composite(image,(i*300+10,35));d.text((i*300+10,10),str(i+1),fill="white")
    target=Path.home()/"Desktop/PAX-A02_contact"/(args.slot+"_candidates.png");target.parent.mkdir(parents=True,exist_ok=True);out.save(target);print(target)

def main():
    if len(__import__("sys").argv)>1 and __import__("sys").argv[1]=="candidates":
      __import__("sys").argv.pop(1); candidates_main(); return
    p=argparse.ArgumentParser(); p.add_argument("--raw",required=True);p.add_argument("--slot",required=True);p.add_argument("--mode",required=True,choices=("sky","strip","material","edge","hazard","object"));p.add_argument("--width",type=int);p.add_argument("--height",type=int);p.add_argument("--dry-run",action="store_true");p.add_argument("--prompt");p.add_argument("--haze",type=float,default=0);p.add_argument("--haze-from");p.add_argument("--grounded",action="store_true");p.add_argument("--tint");p.add_argument("--tint-strength",type=float,default=0);run(p.parse_args())
if __name__=="__main__":main()
