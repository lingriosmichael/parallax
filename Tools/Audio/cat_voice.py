"""D-165 (PAX-110): renders the cat's voice (the developer's pick, reel S, 2026-10-22) into Assets/_Game/Audio/CatVoice/.

Every sound is generated from Tools/Audio/cat_voice_presets.json: per preset, an F0 contour, a level and voicing contour, and
harmonic and breath-noise spectral envelopes over time (48 keyframes), plus a trill; and the purr's spectrum. The tables were
fitted to CC0 recordings (BigSoundBank, Joseph Sardin); no recorded audio is used. Synthesis is additive (each harmonic reads
the envelope at its own frequency, so formants stay put while the pitch moves) plus band-shaped breath noise, with jitter,
shimmer and vibrato from a seed, then reel S's phone mastering. Each role gets a few variants (other seeds, small pitch
steps) that the game plays in turn; variant 0 is reel S's sound. Deterministic: --check re-renders and reports changes.

    python3 Tools/Audio/cat_voice.py [--check]      (numpy and scipy only)
"""
import hashlib, io, json, os, sys, wave
import numpy as np
from scipy import interpolate, signal

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
PRESETS = os.path.join(ROOT, "Tools", "Audio", "cat_voice_presets.json")
OUT = os.path.join(ROOT, "Assets", "_Game", "Audio", "CatVoice")
MANIFEST = os.path.join(OUT, "cat_voice.json")
SR = 44100
BANDS = np.geomspace(150, 11000, 36)          # harmonic envelope band centres, Hz
NBANDS = np.geomspace(150, 14000, 24)         # noise band centres
KEYS = 48                                     # time keyframes per preset
MURMUR = dict(rough=0.03, breath_db=4)        # a closed-mouth "mrp"

# ---- Filters and mastering (reel S's chain) ----

def hp(x, hz, order=2): return signal.sosfilt(signal.butter(order, hz, "highpass", fs=SR, output="sos"), x)
def lp(x, hz, order=2): return signal.sosfilt(signal.butter(order, hz, "lowpass", fs=SR, output="sos"), x)


def peq(x, f0, gain_db, q=0.9):
    """RBJ peaking EQ."""
    A = 10 ** (gain_db / 40); w = 2 * np.pi * f0 / SR; al = np.sin(w) / (2 * q)
    b = [1 + al * A, -2 * np.cos(w), 1 - al * A]; a = [1 + al / A, -2 * np.cos(w), 1 - al / A]
    return signal.lfilter(np.array(b) / a[0], np.array(a) / a[0], x)


def phone_eq(x, hpf=120):
    """Phone speakers: nothing useful under ~150 Hz; a little presence for clarity, tame the very top."""
    x = hp(x, hpf, 4); x = peq(x, 2800, 2.0, 0.8); x = peq(x, 300, -1.5, 1.0); return lp(x, 13000, 2)


def trim(x, rel_db=-42, pad=0.02):
    env = np.abs(signal.hilbert(x)); env = signal.sosfiltfilt(signal.butter(2, 30, fs=SR, output="sos"), env)
    idx = np.where(env > env.max() * 10 ** (rel_db / 20))[0]
    return x[max(0, idx[0] - int(pad * SR)):min(len(x), idx[-1] + int(pad * 3 * SR))]


def fade(x, fin=0.004, fout=0.04):
    x = x.copy(); a = int(fin * SR); b = int(fout * SR)
    if a: x[:a] *= np.linspace(0, 1, a) ** 2
    if b: x[-b:] *= np.linspace(1, 0, b) ** 2
    return x


def loud(x, target_db=-18.0, peak=0.89):
    """The active part's RMS to target_db, then a soft limit on the peak."""
    env = np.sqrt(signal.sosfiltfilt(signal.butter(2, 20, fs=SR, output="sos"), x * x).clip(0))
    rms = np.sqrt(np.mean(x[env > env.max() * 0.25] ** 2)) + 1e-12
    y = x * 10 ** (target_db / 20) / rms
    m = np.max(np.abs(y))
    if m > peak: y = np.tanh(y / peak * 1.2) / np.tanh(1.2) * peak if m > peak * 1.6 else y * peak / m
    return y


def master(x, target, hpf=120): return fade(loud(trim(phone_eq(x, hpf)), target))


def thud(length=0.5, seed=4):
    """A soft body impact: a sinking low tone with harmonics phones can play, a short dull noise burst and a tick."""
    n = int(length * SR); t = np.arange(n) / SR; rng = np.random.default_rng(seed)
    f = 150 * (55 / 150) ** np.minimum(t / 0.09, 1)
    body = np.tanh(2.2 * np.sin(2 * np.pi * np.cumsum(f) / SR)) * np.exp(-t / 0.085)
    nz = lp(rng.uniform(-1, 1, n), 700, 4) * np.exp(-t / 0.035) * 1.6
    tick = hp(rng.uniform(-1, 1, n), 2500, 2) * np.exp(-t / 0.004) * 0.25
    return (body * 0.9 + nz + tick) * np.minimum(t / 0.0015, 1)

# ---- The voice ----

def stft(x, n_fft, hop):
    return signal.stft(x, fs=SR, window="hann", nperseg=n_fft, noverlap=n_fft - hop, boundary="zeros", padded=True)[2]


def istft(Z, n_fft, hop, n):
    return signal.istft(Z, fs=SR, window="hann", nperseg=n_fft, noverlap=n_fft - hop, boundary=True)[1][:n]


def smooth_noise(n, hz, rng):
    w = rng.standard_normal(n // 64 + 4)
    x = np.interp(np.arange(n) / 64, np.arange(len(w)), w)
    x = signal.sosfiltfilt(signal.butter(1, hz, fs=SR, output="sos"), x); return x / (np.std(x) + 1e-9)


def synth(p, seed=1, pitch_st=0.0, formant=1.0, stretch=1.0, jitter=0.04, rough=0.004, shimmer_db=0.8, vibrato=(5.5, 0.0),
          breath_db=0.0, end_fall=0.0, open_db=0.0, trill_gain=1.0):
    """pitch_st shifts F0; formant scales the envelope's frequency axis (>1 = a smaller cat); stretch scales duration;
    jitter is a slow random F0 wobble (semitones); end_fall drops the last 30% by that many more semitones."""
    rng = np.random.default_rng(seed)
    dur = p["dur"] * stretch; n = int(dur * SR); u = np.linspace(0, 1, n); ku = np.linspace(0, 1, KEYS)
    f0k = signal.savgol_filter(np.array(p["f0"]), 7, 2)
    lf0 = interpolate.PchipInterpolator(ku, f0k)(u) + np.log(2) * pitch_st / 12
    lf0 += np.log(2) / 12 * (jitter * smooth_noise(n, 7, rng) * 0.5 + vibrato[1] * np.sin(2 * np.pi * vibrato[0] * u * dur + rng.uniform(0, 6.28)))
    lf0 += rough * smooth_noise(n, 250, rng)                       # fast aperiodicity: widens the upper harmonics
    lf0 -= np.log(2) / 12 * end_fall * np.clip((u - 0.7) / 0.3, 0, 1) ** 2
    f0 = np.exp(lf0); phase = 2 * np.pi * np.cumsum(f0) / SR
    env = np.array(p["env"]); lev = np.array(p["level"]); nz = np.array(p["noise"])
    levu = np.interp(u, ku, lev); levu -= levu.max()
    shim = shimmer_db * smooth_noise(n, 18, rng)
    G = 200; ug = np.linspace(0, 1, G)                             # the envelope on a coarse time grid
    env_g = np.array([np.interp(ug, ku, env[:, j]) for j in range(env.shape[1])]).T
    f0_g = np.interp(ug, u, f0)
    y = np.zeros(n); lb = np.log(BANDS * formant)
    for h in range(1, 41):
        fh_g = h * f0_g
        if fh_g.min() > 12000: break
        amp_g = np.array([np.interp(np.log(fh_g[i]), lb, env_g[i], left=env_g[i][0], right=env_g[i][-1] - 20) for i in range(G)])
        amp_g = np.where(fh_g > 12000, -120, amp_g)
        amp = 10 ** ((np.interp(u, ug, amp_g) + levu + shim + open_db * np.sin(np.pi * u)) / 20)
        y += amp * np.sin(h * phase + rng.uniform(0, 6.28) * (h > 1))
    # Breath noise, band-shaped in the STFT domain.
    W = stft(rng.standard_normal(n), 1024, 256); fq = np.arange(W.shape[0]) * SR / 1024
    tg = np.linspace(0, 1, W.shape[1])
    nzt = np.array([np.interp(tg, ku, nz[:, j]) for j in range(nz.shape[1])])
    levt = np.interp(tg, ku, lev); levt -= levt.max()
    gmat = np.array([np.interp(np.log(np.maximum(fq, 1)), np.log(NBANDS * formant), nzt[:, i], left=-120, right=nzt[-1, i] - 10) for i in range(W.shape[1])]).T
    gmat = 10 ** ((gmat + levt[None, :] + breath_db) / 20)
    nn = istft(W * gmat, 1024, 256, n) * np.sqrt(1024 / 2) / np.sqrt(np.mean(np.hanning(1024) ** 2) * 1024) * 2
    out = y + nn
    if "trill" in p and trill_gain > 0:
        d = np.clip(np.interp(u, ku, p["trill"]) * trill_gain, 0, 0.95)
        rate = p["trill_hz"] * (1 + 0.08 * smooth_noise(n, 3, rng))
        out *= (1 - d) + d * ((1 + np.sin(2 * np.pi * np.cumsum(rate) / SR)) / 2) ** 1.5 / 0.42
    a = int(0.004 * SR); out[:a] *= np.linspace(0, 1, a); b = int(0.02 * SR); out[-b:] *= np.linspace(1, 0, b)
    return out


def band_levels(y, B):
    X = np.abs(stft(y.astype(np.float32), 4096, 441)); fq = np.arange(X.shape[0]) * SR / 4096
    L = 20 * np.log10(X.mean(1) + 1e-9)
    v = np.array([np.mean(L[(fq > b / 2 ** 0.2) & (fq < b * 2 ** 0.2)]) for b in B]); return v - v.max()


def synth_purr(length, fit, seed=7, rate=24.3, breath_hz=1.35, match=3):
    """Re-filtered until its long-term spectrum matches the real purr's (the pulse train isn't spectrally flat)."""
    B = np.array(fit["B"]); target = np.array(fit["loud"]) * 0.6 + np.array(fit["quiet"]) * 0.4
    corr = np.zeros(len(B))
    def with_corr(): return dict(fit, loud=list(np.array(fit["loud"]) + corr), quiet=list(np.array(fit["quiet"]) + corr))
    for _ in range(match): corr += np.clip(target - band_levels(_purr(length, with_corr(), seed, rate, breath_hz), B), -24, 24)
    return _purr(length, with_corr(), seed, rate, breath_hz)


def _purr(length, fit, seed, rate, breath_hz):
    """About 24 laryngeal pulses a second (each a short noise burst), shaped by the real purr's spectrum, swelling on the
    exhale and thinning on the inhale."""
    rng = np.random.default_rng(seed); n = int(length * SR); t = np.arange(n) / SR
    r = rate * (1 + 0.03 * smooth_noise(n, 2, rng))
    idx = np.where(np.diff(np.floor(np.cumsum(r) / SR)) > 0)[0]
    exc = np.zeros(n); plen = int(0.03 * SR); pt = np.arange(plen) / SR
    for i in idx:
        a = 1 + 0.25 * rng.standard_normal()
        burst = rng.standard_normal(plen) * np.exp(-pt / 0.007)
        e = min(n, i + plen); exc[i:e] += a * burst[:e - i]
    pulse_env = signal.sosfiltfilt(signal.butter(1, 40, fs=SR, output="sos"), np.abs(exc)); pulse_env /= pulse_env.max() + 1e-9
    air = signal.sosfilt(signal.butter(2, 900, "highpass", fs=SR, output="sos"), rng.standard_normal(n))
    exc += 0.35 * air * (0.7 + 0.3 * pulse_env)
    B = np.array(fit["B"]); fq = np.linspace(0, SR / 2, 1025)
    def fir(g): return signal.firwin2(1023, fq / (SR / 2), 10 ** (np.interp(np.log(np.maximum(fq, 20)), np.log(B), g) / 20))
    yl = signal.fftconvolve(exc, fir(np.array(fit["loud"])), "same"); yq = signal.fftconvolve(exc, fir(np.array(fit["quiet"])), "same")
    cyc = 0.5 + 0.5 * np.sin(2 * np.pi * breath_hz * t * (1 + 0.05 * smooth_noise(n, 0.5, rng)) - 1.2)
    w = np.clip(cyc, 0, 1) ** 1.3
    y = w * yl + (1 - w) * yq * 0.9
    return y * np.minimum(t / 0.25, 1) * np.minimum((length - t) / 0.45, 1)

# ---- The roles (Parallax.Core.CatVoiceRole) ----

def voice(P, name, target, **kw): return master(synth(P[name], **kw), target)


def purr(fit, seed):
    p = synth_purr(2.6, fit, seed=seed)
    p = hp(p, 90, 4); p = peq(p, 450, 3.0, 0.7); p = peq(p, 900, 2.0, 0.8); p = lp(p, 6000)
    return fade(loud(p, -22), 0.25, 0.5)


def death(P, seed, pitch_st):
    """The body's thud with a short, muffled yelp under it (the developer: "sound like a cat, but not so obvious")."""
    yelp = synth(P["chirp"], seed=seed, pitch_st=pitch_st)[: int(0.19 * SR)]
    yelp = fade(loud(lp(phone_eq(yelp), 3200, 2), -24), 0.003, 0.08)
    d = loud(thud(0.55), -15); at = int(0.012 * SR); d[at:at + len(yelp)] += yelp
    return fade(d * 0.95, 0.001, 0.08)


def roles(data):
    P, fit = data["presets"], data["purr"]
    return {
        # Back at the checkpoint: a small "mew".
        "Mew": [lambda: voice(P, "mew", -17, seed=11, formant=0.94, pitch_st=0.3), lambda: voice(P, "mew", -17, seed=21, formant=0.94, pitch_st=0.0),
                lambda: voice(P, "mew", -17, seed=31, formant=0.94, pitch_st=0.6)],
        # Standing still a while: a soft, low meow.
        "MewLow": [lambda: voice(P, "mew_low", -21, seed=15, formant=0.94, pitch_st=-0.5, stretch=1.08, breath_db=2),
                   lambda: voice(P, "mew_low", -21, seed=25, formant=0.94, pitch_st=-0.8, stretch=1.08, breath_db=2)],
        # A level done: a happy "mrrow".
        "Mrrow": [lambda: voice(P, "mrrow", -16, seed=12, rough=0.006, pitch_st=0.5, end_fall=0.6),
                  lambda: voice(P, "mrrow", -16, seed=22, rough=0.006, pitch_st=0.2, end_fall=0.6)],
        # Jumps and a gravity flip: a bright chirp.
        "Chirp": [lambda: voice(P, "chirp", -19, seed=13, pitch_st=0.4), lambda: voice(P, "chirp", -19, seed=23, pitch_st=0.1),
                  lambda: voice(P, "chirp", -19, seed=33, pitch_st=0.7)],
        # Jumps and wall jumps: a closed-mouth "mrp".
        "Mrp": [lambda: voice(P, "mrp", -19, seed=14, **MURMUR), lambda: voice(P, "mrp", -19, seed=24, pitch_st=0.3, **MURMUR),
                lambda: voice(P, "mrp", -19, seed=34, pitch_st=-0.3, **MURMUR)],
        # A hard landing: the same, lower and quieter ("mrf").
        "Mrf": [lambda: voice(P, "mrp", -21, seed=41, pitch_st=-3.0, formant=0.92, **MURMUR),
                lambda: voice(P, "mrp", -21, seed=42, pitch_st=-3.4, formant=0.92, **MURMUR)],
        # Inverted controls: a questioning trill.
        "Trill": [lambda: voice(P, "trill2", -19, seed=17, **MURMUR), lambda: voice(P, "trill2", -19, seed=27, pitch_st=0.4, **MURMUR)],
        # Under the level-complete chime.
        "Purr": [lambda: purr(fit, 7)],
        # Every death, under its cause's own accent.
        "Death": [lambda: death(P, 16, -1.5), lambda: death(P, 26, -1.8), lambda: death(P, 36, -1.2)],
    }


def wav_bytes(x):
    pcm = (np.clip(x, -1, 1) * 32767).round().astype("<i2").tobytes()
    buf = io.BytesIO()
    with wave.open(buf, "wb") as w: w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(pcm)
    return buf.getvalue()


def main():
    check = "--check" in sys.argv
    data = json.load(open(PRESETS))
    os.makedirs(OUT, exist_ok=True)
    manifest, changes = {"rate": SR, "roles": {}}, []
    for role, makers in roles(data).items():
        entries = []
        for i, make in enumerate(makers):
            name = f"{role}_{i:02}.wav"; path = os.path.join(OUT, name); b = wav_bytes(make())
            old = open(path, "rb").read() if os.path.exists(path) else None
            if old != b:
                changes.append(name)
                if not check: open(path, "wb").write(b)
            entries.append(dict(path=name, seconds=round((len(b) - 44) / 2 / SR, 3), sha1=hashlib.sha1(b).hexdigest()))
        manifest["roles"][role] = entries
    text = json.dumps(manifest, indent=1) + "\n"
    if not os.path.exists(MANIFEST) or open(MANIFEST).read() != text:
        changes.append("cat_voice.json")
        if not check: open(MANIFEST, "w").write(text)
    n = sum(len(v) for v in manifest["roles"].values())
    print(f"{n} sounds, {len(changes)} {'would change' if check else 'changed'}" + (": " + ", ".join(changes) if changes else ""))
    return 1 if check and changes else 0


if __name__ == "__main__":
    sys.exit(main())
