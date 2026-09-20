import sys
from pathlib import Path
import numpy as np
sys.path.insert(0,str(Path(__file__).parents[1]))
import prepare_slot as tool

def gradient(w=134,h=50):
    a=np.zeros((h,w,4),np.uint8); a[...,0]=np.linspace(0,255,w,dtype=np.uint8);a[...,3]=255;return a

def test_seam_has_no_duplicated_block_and_wrap_is_local():
    out=tool.seam(gradient(),120)
    ratio,wrap,inside=tool.seam_ratio(out)
    assert ratio<=3 and wrap<=inside*3 and not np.array_equal(out[:,0],out[:,12])

def test_edge_band_height_and_no_stretch():
    raw=np.zeros((1024,1600,4),np.uint8);raw[400:450,100:1500,3]=255;raw[400:450,100:1500,:3]=100
    out=tool.process(raw,"edge",787,49,"top centre")
    assert out.shape==(49,787,4) and (out[0,:,3]>127).mean()>=.60

def test_object_upscale_preserves_circle_aspect():
    raw=np.zeros((40,40,4),np.uint8); yy,xx=np.ogrid[:40,:40];mask=(xx-20)**2+(yy-20)**2<100;raw[mask]=[20,120,20,255]
    out=tool.process(raw,"object",236,433,"bottom centre");ys,xs=np.where(out[...,3]>0)
    assert max((xs.max()-xs.min()+1)/(ys.max()-ys.min()+1),(ys.max()-ys.min()+1)/(xs.max()-xs.min()+1))<1.02

def test_projection_keyer_unmixes_gold_rim():
    raw=np.full((30,30,4),[255,0,255,255],np.uint8);raw[10:20,10:20]=[220,180,100,255];raw[9,10:20]=[237,90,177,255]
    out,_,_=tool.key_foreground(raw)
    assert abs(int(out[9,15,3])-128)<13
    assert np.allclose(out[9,15,:3],[220,180,100],atol=10) and out[0,0,3]==0 and out[15,15,3]==255

def test_hazard_fade_is_monotonic_and_reaches_zero():
    raw=np.zeros((100,400,4),np.uint8);raw[20:70,:,3]=255;raw[20:70,:,:3]=80
    out=tool.process(raw,"hazard",120,49,"top centre")
    alpha=out[:,60,3]
    assert alpha[0]==255 and alpha[-1]==0 and np.all(np.diff(alpha[round(49*.55):].astype(int))<=0)

def test_provenance_writes_one_row(tmp_path,monkeypatch):
    text="| Slot file name | Reality | What | Source | Ref | Mode | Target size (px) | Pivot | Motif | Raw source | Notes |\n| A_X.png | A | x | Manual | x | Simple | 1 × 1 | centre | | | note |\n| B_X.png | B | x | Manual | x | Simple | 1 × 1 | centre | | | note |\n"
    manifest=tmp_path/"m.md";manifest.write_text(text);monkeypatch.setattr(tool,"MANIFEST",manifest)
    tool.write_provenance("A_X.png","raw.png","P-1")
    lines=manifest.read_text().splitlines();assert "raw.png · P-1" in lines[1] and "raw.png" not in lines[2]

def test_transparent_quiet_edges_skip_seam():
    keyed=np.zeros((30,60,4),np.uint8);assert tool.seam_ratio(keyed)[0] is None

def test_violet_sky_passes_validation():
    sky=np.full((20,20,4),[80,40,140,255],np.uint8)
    tool.validate(sky,"sky",20,20)

def test_violet_block_stays_opaque():
    violet=np.full((20,20,4),[255,0,255,255],np.uint8);violet[8:12,8:12]=[80,40,140,255]
    keyed,_,_=tool.key_foreground(violet);assert keyed[9,9,3]==255 and np.allclose(keyed[9,9,:3],[80,40,140],atol=1)

def test_violet_at_the_core_threshold_stays_opaque():
    violet=np.full((20,20,4),[255,0,255,255],np.uint8);violet[8:12,8:12]=[100,40,160,255]
    keyed,_,_=tool.key_foreground(violet);assert keyed[9,9,3]==255 and np.allclose(keyed[9,9,:3],[100,40,160],atol=1)

def test_thirty_seventy_gold_magenta_mix_is_fringe_not_core():
    raw=np.full((30,30,4),[255,0,255,255],np.uint8);raw[10:20,10:20]=[220,180,100,255]
    raw[9,10:20]=[*np.round(.3*np.array([220,180,100])+ .7*np.array([255,0,255])).astype(np.uint8),255]
    _,_,(_,core,fringe)=tool.key_foreground(raw)
    assert core < 12 and fringe > 1

def test_repeated_sharp_edges_pass_p95_but_artificial_wrap_fails():
    image=np.full((20,80,4),[20,20,20,255],np.uint8)
    image[:,::20]=[220,220,220,255]
    assert tool.seam_ratio(image)[0] <= 1.25
    smooth=np.full((20,100,4),[20,20,20,255]);smooth[:,-1]=[220,220,220,255]
    assert tool.seam_ratio(smooth)[0] > 1.25
