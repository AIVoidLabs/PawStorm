"""
PawStorm procedural audio: music loops (one per theme), ambience loops and extra SFX.
Pure numpy synthesis (no external assets). Run with Blender's bundled Python (it ships numpy):

  "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" --background --python Docs/tools/audio/make_audio.py

Output: Audio/Music/*.wav, Audio/Ambient/*.wav, Audio/*.wav (16-bit mono, 22050 Hz).
Env var PAWSTORM_ONLY = comma list of file stems to (re)build.
"""
import math
import os
import random
import wave

import numpy as np

SR = 22050
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
OUT = os.path.join(ROOT, "Audio")
ONLY = [s for s in os.environ.get("PAWSTORM_ONLY", "").split(",") if s]


# ---------------------------------------------------------------- basics
def mtof(m):
    return 440.0 * 2 ** ((m - 69) / 12.0)


def tt(d):
    return np.arange(int(d * SR)) / SR


def adsr(n, a=0.005, d=0.08, s=0.6, r=0.05):
    a, d, r = int(a * SR), int(d * SR), int(r * SR)
    e = np.ones(n) * s
    a = min(a, n)
    e[:a] = np.linspace(0, 1, a, endpoint=False) if a else e[:a]
    dd = min(d, max(0, n - a))
    if dd:
        e[a:a + dd] = np.linspace(1, s, dd)
    r = min(r, n)
    if r:
        e[n - r:] *= np.linspace(1, 0, r)
    return e


def osc(kind, f, t, width=0.5):
    ph = (f * t) % 1.0
    if kind == "sine":
        return np.sin(2 * np.pi * ph)
    if kind == "square":
        return np.where(ph < width, 1.0, -1.0)
    if kind == "tri":
        return 2 * np.abs(2 * ph - 1) - 1
    if kind == "saw":
        return 2 * ph - 1
    raise ValueError(kind)


def lowpass(x, cutoff):
    spec = np.fft.rfft(x)
    freqs = np.fft.rfftfreq(len(x), 1 / SR)
    spec *= 1.0 / (1.0 + (freqs / cutoff) ** 4)
    return np.fft.irfft(spec, len(x))


def highpass(x, cutoff):
    return x - lowpass(x, cutoff)


def bandpass(x, lo, hi):
    return lowpass(highpass(x, lo), hi)


def noise(n, rng):
    return rng.uniform(-1, 1, n)


def place(buf, start, sig, gain=1.0):
    i = int(start * SR)
    if i >= len(buf):
        return
    j = min(len(buf), i + len(sig))
    buf[i:j] += sig[:j - i] * gain


def loopfix(buf, tail):
    """Fold the render tail back to the start so the loop is seamless."""
    n = len(buf) - tail
    out = buf[:n].copy()
    out[:tail] += buf[n:]
    return out


def seamless(x, fade):
    """Make a (L + fade) noise-like render loop without a click."""
    L = len(x) - fade
    out = x[:L].copy()
    w = np.linspace(0, 1, fade)
    out[:fade] = x[:fade] * w + x[L:L + fade] * (1 - w)
    return out


def save(path, x, peak=0.85):
    x = np.asarray(x, dtype=np.float64)
    m = np.max(np.abs(x)) or 1.0
    x = np.tanh(x / m * 1.3) / np.tanh(1.3) * peak
    data = (x * 32767).astype("<i2").tobytes()
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data)
    print("WROTE", os.path.relpath(path, ROOT), "%.1fs" % (len(x) / SR))


# ---------------------------------------------------------------- instruments
def pluck(f, d, kind="tri", decay=0.18):
    t = tt(d)
    return osc(kind, f, t) * np.exp(-t / decay) * adsr(len(t), 0.002, 0.0, 1.0, 0.02)


def lead(f, d, kind="square", width=0.25, vib=0.0, a=0.01, r=0.06):
    t = tt(d)
    f_t = f * (1 + vib * np.sin(2 * np.pi * 5.5 * t))
    ph = np.cumsum(f_t) / SR % 1.0
    if kind == "square":
        s = np.where(ph < width, 1.0, -1.0)
    elif kind == "saw":
        s = 2 * ph - 1
    elif kind == "tri":
        s = 2 * np.abs(2 * ph - 1) - 1
    else:
        s = np.sin(2 * np.pi * ph)
    return s * adsr(len(t), a, 0.05, 0.75, r)


def bell(f, d, decay=0.9):
    t = tt(d)
    s = np.sin(2 * np.pi * f * t) + 0.5 * np.sin(2 * np.pi * f * 2.76 * t) * np.exp(-t / 0.25) + 0.25 * np.sin(2 * np.pi * f * 5.4 * t) * np.exp(-t / 0.12)
    return s * np.exp(-t / decay) * adsr(len(t), 0.001, 0.0, 1.0, 0.05)


def pad(f, d):
    t = tt(d)
    s = osc("tri", f, t) + 0.6 * osc("tri", f * 1.004, t) + 0.6 * osc("tri", f * 0.996, t)
    return s * adsr(len(t), min(0.6, d * 0.4), 0.1, 0.8, min(0.6, d * 0.4)) * 0.4


def bass(f, d, kind="tri"):
    t = tt(d)
    s = osc(kind, f, t) * 0.8 + osc("sine", f, t) * 0.5
    return s * adsr(len(t), 0.004, 0.06, 0.8, 0.04)


def kick(rng=None):
    t = tt(0.28)
    f = 45 + 110 * np.exp(-t * 26)
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / 0.11)


def snare(rng):
    t = tt(0.22)
    return (noise(len(t), rng) * np.exp(-t / 0.07) * 0.7 + np.sin(2 * np.pi * 190 * t) * np.exp(-t / 0.05) * 0.5)


def hat(rng, open_=False):
    t = tt(0.18 if open_ else 0.05)
    return highpass(noise(len(t), rng), 6000) * np.exp(-t / (0.06 if open_ else 0.015)) * 1.4


def clank(rng):
    t = tt(0.3)
    s = noise(len(t), rng) * np.exp(-t / 0.02) * 0.6
    for f in (410, 977, 1630, 2410):
        s += np.sin(2 * np.pi * f * t) * np.exp(-t / 0.1) * 0.25
    return s


# ---------------------------------------------------------------- music
SCALES = {
    "major": [0, 2, 4, 5, 7, 9, 11], "minor": [0, 2, 3, 5, 7, 8, 10], "mixo": [0, 2, 4, 5, 7, 9, 10],
    "lydian": [0, 2, 4, 6, 7, 9, 11], "phrygian": [0, 1, 3, 5, 7, 8, 10], "dorian": [0, 2, 3, 5, 7, 9, 10],
}


def scale_note(root, scale, degree):
    s = SCALES[scale]
    octv, idx = divmod(degree, 7)
    return root + 12 * octv + s[idx]


def compose(name, bpm, root, scale, prog, seed, lead_kind, drums, arp=None, pad_on=True, bass_style="root8", lead_oct=1,
            swing=0.0, density=0.7, bars=16, lead_gain=0.55, extra=None):
    rng = np.random.default_rng(seed)
    rnd = random.Random(seed)
    beat = 60.0 / bpm
    bar = beat * 4
    step = beat / 4
    total = bar * bars
    tail = int(2.0 * SR)
    buf = np.zeros(int(total * SR) + tail)
    ln = lambda g: g * lead_gain
    cur = 4  # melody degree cursor (relative to root)
    for b in range(bars):
        t0 = b * bar
        deg = prog[b % len(prog)]
        chord = [deg, deg + 2, deg + 4]
        # pad
        if pad_on:
            for c in chord:
                place(buf, t0, pad(mtof(scale_note(root, scale, c) + 12), bar * 1.02), 0.16)
        # bass
        bn = mtof(scale_note(root, scale, deg) - 12)
        if bass_style == "root8":
            for i in range(8):
                place(buf, t0 + i * beat / 2, bass(bn * (2 if i % 4 == 3 else 1), beat / 2 * 0.9), 0.32)
        elif bass_style == "walk":
            seq = [0, 0, 4, 0, 5, 4, 2, 4]
            for i in range(8):
                place(buf, t0 + i * beat / 2, bass(mtof(scale_note(root, scale, deg + seq[i]) - 12), beat / 2 * 0.9), 0.3)
        elif bass_style == "drive16":
            for i in range(16):
                if i % 4 == 2 and i % 8 != 6:
                    continue
                place(buf, t0 + i * step, bass(bn * (2 if i % 8 == 7 else 1), step * 0.9, "saw"), 0.26)
        elif bass_style == "long":
            place(buf, t0, bass(bn, bar * 0.95), 0.35)
        # arp
        if arp:
            pat = {"up": [0, 1, 2, 1], "up2": [0, 2, 1, 2, 0, 2, 1, 2], "fast": [0, 1, 2, 3, 2, 1]}[arp[0]]
            div = arp[1]
            ch = chord + [deg + 7]
            for i in range(div):
                k = ch[pat[i % len(pat)] % len(ch)]
                m = scale_note(root, scale, k) + 12 * arp[2]
                ln_ = bar / div
                inst = pluck(mtof(m), ln_ * 0.95, "tri", 0.12) if arp[3] == "pluck" else bell(mtof(m), ln_ * 1.5, 0.35)
                place(buf, t0 + i * ln_, inst, 0.20)
        # melody (A: bars 0-7, B: 8-15 varies)
        mel_rng = random.Random(seed * 100 + (b % 8) + (0 if b < 8 else 50))
        rhythm = mel_rng.choice([[0, 4, 6, 8, 12], [0, 3, 6, 8, 10, 12], [0, 2, 4, 8, 10, 12, 14], [0, 4, 8, 11, 12], [0, 6, 8, 12, 14]])
        for r_i, pos in enumerate(rhythm):
            if mel_rng.random() > density + (0.25 if pos % 4 == 0 else 0):
                continue
            if pos % 8 == 0:
                cur = min(chord, key=lambda c: abs((c + 7) - cur)) + 7  # land on a chord tone
            else:
                cur += mel_rng.choice([-2, -1, -1, 0, 1, 1, 2])
            cur = max(2, min(cur, 13))
            nxt = rhythm[r_i + 1] if r_i + 1 < len(rhythm) else 16
            dur = (nxt - pos) * step * 0.92
            ts = t0 + pos * step + (step * swing if pos % 2 else 0)
            m = scale_note(root, scale, cur) + 12 * lead_oct
            if lead_kind == "bell":
                sig = bell(mtof(m), max(dur, 0.4), 0.7)
            elif lead_kind == "pluck":
                sig = pluck(mtof(m + 12), max(dur, 0.25), "tri", 0.22)
            elif lead_kind == "flute":
                sig = lead(mtof(m), dur, "sine", vib=0.006, a=0.05, r=0.1)
            else:
                sig = lead(mtof(m), dur, lead_kind, width=0.25 if lead_kind == "square" else 0.5, vib=0.003)
            place(buf, ts, sig, ln(0.5))
        # drums
        for i in range(16):
            ts = t0 + i * step + (step * swing if i % 2 else 0)
            if drums.get("kick", [0] * 16)[i]:
                place(buf, ts, kick(), 0.75)
            if drums.get("snare", [0] * 16)[i]:
                place(buf, ts, snare(rng), 0.45)
            if drums.get("hat", [0] * 16)[i]:
                place(buf, ts, hat(rng), 0.18)
            if drums.get("open", [0] * 16)[i]:
                place(buf, ts, hat(rng, True), 0.14)
            if drums.get("clank", [0] * 16)[i]:
                place(buf, ts, clank(rng), 0.28)
    out = loopfix(buf, tail)
    save(os.path.join(OUT, "Music", name + ".wav"), out)


K4 = [1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0]
K2 = [1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0]
KS = [1, 0, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0]
KD = [1, 0, 1, 0, 1, 0, 0, 0, 1, 0, 1, 0, 1, 0, 0, 1]
SN = [0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0]
H8 = [1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0]
H16 = [1, 1, 1, 1] * 4
OP = [0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0]

MUSIC = {
    "menu": dict(bpm=96, root=60, scale="major", prog=[0, 5, 3, 4], seed=11, lead_kind="bell", drums=dict(kick=K2, hat=H8), arp=("up2", 8, 0, "bell"), bass_style="long", lead_oct=0, density=0.55),
    "rooftops": dict(bpm=138, root=62, scale="major", prog=[0, 4, 5, 3], seed=21, lead_kind="square", drums=dict(kick=K4, snare=SN, hat=H8, open=OP), arp=("fast", 8, 1, "pluck"), bass_style="root8", density=0.75),
    "harbor": dict(bpm=116, root=55, scale="mixo", prog=[0, 6, 3, 0], seed=31, lead_kind="pluck", drums=dict(kick=KS, snare=SN, hat=H8), arp=("up", 4, 1, "pluck"), bass_style="walk", swing=0.25, density=0.7),
    "forest": dict(bpm=104, root=53, scale="lydian", prog=[0, 3, 4, 1], seed=41, lead_kind="flute", drums=dict(kick=K2, hat=[1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0]), arp=("up2", 8, 1, "pluck"), bass_style="long", lead_oct=1, density=0.6),
    "factory": dict(bpm=126, root=50, scale="minor", prog=[0, 0, 5, 6], seed=51, lead_kind="saw", drums=dict(kick=K4, snare=SN, hat=H16, clank=[0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1]), bass_style="drive16", pad_on=False, density=0.65),
    "sky": dict(bpm=120, root=64, scale="lydian", prog=[0, 4, 1, 3], seed=61, lead_kind="bell", drums=dict(kick=K2, hat=H8, open=OP), arp=("fast", 16, 1, "bell"), bass_style="long", lead_oct=0, density=0.6),
    "crystal": dict(bpm=108, root=57, scale="dorian", prog=[0, 5, 3, 4], seed=81, lead_kind="bell", drums=dict(kick=K2, hat=H8), arp=("up2", 8, 1, "bell"), bass_style="long", lead_oct=0, density=0.5),
    "temple": dict(bpm=112, root=50, scale="dorian", prog=[0, 3, 5, 4], seed=91, lead_kind="pluck", drums=dict(kick=KS, snare=SN, hat=H8), arp=("up", 4, 1, "pluck"), bass_style="walk", swing=0.15, density=0.7),
    "boss2": dict(bpm=164, root=47, scale="phrygian", prog=[0, 0, 1, 6], seed=101, lead_kind="saw", drums=dict(kick=KD, snare=SN, hat=H16, open=OP), arp=("fast", 16, 1, "pluck"), bass_style="drive16", density=0.85, lead_gain=0.62),
    "boss": dict(bpm=152, root=52, scale="phrygian", prog=[0, 1, 0, 6], seed=71, lead_kind="saw", drums=dict(kick=KD, snare=SN, hat=H16, open=OP), arp=("fast", 16, 1, "pluck"), bass_style="drive16", density=0.8, lead_gain=0.6),
}


# ---------------------------------------------------------------- ambience (10 s seamless loops)
def amb_wind(rng, gain=1.0, lo=120, hi=900, gust=0.25):
    n = int(10 * SR) + int(1.0 * SR)
    x = bandpass(noise(n, rng), lo, hi)
    t = np.arange(n) / SR
    mod = 0.55 + 0.45 * np.sin(2 * np.pi * 0.2 * t + 1) * np.sin(2 * np.pi * 0.13 * t)
    return seamless(x * mod * (1 + gust), int(1.0 * SR)) * gain


def amb_waves(rng):
    n = int(10 * SR) + int(1.0 * SR)
    t = np.arange(n) / SR
    swell = (0.5 + 0.5 * np.sin(2 * np.pi * 0.2 * t - 1.2)) ** 2
    low = lowpass(noise(n, rng), 500) * 1.4
    foam = highpass(noise(n, rng), 2500) * 0.25 * swell
    return seamless((low * (0.3 + 0.7 * swell) + foam), int(1.0 * SR))


def amb_factory(rng):
    n = int(10 * SR) + int(1.0 * SR)
    t = np.arange(n) / SR
    hum = sum(np.sin(2 * np.pi * f * t) * a for f, a in ((55, 0.5), (110, 0.35), (165, 0.18), (220, 0.1)))
    rumble = lowpass(noise(n, rng), 160) * 1.2
    x = hum * 0.5 + rumble
    for k in range(5):  # periodic steam hiss / clank
        s0 = int((0.7 + 2.0 * k) * SR)
        h = highpass(noise(int(0.5 * SR), rng), 2500) * np.exp(-np.arange(int(0.5 * SR)) / (0.15 * SR)) * 0.3
        x[s0:s0 + len(h)] += h
    return seamless(x, int(1.0 * SR))


def amb_forest(rng):
    base = amb_wind(rng, 0.45, 150, 700, 0.1)
    out = base.copy()
    rnd = random.Random(7)
    for _ in range(9):  # bird chirps
        st = rnd.uniform(0.3, 9.0)
        f0 = rnd.uniform(2200, 3800)
        for k in range(rnd.choice([2, 3, 4])):
            d = 0.09
            t = tt(d)
            f = f0 * (1 + 0.35 * np.sin(np.pi * t / d)) * (1 + 0.1 * k)
            chirp = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-((t - d / 2) ** 2) / (2 * (d / 4) ** 2)) * 0.18
            place(out, st + k * 0.13, chirp)
    return out


def amb_sky(rng):
    base = amb_wind(rng, 0.9, 300, 2500, 0.5)
    out = base.copy()
    for st, m in ((1.2, 88), (4.4, 91), (7.1, 84)):  # distant chimes
        place(out, st, bell(mtof(m), 2.4, 1.0), 0.06)
    return out


def amb_rooftops(rng):
    return amb_wind(rng, 0.9, 100, 700, 0.35)


def amb_boss(rng):
    n = int(10 * SR) + int(1.0 * SR)
    t = np.arange(n) / SR
    x = lowpass(noise(n, rng), 120) * 1.5 + np.sin(2 * np.pi * 41 * t) * 0.35 + np.sin(2 * np.pi * 61.5 * t) * 0.2 * (0.6 + 0.4 * np.sin(2 * np.pi * 0.3 * t))
    return seamless(x, int(1.0 * SR))


def amb_crystal(rng):
    n = int(10 * SR) + int(1.0 * SR)
    t = np.arange(n) / SR
    hum = (np.sin(2 * np.pi * 55 * t) * 0.35 + np.sin(2 * np.pi * 82.5 * t) * 0.2) * (0.7 + 0.3 * np.sin(2 * np.pi * 0.2 * t))
    x = hum + lowpass(noise(n, rng), 300) * 0.5
    rnd = random.Random(3)
    for _ in range(7):  # water drips with a soft echo
        st = rnd.uniform(0.3, 9.0)
        f = rnd.uniform(1100, 2000)
        tt_ = tt(0.18)
        drip = np.sin(2 * np.pi * f * tt_ * (1 + 0.25 * np.exp(-tt_ * 30))) * np.exp(-tt_ / 0.04) * 0.25
        place(x, st, drip)
        place(x, st + 0.22, drip * 0.4)
    return seamless(x, int(1.0 * SR))


def amb_temple(rng):
    out = amb_wind(rng, 0.8, 120, 800, 0.4)
    for st, m in ((1.5, 86), (3.8, 91), (6.6, 83), (8.4, 88)):
        place(out, st, bell(mtof(m), 2.0, 0.9), 0.05)
    return out


def amb_storm(rng):
    n = int(10 * SR) + int(1.0 * SR)
    t = np.arange(n) / SR
    rain = highpass(noise(n, rng), 2500) * 0.28
    rumble = lowpass(noise(n, rng), 90) * 1.4 * (0.6 + 0.4 * np.sin(2 * np.pi * 0.17 * t))
    x = rain + rumble
    for st in (2.2, 6.8):  # thunder rolls
        tt_ = tt(2.2)
        roll = lowpass(noise(len(tt_), rng), 140) * np.exp(-tt_ / 0.9) * np.minimum(1, tt_ / 0.08) * 1.6
        place(x, st, roll)
    return seamless(x, int(1.0 * SR))

AMBIENT = {"amb_rooftops": amb_rooftops, "amb_harbor": amb_waves, "amb_forest": amb_forest, "amb_factory": amb_factory,
           "amb_sky": amb_sky, "amb_boss": amb_boss, "amb_crystal": amb_crystal, "amb_temple": amb_temple, "amb_storm": amb_storm}


# ---------------------------------------------------------------- sfx
def sfx_land(rng):
    t = tt(0.22)
    return np.sin(2 * np.pi * np.cumsum(90 * np.exp(-t * 6) + 40) / SR) * np.exp(-t / 0.06) + lowpass(noise(len(t), rng), 900) * np.exp(-t / 0.04) * 0.5


def sfx_step(rng):
    t = tt(0.07)
    return lowpass(noise(len(t), rng), 1800) * np.exp(-t / 0.02) * 0.8 + np.sin(2 * np.pi * 120 * t) * np.exp(-t / 0.02) * 0.4


def sfx_wall(rng):
    out = np.zeros(int(0.3 * SR))
    for k in range(3):
        t = tt(0.07)
        place(out, k * 0.09, highpass(noise(len(t), rng), 1800) * np.exp(-t / 0.02), 0.8)
    return out


def sfx_loop(rng):
    n = int(0.6 * SR)
    t = np.arange(n) / SR
    sweep = bandpass(noise(n, rng), 300, 4000)
    env = np.sin(np.pi * t / 0.6) ** 2
    f = 220 + 900 * (t / 0.6)
    return (sweep * 0.7 + np.sin(2 * np.pi * np.cumsum(f) / SR) * 0.25) * env


def sfx_uiclick(rng):
    t = tt(0.09)
    return (np.sin(2 * np.pi * 740 * t) + 0.5 * np.sin(2 * np.pi * 1110 * t)) * np.exp(-t / 0.03)


def sfx_uihover(rng):
    t = tt(0.05)
    return np.sin(2 * np.pi * 1250 * t) * np.exp(-t / 0.015) * 0.6


def sfx_boss_warn(rng):
    t = tt(0.7)
    f = np.where((t % 0.35) < 0.175, 660, 880)
    return np.sign(np.sin(2 * np.pi * np.cumsum(f) / SR)) * 0.5 * adsr(len(t), 0.01, 0.0, 1.0, 0.1)


def sfx_boss_stun(rng):
    t = tt(0.5)
    return np.sin(2 * np.pi * np.cumsum(420 * np.exp(-t * 3.5) + 60) / SR) * np.exp(-t / 0.25)


def sfx_grind_loop(rng):
    n = int(0.5 * SR)
    t = np.arange(n) / SR
    hiss = highpass(noise(n, rng), 3500) * 0.5 * (0.7 + 0.3 * np.sin(2 * np.pi * 8 * t))
    ring = np.sin(2 * np.pi * 2800 * t) * 0.18 * (0.6 + 0.4 * np.sin(2 * np.pi * 12 * t))
    return hiss + ring


def sfx_charge_loop(rng):
    n = int(0.5 * SR)
    t = np.arange(n) / SR
    buzz = np.where(((180 * t) % 1.0) < 0.3, 1.0, -1.0) * 0.4 + np.sin(2 * np.pi * 90 * t) * 0.4
    return buzz * (0.65 + 0.35 * np.sin(2 * np.pi * 12 * t))


def sfx_laser_fire(rng):
    t = tt(0.4)
    sweep = np.sin(2 * np.pi * np.cumsum(1500 * np.exp(-t * 7) + 260) / SR) * np.exp(-t / 0.16)
    return sweep * 0.8 + highpass(noise(len(t), rng), 3000) * np.exp(-t / 0.03) * 0.5


def sfx_laser_hit(rng):
    t = tt(0.4)
    return lowpass(noise(len(t), rng), 1800) * np.exp(-t / 0.1) * 0.9 + np.sin(2 * np.pi * np.cumsum(140 * np.exp(-t * 8) + 40) / SR) * np.exp(-t / 0.12)


def sfx_reflect(rng):
    t = tt(0.5)
    ping = (np.sin(2 * np.pi * 880 * t) + 0.6 * np.sin(2 * np.pi * 1320 * t) + 0.3 * np.sin(2 * np.pi * 1760 * t)) * np.exp(-t / 0.18)
    return ping * 0.6 + bandpass(noise(len(t), rng), 800, 5000) * np.exp(-t / 0.05) * 0.4


def sfx_vent(rng):
    t = tt(1.0)
    hiss = highpass(noise(len(t), rng), 1800) * np.minimum(1, t / 0.05) * np.exp(-t / 0.55)
    return hiss * 0.9 + np.sin(2 * np.pi * np.cumsum(500 * np.exp(-t * 2.5) + 90) / SR) * np.exp(-t / 0.4) * 0.3


def sfx_thunder(rng):
    t = tt(1.8)
    return lowpass(noise(len(t), rng), 220) * np.exp(-t / 0.7) * np.minimum(1, t / 0.03) * 1.6 + highpass(noise(len(t), rng), 2500) * np.exp(-t / 0.02) * 0.5

SFX = {"land": sfx_land, "step": sfx_step, "wall": sfx_wall, "loop": sfx_loop, "uiclick": sfx_uiclick, "uihover": sfx_uihover,
       "boss_warn": sfx_boss_warn, "boss_stun": sfx_boss_stun, "grind_loop": sfx_grind_loop, "charge_loop": sfx_charge_loop,
       "laser_fire": sfx_laser_fire, "laser_hit": sfx_laser_hit, "reflect": sfx_reflect, "vent": sfx_vent, "thunder": sfx_thunder}


def want(stem):
    return not ONLY or stem in ONLY


if __name__ == "__main__":
    for name, kw in MUSIC.items():
        if want(name):
            compose(name, **kw)
    for name, fn in AMBIENT.items():
        if want(name):
            save(os.path.join(OUT, "Ambient", name + ".wav"), fn(np.random.default_rng(sum(map(ord, name)))), peak=0.8)
    for name, fn in SFX.items():
        if want(name):
            save(os.path.join(OUT, name + ".wav"), fn(np.random.default_rng(5)), peak=0.8)
    print("DONE")
