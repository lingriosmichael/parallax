"""D-277 (PAX-125): composes and renders the game's music, one loop per world, into Assets/_Game/Audio/Music/

Nothing here is recorded. The scores are written out below, note by note, and every instrument is additive synthesis:
- GoldenRuins: the sunset, the waterfalls and the ivy. A lilting 6/8 in G major: harp arpeggios, ocarina, a warm bowed line, a
  tiptoeing pizzicato for the game's mischief, running water and a few birds.
- ObsidianVoid: stars, teal nebula, cold glass. A slow 4/4 in D minor: glass bells on long echoes, a quiet glass arpeggio, a
  theremin-like voice, a choir pad, a soft pulse.

Each track is one seamless loop: notes, reverb, echoes and noise all wrap round the end (circular buffers, FFT convolution,
FFT-shaped noise, a zero-phase EQ), so there's no cross-fade and no silence at the seam. Stems are balanced by measured loudness,
not by hand. The tracks carry their own ambience, so they replace the synthesized bed in GameAudio. Deterministic: --check
re-renders and reports changes.

    python3 Tools/Audio/music.py [--check] [--only GoldenRuins]      (numpy and scipy only)
"""
import hashlib, io, json, os, sys, wave
import numpy as np
from scipy import signal

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Assets", "_Game", "Audio", "Music")
MANIFEST = os.path.join(OUT, "music.json")
SR = 44100
TARGET_RMS_DB = -19.0     # the master's loudness; AudioCueBank's music volume scales it under the effects
PEAK = 0.89

# ---- Notes ----

PITCH = dict(C=0, D=2, E=4, F=5, G=7, A=9, B=11)


def midi(name):
    n, i = PITCH[name[0]], 1
    while name[i] in "#b":
        n += 1 if name[i] == "#" else -1
        i += 1
    return n + 12 * (int(name[i:]) + 1)


def hz(note): return 440.0 * 2 ** (((midi(note) if isinstance(note, str) else note) - 69) / 12)


def parse(line):
    """'B4:2 D5:1 | G5:3 R:1': notes with lengths in score units, bars marked for the eye only. Returns (start, length, midi)s."""
    at, notes = 0, []
    for token in line.replace("|", " ").split():
        name, length = token.split(":")
        if name != "R":
            notes.append((at, float(length), midi(name)))
        at += float(length)
    return notes

# ---- Instruments (mono arrays unless noted) ----


def times(seconds): return np.arange(int(seconds * SR)) / SR


def harp(f, vel=1.0, ring=2.6, damp=1.0, rng=None):
    """A plucked string: decaying harmonics, the higher ones sooner, slightly stretched, with a small noise of the finger."""
    t = times(ring)
    y = np.zeros_like(t)
    for k in range(1, 12):
        fk = k * f * np.sqrt(1 + 1.2e-4 * k * k)
        if fk > 9000:
            break
        amp = abs(np.sin(np.pi * k * 0.27)) / k ** 1.25
        y += amp * np.exp(-t * damp * (0.95 + 0.6 * k) * (f / 440) ** 0.3) * np.sin(2 * np.pi * fk * t)
    y *= 1 - np.exp(-t / 0.0025)
    if rng is not None:
        tick = rng.standard_normal(int(0.008 * SR)) * np.linspace(1, 0, int(0.008 * SR))
        y[:len(tick)] += 0.12 * tick
    return y * vel


def bell(f, vel=1.0, ring=4.0, damp=1.0):
    """Glass: inharmonic partials with long, separate decays."""
    t = times(ring)
    y = np.zeros_like(t)
    for ratio, amp, decay in ((1, 1, 1.1), (2.0, 0.34, 1.8), (2.76, 0.26, 2.9), (4.07, 0.13, 4.8), (5.4, 0.07, 7.0)):
        if ratio * f < 10000:
            y += amp * np.exp(-t * decay * damp) * np.sin(2 * np.pi * ratio * f * t)
    return y * (1 - np.exp(-t / 0.002)) * vel


def voiced(f, seconds, vel=1.0, harmonics=(1.0, 0.18, 0.08), vibrato=(5.2, 0.0045, 0.35), attack=0.05, release=0.12, breath=0.0, rng=None):
    """A held voice (ocarina, bowed string, theremin): one phase driving its harmonics, a small scoop into the note, vibrato
    that comes in late, an optional breath."""
    t = times(seconds + release)
    rate, depth, delay = vibrato
    wobble = depth * np.clip((t - delay) / 0.3, 0, 1) * np.sin(2 * np.pi * rate * t)
    phase = np.cumsum(2 * np.pi * f * (1 - 0.012 * np.exp(-t / 0.05)) * (1 + wobble) / SR)
    y = sum(a * np.sin((k + 1) * phase) for k, a in enumerate(harmonics))
    env = np.minimum(1, t / attack) ** 0.7 * np.clip((seconds + release - t) / release, 0, 1) ** 1.5
    y = y * env
    if breath > 0 and rng is not None:
        sos = signal.butter(2, [max(200, f * 1.2), min(9000, f * 5)], "bandpass", fs=SR, output="sos")
        y += breath * signal.sosfilt(sos, rng.standard_normal(len(t))) * env
    return y * vel


def pad(freqs, seconds, vel=1.0, harmonics=(1.0, 0.5, 0.26, 0.14, 0.08, 0.04), attack=1.1, release=1.5, rng=None):
    """Stereo. Slow held tones, each left and right slightly detuned from each other."""
    t = times(seconds + release)
    env = np.minimum(1, t / attack) ** 2 * np.clip((seconds + release - t) / release, 0, 1) ** 2
    out = np.zeros((len(t), 2))
    for f in freqs:
        for side, cents in ((0, -5.0), (1, 5.0)):
            fd = f * 2 ** (cents / 1200)
            for k, a in enumerate(harmonics):
                out[:, side] += a * np.sin(2 * np.pi * (k + 1) * fd * t + rng.uniform(0, 6.28))
    return out * env[:, None] * vel


def bass(f, seconds, vel=1.0):
    """Round, with enough harmonics that a phone speaker, which plays little under 200 Hz, still gives the note."""
    t = times(seconds + 0.2)
    y = sum(a * np.sin(2 * np.pi * (k + 1) * f * t) for k, a in enumerate((1.0, 0.5, 0.3, 0.14)))
    env = (1 - np.exp(-t / 0.02)) * np.exp(-t * 0.9) * np.clip((seconds + 0.2 - t) / 0.2, 0, 1)
    return y * env * vel


def drum(f=105.0, vel=1.0, rng=None):
    t = times(0.5)
    phase = np.cumsum(2 * np.pi * f * (1 + 1.1 * np.exp(-t / 0.018)) / SR)
    y = (np.sin(phase) + 0.35 * np.sin(2 * phase)) * np.exp(-t / 0.14)
    y += 0.2 * rng.standard_normal(len(t)) * np.exp(-t / 0.008)
    return y * vel


def shaker(vel=1.0, rng=None):
    t = times(0.09)
    y = rng.standard_normal(len(t)) * (1 - np.exp(-t / 0.004)) * np.exp(-t / 0.024)
    return signal.sosfilt(signal.butter(2, 5200, "highpass", fs=SR, output="sos"), y) * vel


def loop_noise(n, low, high, seed):
    """Stereo noise in a band that loops exactly: random phases in the spectrum."""
    rng = np.random.default_rng(seed)
    f = np.fft.rfftfreq(n, 1 / SR)
    shape = np.clip((f - low) / (0.3 * low), 0, 1) * np.clip((high - f) / (0.3 * high), 0, 1)
    out = np.stack([np.fft.irfft(shape * np.exp(2j * np.pi * rng.random(len(f))), n) for _ in range(2)], 1)
    return out / np.sqrt(np.mean(out ** 2))

# ---- The mix ----


def active_rms(x):
    """A stem's level where it's actually sounding: the louder half of its frames."""
    size = int(0.2 * SR)
    frames = (x[: len(x) // size * size] ** 2).sum(1).reshape(-1, size).mean(1)
    frames = np.sort(frames)[len(frames) // 2:]
    return float(np.sqrt(frames.mean() + 1e-12))


def circular_convolve(x, ir):
    n = len(x)
    return np.stack([np.fft.irfft(np.fft.rfft(x[:, c]) * np.fft.rfft(ir[:, c], n), n) for c in range(2)], 1)


def impulse(seconds, seed):
    """A hall: decaying stereo noise that darkens as it dies, after a short pre-delay."""
    rng = np.random.default_rng(seed)
    n, pre = int(seconds * SR), int(0.024 * SR)
    t = np.arange(n) / SR
    ir = np.zeros((n + pre, 2))
    for c in range(2):
        y = rng.standard_normal(n) * np.exp(-t * 6.9 / seconds)
        dark = signal.sosfilt(signal.butter(1, 1400, "lowpass", fs=SR, output="sos"), y)
        fade = np.exp(-t * 3 / seconds)
        ir[pre:, c] = y * fade + dark * (1 - fade)
    return ir / np.sqrt((ir ** 2).sum(0).mean())


def zero_phase_eq(x):
    """The master EQ: clear the rumble, a little presence for small speakers, ease the very top. Applied on the loop's whole
    spectrum, so it has no seam."""
    f = np.fft.rfftfreq(len(x), 1 / SR)
    gain = 1 / np.sqrt(1 + (70 / np.maximum(f, 1)) ** 4) / np.sqrt(1 + (f / 13000) ** 4)
    gain *= 1 + 0.26 * np.exp(-0.5 * (np.log2(np.maximum(f, 1) / 2500) / 0.8) ** 2)
    return np.stack([np.fft.irfft(np.fft.rfft(x[:, c]) * gain, len(x)) for c in range(2)], 1)


def soft_limit(x, knee=0.7):
    over = np.abs(x) > knee
    y = x.copy()
    y[over] = np.sign(x[over]) * (knee + (PEAK - knee) * np.tanh((np.abs(x[over]) - knee) / (PEAK - knee)))
    return y


class Mix:
    def __init__(self, seconds, seed):
        self.n = int(round(seconds * SR))
        self.rng = np.random.default_rng(seed)
        self.stems = {}

    def stem(self, name, level_db, reverb=0.2, echo=0.0):
        self.stems[name] = dict(audio=np.zeros((self.n, 2)), level=level_db, reverb=reverb, echo=echo)

    def put(self, name, seconds, sound, pan=0.0):
        """Adds a sound at a time, wrapping round the loop's end."""
        if sound.ndim == 1:
            angle = (pan + 1) * np.pi / 4
            sound = np.stack([sound * np.cos(angle), sound * np.sin(angle)], 1)
        at = np.arange(len(sound)) + int(round(seconds * SR))
        np.add.at(self.stems[name]["audio"], at % self.n, sound)

    def fill(self, name, sound): self.stems[name]["audio"] += sound

    def render(self, ir_seconds, echo_seconds=0.0, seed=7):
        ir = impulse(ir_seconds, seed)
        dry, verb, delay = (np.zeros((self.n, 2)) for _ in range(3))
        for stem in self.stems.values():
            a = stem["audio"] / active_rms(stem["audio"]) * 10 ** (stem["level"] / 20)
            dry += a
            verb += a * stem["reverb"]
            delay += a * stem["echo"]
        if echo_seconds > 0:
            step = int(round(echo_seconds * SR))
            trail = np.zeros((self.n, 2))
            for k in range(1, 8):      # ping-pong repeats
                repeat = np.roll(delay, k * step, axis=0) * 0.62 ** k
                trail += repeat[:, ::-1] if k % 2 else repeat
            dry += trail
            verb += trail * 0.5
        mix = zero_phase_eq(dry + circular_convolve(verb, ir))
        mix *= 10 ** (TARGET_RMS_DB / 20) / np.sqrt(np.mean(mix ** 2))
        return soft_limit(mix)

# ---- Golden Ruins: G major, 6/8, a storybook waltz at sunset ----

# Name: bass, pad tones, six arpeggio tones (root low, up and back).
GOLD_CHORDS = {
    "G": ("G2", "G3 B3 D4", "G3 D4 G4 B4 G4 D4"), "Em": ("E2", "G3 B3 E4", "E3 B3 E4 G4 E4 B3"),
    "C": ("C3", "G3 C4 E4", "C3 G3 C4 E4 C4 G3"), "Cmaj7": ("C3", "G3 B3 E4", "C3 G3 B3 E4 B3 G3"),
    "D": ("D3", "F#3 A3 D4", "D3 A3 D4 F#4 D4 A3"), "Dsus": ("D3", "G3 A3 D4", "D3 A3 D4 G4 D4 A3"),
    "Bm": ("B2", "F#3 B3 D4", "B2 F#3 B3 D4 B3 F#3"), "Am": ("A2", "A3 C4 E4", "A2 E3 A3 C4 A3 E3"),
}
GOLD_A = "B4:2 D5:1 G5:3 | F#5:1 E5:1 D5:1 B4:3 | C5:2 E5:1 G5:2 E5:1 | D5:5 R:1 | B4:2 D5:1 G5:2 A5:1 | B5:1 A5:1 F#5:1 D5:3 | E5:2 D5:1 C5:2 B4:1 | A4:2 B4:1 A4:3"
GOLD_A_END = "B4:2 D5:1 G5:3 | F#5:1 E5:1 D5:1 B4:3 | C5:2 E5:1 G5:2 E5:1 | D5:5 R:1 | B4:2 D5:1 G5:2 A5:1 | B5:1 A5:1 F#5:1 D5:3 | E5:2 C5:1 A4:2 F#5:1 | G5:5 R:1"
GOLD_B = "G5:2 F#5:1 E5:3 | C5:2 D5:1 E5:3 | D5:2 G5:1 B4:3 | A4:2 B4:1 D5:3 | G5:2 F#5:1 E5:2 G5:1 | C6:3 B5:1 A5:1 G5:1 | E5:2 F#5:1 G5:2 A5:1 | F#5:5 R:1"
GOLD_FORM = (
    ("Intro", "G C G Dsus", None, None),
    ("A", "G Em C D G Bm C D", "harp_lead", GOLD_A),
    ("A2", "G Em C D G Bm Am D", "ocarina", GOLD_A_END),
    ("B", "Em C G D Em C Am D", "ocarina", GOLD_B),
    ("A3", "G Em C D G Bm Am D", "bowed", GOLD_A_END),
    ("Tag", "Cmaj7 G C Dsus", None, None),
)
GOLD_TIPTOE = "R:1 E4:1 R:1 B3:1 R:1 G3:1"     # the pizzicato's sneaking step, a bar at a time, off the beat
GOLD_WINK = "D5:1 C#5:1 C5:1 B4:1 R:2"         # the Tag's chromatic slide down: something's up


def diatonic_third_below(m):
    scale = [0, 2, 4, 6, 7, 9, 11]     # G major
    pc = m % 12
    return m - ((pc - scale[scale.index(pc) - 2]) % 12)


def golden():
    bpm_dotted = 58.0
    unit = 60.0 / (bpm_dotted * 3)          # an eighth note
    bar = unit * 6
    bars = sum(len(section[1].split()) for section in GOLD_FORM)
    m = Mix(bars * bar, seed=2041)
    rng = m.rng
    m.stem("harp", -8.5, reverb=0.32)
    m.stem("harp_lead", -4.0, reverb=0.34)
    m.stem("ocarina", -4.0, reverb=0.42)
    m.stem("bowed", -4.5, reverb=0.46)
    m.stem("pad", -14.0, reverb=0.36)
    m.stem("bass", -12.5, reverb=0.1)
    m.stem("pizz", -11.0, reverb=0.24)
    m.stem("drum", -18.0, reverb=0.16)
    m.stem("shaker", -27.0, reverb=0.1)
    m.stem("water", -25.0, reverb=0.0)
    m.stem("birds", -27.0, reverb=0.5)

    at = 0
    for name, chords, lead, score in GOLD_FORM:
        start = at * bar
        for i, chord in enumerate(chords.split()):
            low, tones, arp = GOLD_CHORDS[chord]
            t = (at + i) * bar
            fuller = name in ("A2", "A3")
            m.put("bass", t, bass(hz(low), bar * 0.95, 0.8))
            if name != "Intro":
                m.put("bass", t + 3 * unit, bass(hz(low) * 1.5, bar * 0.45, 0.45))
            m.put("pad", t, pad([hz(n) for n in tones.split()], bar * 1.1, 0.7 if not fuller else 1.0, rng=rng))
            for k, note in enumerate(arp.split()):
                nudge = float(rng.uniform(-0.006, 0.006))
                strength = (1.0 if k in (0, 3) else 0.72) * (1.0 if name != "B" else 0.8)
                m.put("harp", t + k * unit + nudge, harp(hz(note), strength, rng=rng), pan=-0.3 + 0.12 * k)
            if name == "B":
                for s, length, note in parse(GOLD_TIPTOE):
                    m.put("pizz", t + s * unit, harp(hz(int(note) + 12), 0.9, ring=0.5, damp=6.0, rng=rng), pan=0.35)
            if fuller or name == "B":
                m.put("drum", t, drum(rng=rng), pan=-0.05)
                m.put("drum", t + 3 * unit, drum(vel=0.6, rng=rng), pan=0.05)
            if fuller:
                for k in range(6):
                    m.put("shaker", t + k * unit, shaker(1.0 if k % 3 == 0 else 0.55, rng=rng), pan=0.3)
        if score:
            for s, length, note in parse(score):
                t = start + s * unit + float(rng.uniform(-0.004, 0.004))
                seconds = length * unit * 0.97
                if lead == "harp_lead":
                    m.put(lead, t, harp(hz(note), 1.0, ring=3.2, rng=rng), pan=0.1)
                elif lead == "ocarina":
                    m.put(lead, t, voiced(hz(note), seconds, 1.0, breath=0.05, rng=rng), pan=0.08)
                else:
                    m.put(lead, t, voiced(hz(note), seconds, 1.0, harmonics=(1, 0.62, 0.42, 0.25, 0.16, 0.09, 0.05), vibrato=(5.6, 0.0055, 0.18),
                                          attack=0.09, release=0.2), pan=-0.05)
                    m.put("harp_lead", t, harp(hz(diatonic_third_below(note)), 0.55, ring=2.6, rng=rng), pan=0.25)
        if name == "Tag":
            for s, length, note in parse(GOLD_WINK):
                m.put("pizz", start + 2 * bar + s * unit, harp(hz(note), 1.0, ring=0.6, damp=5.0, rng=rng), pan=0.2)
        at += len(chords.split())

    seconds = m.n / SR
    t = np.arange(m.n) / SR
    swell = 0.75 + 0.25 * np.sin(2 * np.pi * 5 * t / seconds)      # the falls, breathing, a whole number of times a loop
    m.fill("water", loop_noise(m.n, 900, 5200, 31) * swell[:, None] + 0.7 * loop_noise(m.n, 180, 700, 32))
    for at_bar, pitch in ((2.5, 2900), (14.25, 3300), (22.5, 2650), (31.75, 3100), (37.5, 2800)):
        for k in range(3):
            chirp = voiced(pitch * (1 + 0.05 * k), 0.06, 1.0 - 0.2 * k, harmonics=(1,), vibrato=(0, 0, 1), attack=0.01, release=0.03)
            m.put("birds", at_bar * bar + k * 0.12, chirp, pan=0.5 if int(at_bar) % 2 else -0.5)
    return m.render(2.6), dict(meter="6/8", dotted_quarter_bpm=bpm_dotted, bars=bars, key="G major")

# ---- Obsidian Void: D minor, 4/4, glass under the stars ----

COLD_CHORDS = {
    "Dm": ("D3", "D4 F4 A4", "D4 A4 D5 F5"), "Bb": ("Bb2", "D4 F4 Bb4", "Bb3 F4 Bb4 D5"), "Gm": ("G2", "D4 G4 Bb4", "G3 D4 G4 Bb4"),
    "A": ("A2", "C#4 E4 A4", "A3 E4 A4 C#5"), "F": ("F2", "C4 F4 A4", "F3 C4 F4 A4"), "Eb": ("Eb3", "Eb4 G4 Bb4", "Eb4 Bb4 Eb5 G5"),
}
COLD_A = "A4:3 D5:1 F5:4 | E5:2 D5:2 F5:4 | G5:3 F5:1 D5:4 | E5:6 C#5:2 | A4:3 D5:1 F5:2 A5:2 | C6:4 A5:2 G5:2 | Bb5:3 A5:1 G5:2 F5:2 | E5:8"
COLD_B = "Bb5:6 A5:2 | G5:6 Eb5:2 | F5:4 D5:4 | E5:6 C#5:2 | Bb5:4 C6:2 Bb5:2 | G5:6 Bb5:2 | A5:4 F5:2 D5:2 | E5:8"
COLD_FORM = (
    ("Intro", "Dm Dm", None, None),
    ("A", "Dm Bb Gm A Dm F Gm A", "bell_lead", COLD_A),
    ("A2", "Dm Bb Gm A Dm F Gm A", "theremin", COLD_A),
    ("B", "Gm Eb Dm A Gm Eb Dm A", "theremin", COLD_B),
)
COLD_ARP_ORDER = (0, 1, 2, 3, 2, 1, 2, 3, 0, 1, 2, 3, 2, 3, 2, 1)     # sixteen sixteenths a bar


def obsidian():
    bpm = 72.0
    beat = 60.0 / bpm
    unit = beat / 2                         # an eighth note
    bar = beat * 4
    bars = sum(len(section[1].split()) for section in COLD_FORM)
    m = Mix(bars * bar, seed=3107)
    rng = m.rng
    m.stem("arp", -14.0, reverb=0.3, echo=0.1)
    m.stem("bell_lead", -4.5, reverb=0.46, echo=0.36)
    m.stem("theremin", -5.0, reverb=0.5, echo=0.2)
    m.stem("bell_double", -11.0, reverb=0.5, echo=0.4)
    m.stem("pad", -11.0, reverb=0.5)
    m.stem("bass", -12.0, reverb=0.12)
    m.stem("pulse", -19.0, reverb=0.2)
    m.stem("stars", -24.0, reverb=0.6, echo=0.5)
    m.stem("air", -29.0, reverb=0.0)

    at = 0
    for name, chords, lead, score in COLD_FORM:
        start = at * bar
        for i, chord in enumerate(chords.split()):
            low, tones, arp = COLD_CHORDS[chord]
            t = (at + i) * bar
            m.put("bass", t, bass(hz(low), bar * 1.1, 0.9))
            m.put("pad", t, pad([hz(n) for n in tones.split()], bar * 1.05, 1.0, harmonics=(1.0, 0.1, 0.28, 0.05, 0.1), attack=1.4, release=2.0, rng=rng))
            cells = [hz(n) for n in arp.split()]
            strength = 0.75 if name == "Intro" else 1.0
            for k, cell in enumerate(COLD_ARP_ORDER):
                accent = 1.0 if k % 4 == 0 else 0.6
                m.put("arp", t + k * unit / 2, bell(cells[cell], accent * strength, ring=1.4, damp=4.5), pan=-0.45 if k % 2 else 0.45)
            if name != "Intro":
                for hit, vel in ((0, 1.0), (1.5, 0.5), (2, 0.8), (3.5, 0.5)):
                    m.put("pulse", t + hit * beat, drum(f=78.0, vel=vel, rng=rng))
        if score:
            for s, length, note in parse(score):
                t = start + s * unit
                seconds = length * unit * 0.96
                if lead == "bell_lead":
                    m.put(lead, t, bell(hz(note), 1.0, ring=4.5), pan=0.12)
                else:
                    m.put(lead, t, voiced(hz(note), seconds, 1.0, harmonics=(1, 0.1, 0.05), vibrato=(4.6, 0.007, 0.3), attack=0.14, release=0.35), pan=-0.1)
                    m.put("bell_double", t, bell(hz(note + 12), 0.7, ring=3.0), pan=0.3)
        at += len(chords.split())

    for at_bar, note, pan in ((0.5, "A6", 0.6), (3.75, "D7", -0.5), (7.25, "F6", 0.4), (11.5, "E7", -0.6), (14.25, "A6", 0.5),
                              (17.75, "C7", -0.4), (20.25, "D7", 0.6), (23.5, "A6", -0.5)):
        m.put("stars", at_bar * bar, bell(hz(note), 1.0, ring=2.5, damp=1.4), pan=pan)
    t = np.arange(m.n) / SR
    drift = 0.7 + 0.3 * np.sin(2 * np.pi * 3 * t / (m.n / SR))
    m.fill("air", loop_noise(m.n, 220, 1800, 41) * drift[:, None])
    return m.render(3.4, echo_seconds=unit * 1.5), dict(meter="4/4", bpm=bpm, bars=bars, key="D minor")

# ---- Output ----


def wav_bytes(x):
    pcm = np.clip(np.round(x * 32767), -32768, 32767).astype("<i2")
    out = io.BytesIO()
    with wave.open(out, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())
    return out.getvalue()


TRACKS = dict(GoldenRuins=golden, ObsidianVoid=obsidian)


def main():
    check = "--check" in sys.argv
    only = sys.argv[sys.argv.index("--only") + 1] if "--only" in sys.argv else None
    manifest = json.load(open(MANIFEST)) if os.path.exists(MANIFEST) else {}
    changed = []
    for name, compose in TRACKS.items():
        if only and name != only:
            continue
        audio, info = compose()
        data = wav_bytes(audio)
        digest = hashlib.sha256(data).hexdigest()
        if manifest.get(name, {}).get("sha256") != digest:
            changed.append(name)
        print(f"{name}: {len(audio) / SR:.1f} s, {len(data) / 1e6:.1f} MB, peak {np.abs(audio).max():.3f}, {digest[:12]}")
        if not check:
            os.makedirs(OUT, exist_ok=True)
            with open(os.path.join(OUT, name + ".wav"), "wb") as f:
                f.write(data)
            manifest[name] = dict(info, seconds=round(len(audio) / SR, 3), rate=SR, sha256=digest)
    if check:
        print("changed: " + ", ".join(changed) if changed else "no changes")
        return 1 if changed else 0
    with open(MANIFEST, "w") as f:
        json.dump(manifest, f, indent=1, sort_keys=True)
        f.write("\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
