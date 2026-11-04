"""D-287 (replaces D-277's scores): composes and renders the game's music, one loop per world, into Assets/_Game/Audio/Music/

A cat dodges traps and dies a million times, so the music is fast and cool, built on rhythm, not on a tune. Nothing here is recorded:
the patterns are written out below, step by step, and every instrument is synthesized.
- GoldenRuins: a cat burglar's funk breakbeat. 138 bpm, E Dorian on a two-chord vamp: a swung break, slap bass, electric piano
  stabs, muted guitar, record scratches, a little vinyl crackle.
- ObsidianVoid: dark phonk. 148 bpm, D Phrygian: a cowbell riff, a distorted 808, hard claps, trap hat rolls, a reese bass.

Each track is one seamless loop: hits, reverb, echoes and noise all wrap round the end (circular buffers, FFT convolution, FFT-shaped
noise, a zero-phase EQ), so there's no cross-fade and no silence at the seam. Bass and pads duck under the kick, and everything low is
driven until it has harmonics, because a phone speaker plays little under 200 Hz. Stems are balanced by measured loudness, not by hand.
D-289: each world has several tracks, each its own tune, a new one every 5 levels. The tracks replace the synthesized bed in GameAudio. Deterministic: --check re-renders and reports changes.

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
GLUE_DB = -9.0            # the mix's level going into the saturator: higher is denser

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


def band(y, low, high): return signal.sosfilt(signal.butter(2, [low, high], "bandpass", fs=SR, output="sos"), y)


def high(y, cut): return signal.sosfilt(signal.butter(2, cut, "highpass", fs=SR, output="sos"), y)


def kick(rng, f=54.0, drive=2.4, tail=0.075):
    """A short boom with a click on it; driven, so its body still reaches a phone."""
    t = times(0.34)
    phase = np.cumsum(2 * np.pi * (f + 190 * np.exp(-t / 0.024)) / SR)
    y = np.tanh(drive * np.sin(phase) * np.exp(-t / tail)) / np.tanh(drive)
    y[:int(0.003 * SR)] += 0.5 * rng.standard_normal(int(0.003 * SR))
    return y


def snare(rng, tone=196.0, vel=1.0):
    t = times(0.26)
    body = (np.sin(2 * np.pi * tone * t) + 0.6 * np.sin(2 * np.pi * tone * 1.72 * t)) * np.exp(-t / 0.05)
    wires = band(rng.standard_normal(len(t)), 1300, 8000) * np.exp(-t / 0.08)
    return np.tanh(1.4 * (0.75 * body + 1.5 * wires)) * vel


def clap(rng, vel=1.0):
    t = times(0.3)
    y = np.zeros_like(t)
    for ms in (0, 11, 23):
        k = int(ms * 0.001 * SR)
        y[k:] += rng.standard_normal(len(t) - k) * np.exp(-t[:len(t) - k] / 0.006)
    y += 0.9 * rng.standard_normal(len(t)) * np.exp(-np.maximum(t - 0.03, 0) / 0.075) * (t > 0.03)
    return band(y, 900, 6500) * vel


def hat(rng, vel=1.0, tail=0.026):
    """Metal: six clashing squares and noise, only the top of it."""
    t = times(max(0.08, tail * 5))
    y = sum(np.sign(np.sin(2 * np.pi * f * t)) for f in (317, 449, 557, 761, 913, 1127)) / 6 + 0.7 * rng.standard_normal(len(t))
    return high(y, 7000) * np.exp(-t / tail) * vel


def rim(f=1750.0, vel=1.0):
    t = times(0.05)
    return np.tanh(3 * np.sin(2 * np.pi * f * t)) * np.exp(-t / 0.008) * vel


def cowbell(f, seconds, vel=1.0):
    """The 808's two squares a sour fifth apart, played as a riff."""
    t = times(max(seconds, 0.12) + 0.1)
    y = np.sign(np.sin(2 * np.pi * f * t)) + 0.8 * np.sign(np.sin(2 * np.pi * f * 1.482 * t))
    env = np.exp(-t / 0.05) * 0.7 + 0.3 * np.exp(-t / 0.2)
    return np.tanh(1.6 * band(y, f * 0.8, f * 4.2)) * env * vel


def slap_bass(f, seconds, vel=1.0):
    """A plucked string with a bright snap that dies fast: the higher the harmonic, the sooner it goes."""
    t = times(seconds + 0.04)
    y = sum(np.exp(-t * (2.2 + 1.5 * k)) * np.sin(2 * np.pi * k * f * t) / k ** 1.05 for k in range(1, 16) if k * f < 7000)
    env = (1 - np.exp(-t / 0.0015)) * np.clip((seconds + 0.04 - t) / 0.04, 0, 1)
    return np.tanh(1.9 * y) * env * vel


def sub808(f, seconds, vel=1.0, slide_from=None):
    """Sine driven hard, with a glide into the note when asked."""
    t = times(seconds + 0.05)
    start = slide_from if slide_from else f
    phase = np.cumsum(2 * np.pi * (f + (start - f) * np.exp(-t / 0.05)) / SR)
    y = np.tanh(3.2 * (np.sin(phase) + 0.3 * np.sin(2 * phase)))
    env = (1 - np.exp(-t / 0.002)) * np.exp(-t / max(seconds, 0.2)) * np.clip((seconds + 0.05 - t) / 0.05, 0, 1)
    return y * env * vel


def reese(f, seconds, vel=1.0):
    """Stereo. Two saws beating against each other, the mean bass of drum and bass."""
    t = times(seconds + 0.06)
    env = np.minimum(1, t / 0.008) * np.clip((seconds + 0.06 - t) / 0.06, 0, 1)
    out = np.zeros((len(t), 2))
    for side, cents in ((0, -11.0), (1, 11.0)):
        fd = f * 2 ** (cents / 1200)
        y = sum(np.sin(2 * np.pi * k * fd * t) / k / (1 + (k * fd / 1100) ** 2) for k in range(1, 26) if k * fd < 6000)
        y2 = sum(np.sin(2 * np.pi * k * fd * 1.0037 * t + k) / k / (1 + (k * fd / 1100) ** 2) for k in range(1, 26) if k * fd < 6000)
        out[:, side] = np.tanh(1.5 * (y + y2)) * env
    return out * vel


def epiano(f, seconds, vel=1.0):
    """Two-operator FM: a soft bark on the attack and a little bell, then a round tone."""
    t = times(seconds + 0.12)
    bark = 1.7 * np.exp(-t * 7) * np.sin(2 * np.pi * f * t)
    tine = 0.5 * np.exp(-t * 24) * np.sin(2 * np.pi * f * 14 * t)
    y = np.sin(2 * np.pi * f * t + bark + tine) * np.exp(-t * 2.4)
    return np.tanh(1.3 * y) * (1 - np.exp(-t / 0.002)) * np.clip((seconds + 0.12 - t) / 0.12, 0, 1) * vel


def organ(f, seconds, vel=1.0):
    """A drawbar organ: a stack of harmonics, a key click, a fast wobble."""
    t = times(seconds + 0.04)
    wobble = 1 + 0.0025 * np.sin(2 * np.pi * 6.4 * t)
    y = sum(a * np.sin(2 * np.pi * k * f * wobble * t) for k, a in ((1, 1.0), (2, 0.8), (3, 0.6), (4, 0.3), (6, 0.2)))
    y[:int(0.002 * SR)] += 0.6
    return np.tanh(0.9 * y) * np.minimum(1, t / 0.003) * np.clip((seconds + 0.04 - t) / 0.04, 0, 1) * vel


def clav(f, seconds, vel=1.0):
    """A clavinet: a thin bright pluck that bites."""
    t = times(seconds + 0.03)
    y = sum(np.exp(-t * (7 + 1.1 * k)) * np.sin(2 * np.pi * k * f * t) / k ** 0.8 for k in range(2, 14) if k * f < 9000)
    return np.tanh(1.8 * y) * np.clip((seconds + 0.03 - t) / 0.03, 0, 1) * vel


def brass(f, seconds, vel=1.0):
    """A short horn stab: the top opens as the note speaks, then falls."""
    t = times(seconds + 0.06)
    y = sum((1 - np.exp(-t * (40 / k))) * np.exp(-t * 0.6 * k) * np.sin(2 * np.pi * k * f * t) / k for k in range(1, 12) if k * f < 7000)
    return np.tanh(1.5 * y) * np.clip((seconds + 0.06 - t) / 0.06, 0, 1) * vel


def lead(f, seconds, vel=1.0):
    """A hollow square with a little glide into the note: the hook."""
    t = times(seconds + 0.05)
    phase = np.cumsum(2 * np.pi * f * (1 - 0.03 * np.exp(-t / 0.02)) * (1 + 0.004 * np.sin(2 * np.pi * 5.5 * t)) / SR)
    y = sum(np.sin(k * phase) / k for k in (1, 3, 5, 7, 9))
    return np.tanh(1.4 * y) * np.minimum(1, t / 0.006) * np.clip((seconds + 0.05 - t) / 0.05, 0, 1) * vel


def muted_guitar(f, vel=1.0):
    """A dead string: a click of pitch and no ring. Rhythm, not notes."""
    t = times(0.07)
    y = sum(np.exp(-t * (30 + 6 * k)) * np.sin(2 * np.pi * k * f * t) / k for k in range(1, 12))
    return band(y, 500, 3800) * vel


def glass(f, vel=1.0):
    t = times(0.6)
    return sum(a * np.exp(-t * d) * np.sin(2 * np.pi * r * f * t) for r, a, d in ((1, 1, 9), (2.76, 0.4, 16), (5.4, 0.15, 30))) * vel


def scratch(rng, seconds, strokes=4, vel=1.0):
    """A record pushed back and forth under the needle: a rough loop of tone read at a speed that swings through zero."""
    t = times(seconds)
    source = band(rng.standard_normal(SR) + 0.6 * np.sign(np.sin(2 * np.pi * 233 * times(1.0))), 420, 3400)
    place = 2600 + 2400 * np.sin(2 * np.pi * strokes / seconds * t)
    y = np.interp(place, np.arange(len(source)), source)
    speed = np.abs(np.gradient(place))
    return y * np.clip(speed / speed.max() * 1.6, 0, 1) * np.sin(np.pi * t / seconds) ** 0.3 * vel


def riser(rng, seconds, vel=1.0):
    t = times(seconds)
    return high(rng.standard_normal(len(t)), 1800) * (t / seconds) ** 2.4 * vel


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
    """The master EQ: clear the rumble, thin the sub a phone can't play, lift the bass's harmonics and the presence for small speakers,
    ease the very top. Applied on the loop's whole spectrum, so it has no seam."""
    f = np.fft.rfftfreq(len(x), 1 / SR)
    gain = 1 / np.sqrt(1 + (70 / np.maximum(f, 1)) ** 4) / np.sqrt(1 + (f / 13000) ** 4)
    gain *= 0.4 + 0.6 * (1 - np.exp(-(f / 170) ** 2))
    gain *= 1 + 0.9 * np.exp(-0.5 * (np.log2(np.maximum(f, 1) / 420) / 0.7) ** 2)
    gain *= 1 + 0.26 * np.exp(-0.5 * (np.log2(np.maximum(f, 1) / 2500) / 0.8) ** 2)
    return np.stack([np.fft.irfft(np.fft.rfft(x[:, c]) * gain, len(x)) for c in range(2)], 1)


def glue(x, drive_db=GLUE_DB):
    """The whole mix pressed into a tanh, so the drums stop towering over everything and the loop sounds dense. No memory, so no seam."""
    return np.tanh(x * 10 ** (drive_db / 20) / np.sqrt(np.mean(x ** 2)))


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

    def stem(self, name, level_db, reverb=0.2, echo=0.0, duck=0.0):
        self.stems[name] = dict(audio=np.zeros((self.n, 2)), level=level_db, reverb=reverb, echo=echo, duck=duck)

    def put(self, name, seconds, sound, pan=0.0):
        """Adds a sound at a time, wrapping round the loop's end."""
        if sound.ndim == 1:
            angle = (pan + 1) * np.pi / 4
            sound = np.stack([sound * np.cos(angle), sound * np.sin(angle)], 1)
        at = np.arange(len(sound)) + int(round(seconds * SR))
        np.add.at(self.stems[name]["audio"], at % self.n, sound)

    def fill(self, name, sound): self.stems[name]["audio"] += sound

    def pump(self, kicks, release=0.11):
        """Stems with `duck` dip under each kick and swell back; wraps like everything else."""
        env = np.zeros(self.n)
        t = np.arange(int(release * 6 * SR)) / SR
        for k in kicks:
            at = (np.arange(len(t)) + int(round(k * SR))) % self.n
            env[at] = np.maximum(env[at], np.exp(-t / release))
        for stem in self.stems.values():
            if stem["duck"] > 0:
                stem["audio"] *= (1 - stem["duck"] * env)[:, None]

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
        mix = glue(zero_phase_eq(dry + circular_convolve(verb, ir)))
        mix *= 10 ** (TARGET_RMS_DB / 20) / np.sqrt(np.mean(mix ** 2))
        return soft_limit(mix)

# ---- Patterns ----


def hits(pattern):
    """'x..g..X.': a step per character. X an accent, x a hit, g a ghost, o an open hat, . a rest. Spaces are for the eye."""
    strength = dict(X=1.0, x=0.8, g=0.3, o=0.8)
    return [(k, strength[c], c) for k, c in enumerate(pattern.replace(" ", "")) if c != "."]


class Grid:
    """A bar of sixteen steps, the off-steps late by `swing` of a step."""

    def __init__(self, bpm, swing=0.0):
        self.step, self.swing = 60.0 / bpm / 4, swing
        self.bar = self.step * 16

    def at(self, bar, step): return (bar * 16 + step + (self.swing if int(step) % 2 else 0)) * self.step

# ---- Golden Ruins: a cat burglar's funk break, E Dorian ----


GOLD_BASS = ("E2:2 R:1 E3:1 R:2 D3:1 E3:1 R:2 G2:2 A2:2 B2:2", "E2:2 R:1 E3:1 R:2 D3:1 E3:1 R:2 B2:1 R:1 D3:2 Bb2:1 A2:1")
GOLD_BASS_WALK = ("E2:3 G2:1 R:2 A2:2 R:1 B2:1 R:2 D3:2 B2:2", "E2:3 G2:1 R:2 A2:2 R:1 B2:1 R:2 E3:1 D3:1 Bb2:2")
GOLD_BASS_OCTAVE = ("E2:1 E3:1 R:1 E2:1 R:2 E3:2 R:2 E2:1 E3:1 D3:2 B2:2", "E2:1 E3:1 R:1 E2:1 R:2 E3:2 R:2 G2:1 A2:1 Bb2:1 B2:1 D3:2")
HAT_BUSY = "xgxgxgxgxgxgxgox"
# D-289: a new track every 5 levels (MusicImport.TrackForLevel). The first is the track the developer liked; the rest add GOLD_TUNES.
GOLD_BEATS = (
    dict(bpm=138.0, swing=0.16, kick=("x.....x...x.....", "x.x.......x..x.."), snare=("....X..g.g..X..g", "....X..g.g..X.g."),
         hat="x.x.x.x.x.x.x.ox", rim="...x.....x....x.", stab="...x..x.......x.", bass=GOLD_BASS),
    dict(bpm=140.0, swing=0.12, kick=("x...x...x...x...", "x...x...x...x.x."), snare=("....X.......X...", "....X.......X.gg"),      # four on the floor
         hat="..x...x...x...ox", rim="......x.......x.", stab="x..x......x.....", bass=GOLD_BASS_OCTAVE),
    dict(bpm=134.0, swing=0.24, kick=("x.......x.x.....", "x.x.....x.....x."), snare=("....X.......X..g", "....X..g....X..."),      # a lazy boom-bap shuffle
         hat="x.x.x.x.x.x.x.x.", rim=".....x.....x....", stab="......x...x.....", bass=GOLD_BASS_WALK),
    dict(bpm=142.0, swing=0.10, kick=("xx....x...x..x..", "x.....xx..x.....",), snare=("....X..g..g.X...", "....X.g...g.X.g."),     # a busy double kick
         hat="xgxgxgxgxgxgxgox", rim="..x.....x.....x.", stab="...x.....x...x..", bass=GOLD_BASS),
    dict(bpm=136.0, swing=0.20, kick=("x.........x.....", "x.....x...x..x.."), snare=("........X.....g.", "........X..g..g."),     # half time: the snare on three
         hat="x.x.x.oxx.x.x.ox", rim="...x...x...x....", stab="..x.....x.......", bass=GOLD_BASS_WALK),
    dict(bpm=144.0, swing=0.14, kick=("x.....x.x.....x.", "x.....x.....x.x."), snare=("....X..g....X...", "....X..g....X.gg"),     # two-step, the fastest, for the world's end
         hat="x.oxx.oxx.oxx.ox", rim="...x.......x....", stab=".x....x.......x.", bass=GOLD_BASS_OCTAVE),
)
GOLD_VAMP = ("D4 F#4 G4 B4", "C#4 F#4 G4 B4")     # Em9, A13: two chords, no tune
GOLD_TURN = (("C3", "E4 G4 B4 D5"), ("C3", "E4 G4 B4 D5"), ("B2", "D#4 A4 D5"), ("B2", "D#4 A4 D5"),
             ("A2", "C4 E4 G4 B4"), ("A2", "C4 E4 G4 B4"), ("B2", "D#4 A4 D5"), ("B2", "D#4 A4 D5"))
# D-289 amendment: from the second on, a tune of its own: key, chords, lead instrument, bass line and hook, not only the drums.
# keys: the stab's instrument. prog: (root, chord) a bar. walk: the bass line, (semitones over the root, step, length). hook: two bars for the lead.
GOLD_TUNES = {
    1: dict(key="G minor", keys=organ, prog=(("G2", "F4 Bb4 D5"), ("G2", "F4 Bb4 D5"), ("Eb2", "D4 G4 Bb4"), ("D2", "F#4 C5 F5")),
            walk=((0, 0, 2), (12, 3, 1), (7, 6, 2), (10, 9, 1), (12, 10, 2), (7, 13, 1), (10, 14, 2)), hook="G4:2 R:1 Bb4:1 R:2 C5:2 R:2 Bb4:1 G4:1 R:2 F4:2 R:16"),
    2: dict(key="C minor", keys=clav, prog=(("C3", "Bb4 Eb5 G5"), ("Ab2", "C5 Eb5 G5"), ("F2", "C5 Eb5 Ab5"), ("G2", "B4 F5 Bb5")),
            walk=((0, 0, 3), (7, 4, 1), (10, 6, 2), (0, 8, 2), (3, 11, 2), (5, 13, 1), (7, 14, 2)), hook="R:8 Eb5:1 C5:1 R:2 G4:3 R:1 R:4 Bb4:1 C5:1 R:1 Eb5:2 R:7"),
    3: dict(key="A minor", keys=brass, prog=(("A2", "C4 E4 G4"), ("F2", "C4 E4 A4"), ("D3", "C4 F4 A4"), ("E2", "G#3 D4 G4")),
            walk=((0, 0, 1), (12, 2, 1), (0, 4, 1), (12, 6, 1), (10, 8, 1), (7, 10, 2), (3, 12, 1), (5, 14, 2)), hook="A4:1 R:1 A4:1 C5:1 R:2 E5:2 D5:1 R:1 C5:1 R:1 A4:2 R:18"),
    4: dict(key="D Dorian", keys=epiano, prog=(("D3", "C4 F4 A4 E5"), ("D3", "C4 F4 A4 E5"), ("G2", "B3 F4 A4 E5"), ("G2", "B3 F4 A4 E5")),
            walk=((0, 0, 4), (12, 6, 1), (10, 7, 1), (7, 8, 4), (5, 13, 1), (3, 14, 2)), hook="R:16 D5:2 F5:1 G5:1 R:2 A5:3 R:1 C6:2 A5:1 G5:1 R:2"),
    5: dict(key="F# minor", keys=clav, prog=(("F#2", "E4 A4 C#5"), ("D3", "F#4 A4 C#5"), ("B2", "D#4 A4 C#5"), ("C#3", "E#4 B4 D5")),
            walk=((0, 0, 2), (0, 3, 1), (12, 6, 2), (0, 8, 1), (0, 10, 1), (10, 12, 2), (7, 14, 2)), hook="C#5:1 R:1 C#5:1 R:1 E5:2 F#5:2 R:2 E5:1 C#5:1 R:4 A4:1 B4:1 R:2 C#5:2 R:10"),
}
# Name, bars, what plays: drums, bass, keys, guitar.
GOLD_FORM = (("Intro", 4, "d"), ("A", 8, "dbk"), ("A2", 8, "dbkg"), ("Break", 4, "bk"), ("Turn", 8, "dbkg"), ("A3", 8, "dbkgh"))


def turn_bass(root):
    r = midi(root)
    return ((r, 0, 2), (r + 12, 3, 1), (r + 10, 6, 1), (r + 12, 7, 1), (r, 10, 2), (r + 7, 13, 1), (r + 10, 14, 2))


def golden(beat=0):
    v = GOLD_BEATS[beat]
    tune = GOLD_TUNES.get(beat)
    bpm = v["bpm"]
    g = Grid(bpm, swing=v["swing"])
    bars = sum(s[1] for s in GOLD_FORM)
    m = Mix(bars * g.bar, seed=2041 + beat)
    rng = m.rng
    m.stem("kick", -3.5, reverb=0.04)
    m.stem("snare", -5.5, reverb=0.16)
    m.stem("hat", -15.0, reverb=0.05)
    m.stem("rim", -17.0, reverb=0.18)
    m.stem("bass", -4.0, reverb=0.03, duck=0.45)
    m.stem("keys", -11.5, reverb=0.3, echo=0.2, duck=0.25)
    m.stem("guitar", -17.0, reverb=0.12)
    m.stem("scratch", -11.0, reverb=0.14, echo=0.14)
    m.stem("riser", -20.0, reverb=0.3)
    m.stem("crackle", -34.0, reverb=0.0)
    if tune:
        m.stem("lead", -9.0, reverb=0.22, echo=0.22)

    kicks, at = [], 0
    for name, length, plays in GOLD_FORM:
        if tune and name == "Intro":
            plays = "dbk"       # a new block's level starts here: its tune from the first bar
        for i in range(length):
            b = at + i
            last = i == length - 1
            if "d" in plays:
                for k, vel, _ in hits(v["kick"][i % 2]):
                    m.put("kick", g.at(b, k), kick(rng) * vel)
                    kicks.append(g.at(b, k))
                for k, vel, _ in hits(v["snare"][i % 2] if not last else "....X..g.g.xX.xx"):
                    m.put("snare", g.at(b, k), snare(rng, vel=vel), pan=0.05)
            for k, vel, c in hits(HAT_BUSY if "h" in plays or name == "Turn" else v["hat"]):
                m.put("hat", g.at(b, k), hat(rng, vel, tail=0.09 if c == "o" else 0.024), pan=0.22)
            for k, vel, _ in hits(v["rim"]):
                m.put("rim", g.at(b, k), rim(vel=vel), pan=-0.3)
            if tune:
                root, chord = tune["prog"][(i + (2 if name == "Turn" else 0)) % len(tune["prog"])]
                if "b" in plays:
                    for up, step, size in tune["walk"]:
                        m.put("bass", g.at(b, step), slap_bass(hz(midi(root) + up), size * g.step * 0.92, 1.0 if step == 0 else 0.82))
                if name not in ("Break", "Turn") and i % 2 == 0:
                    for step, size, note in parse(tune["hook"]):
                        m.put("lead", g.at(b, step), lead(hz(note), size * g.step * 0.9, 1.0), pan=0.12)
            elif "b" in plays and name == "Turn":
                root, chord = GOLD_TURN[i]
                for note, step, size in turn_bass(root):
                    m.put("bass", g.at(b, step), slap_bass(hz(note), size * g.step * 0.92, 1.0 if step == 0 else 0.82))
            elif "b" in plays:
                for step, size, note in parse(v["bass"][i % 2]):
                    m.put("bass", g.at(b, step), slap_bass(hz(note), size * g.step * 0.92, 1.0 if step == 0 else 0.82))
            if "k" in plays:
                if not tune:
                    chord = GOLD_TURN[i][1] if name == "Turn" else GOLD_VAMP[i % 2]
                keys = tune["keys"] if tune else epiano
                for k, vel, _ in hits(v["stab"]):
                    for j, note in enumerate(chord.split()):
                        m.put("keys", g.at(b, k) + j * 0.004, keys(hz(note), g.step * 1.4, vel), pan=-0.2 + 0.13 * j)
            if "g" in plays:
                for k in range(16):
                    if k % 4 == 2 or k % 8 == 5 or k == 15:
                        m.put("guitar", g.at(b, k), muted_guitar(hz(tune["prog"][0][0]) * 4 if tune else hz("E4"), 1.0 if k % 4 == 2 else 0.6), pan=0.42)
            if name == "Intro" and last or name == "Break" or (name != "Intro" and last and i % 8 == 7):
                m.put("scratch", g.at(b, 8), scratch(rng, g.step * 8, strokes=4), pan=-0.3)
            if last:
                m.put("riser", g.at(b, 0), riser(rng, g.bar), pan=0.1)
        at += length
    m.pump(kicks)
    pops = loop_noise(m.n, 1500, 7000, 31) * (np.abs(loop_noise(m.n, 3, 40, 32)[:, :1]) > 2.4)
    m.fill("crackle", pops + 0.2 * loop_noise(m.n, 500, 3000, 33))
    return m.render(0.9, echo_seconds=g.step * 3), dict(meter="4/4", bpm=bpm, bars=bars, key=tune["key"] if tune else "E Dorian")

# ---- Obsidian Void: dark phonk, D Phrygian ----


COLD_BEATS = (
    dict(bpm=148.0, key="D Phrygian", kick=("x.....x...x.....", "x.....x..x..x..."), clap="....X.......X...", shift=0),
    # D-289 amendment: its own tune: a minor third up, half time, a riff and 808 line of its own.
    dict(bpm=152.0, key="F minor", kick=("x..x......x.....", "x..x...x..x..x.."), clap="........X.......", shift=3,
         bell=("F5:1 R:1 Ab5:1 R:1 F5:1 C6:2 R:1 Bb5:2 Ab5:2 F5:1 E5:1 R:2", "F5:1 R:1 Ab5:1 R:1 Bb5:1 C6:2 R:1 Db6:2 C6:2 Ab5:1 Bb5:1 R:2"),
         sub=("F2:4 F2:2 Ab2:2 F2:4 Db2:4", "F2:4 F2:2 Bb2:2 Ab2:4 E2:4")),
)
COLD_HAT = "x.x.x.x.x.x.x.x."
COLD_BELL = ("D5:2 D5:1 F5:1 D5:2 Eb5:2 D5:2 C5:1 D5:1 R:2 A4:2", "D5:2 D5:1 F5:1 G5:2 F5:2 Eb5:2 D5:2 C5:2 A4:2")
COLD_808 = ("D2:6 D2:4 F2:2 Eb2:4", "D2:6 D2:4 C2:2 A1:4")
COLD_TURN = ("Bb1", "Bb1", "A1", "A1", "Bb1", "Bb1", "Eb2", "A1")
# Name, bars, what plays: drums, 808, cowbell, reese.
COLD_FORM = (("Intro", 4, "c"), ("A", 8, "dbc"), ("A2", 8, "dbcr"), ("Break", 4, "cr"), ("Turn", 8, "dbcr"), ("A3", 8, "dbc"))


def obsidian(beat=0):
    v = COLD_BEATS[beat]
    bpm = v["bpm"]
    g = Grid(bpm)
    bars = sum(s[1] for s in COLD_FORM)
    m = Mix(bars * g.bar, seed=3107 + beat)
    rng = m.rng
    m.stem("kick", -3.5, reverb=0.03)
    m.stem("clap", -6.5, reverb=0.2)
    m.stem("hat", -15.5, reverb=0.04)
    m.stem("sub", -3.5, reverb=0.02, duck=0.5)
    m.stem("bell", -8.0, reverb=0.14, echo=0.2)
    m.stem("reese", -10.5, reverb=0.08, duck=0.4)
    m.stem("glass", -20.0, reverb=0.4, echo=0.4)
    m.stem("riser", -19.0, reverb=0.3)
    m.stem("air", -33.0, reverb=0.0)

    kicks, at = [], 0
    for name, length, plays in COLD_FORM:
        for i in range(length):
            b = at + i
            last = i == length - 1
            if "d" in plays:
                for k, vel, _ in hits(v["kick"][i % 2]):
                    m.put("kick", g.at(b, k), kick(rng, f=50.0, drive=3.2) * vel)
                    kicks.append(g.at(b, k))
                for k, vel, _ in hits(v["clap"]):
                    m.put("clap", g.at(b, k), clap(rng, vel))
                    m.put("clap", g.at(b, k), snare(rng, tone=170.0, vel=0.5 * vel))
            for k, vel, _ in hits(COLD_HAT):
                m.put("hat", g.at(b, k), hat(rng, vel * (1.0 if k % 4 == 0 else 0.7), tail=0.02), pan=0.2)
            if i % 2 == 1 and "d" in plays:       # the trap roll
                for k in range(8):
                    m.put("hat", g.at(b, 12 + k * 0.5), hat(rng, 0.45 + 0.07 * k, tail=0.012), pan=0.2)
            if "b" in plays:
                if name == "Turn":
                    m.put("sub", g.at(b, 0), sub808(hz(midi(COLD_TURN[i]) + v["shift"]), g.step * 7, 1.0))
                    m.put("sub", g.at(b, 10), sub808(hz(midi(COLD_TURN[i]) + v["shift"]), g.step * 5, 0.85, slide_from=hz(midi(COLD_TURN[i]) + v["shift"]) * 1.5))
                else:
                    for step, size, note in parse(v.get("sub", COLD_808)[i % 2]):
                        m.put("sub", g.at(b, step), sub808(hz(note), size * g.step, 1.0 if step == 0 else 0.85, slide_from=hz(note) * 1.33 if step > 6 else None))
            if "c" in plays:
                shift = -2 if name == "Turn" and i % 4 < 2 else 0
                for step, size, note in parse(v.get("bell", COLD_BELL)[i % 2]):
                    m.put("bell", g.at(b, step), cowbell(hz(note + shift), size * g.step * 0.8, 1.0 if step % 4 == 0 else 0.75), pan=-0.12)
            if "r" in plays:
                root = hz(midi(COLD_TURN[i] if name == "Turn" else "D2") + v["shift"])
                for step, size in ((0, 3), (6, 2), (10, 3)):
                    m.put("reese", g.at(b, step), reese(root * 2, size * g.step, 1.0))
            if i % 4 == 3:
                m.put("glass", g.at(b, 14), glass(hz("A6")), pan=0.5)
            if last:
                m.put("riser", g.at(b, 0), riser(rng, g.bar), pan=-0.1)
        at += length
    m.pump(kicks, release=0.14)
    m.fill("air", loop_noise(m.n, 220, 1800, 41))
    return m.render(0.8, echo_seconds=g.step * 3), dict(meter="4/4", bpm=bpm, bars=bars, key=v["key"])

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


TRACKS = {"GoldenRuins" + (str(k + 1) if k else ""): (lambda k=k: golden(k)) for k in range(len(GOLD_BEATS))}
TRACKS.update({"ObsidianVoid" + (str(k + 1) if k else ""): (lambda k=k: obsidian(k)) for k in range(len(COLD_BEATS))})


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
