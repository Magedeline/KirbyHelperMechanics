"""
Kirby dialogue voice syllables for Kirby Helper Mechanics.

Same idea as Celeste's dialogue voices (Kevin Regamey / Power Up Audio): a synth
tone shaped with formant filters so it reads as vowels, with a pitch contour per
emotion. Files mirror Madeline's set in the FMOD project
(Assets/character/dialogue/madeline/) name for name, 401 in total:

    char_dia_kirby_<emotion>_mid_A_01..10   passing syllables
    char_dia_kirby_<emotion>_mid_B_01..10   passing syllables
    char_dia_kirby_<emotion>_mid_C_01..10   emphasized syllables
    char_dia_kirby_<emotion>_per_01..10     line endings
    char_dia_kirby_thatisnice_solo          the "That *is* nice." phrase

for the 10 emotions of event:/char/dialogue/Celestellaris/kirby. relink_kirby_voice.py
then swaps that event's Madeline instruments for these, one for one.

Usage:  python generate_voice.py <output_dir>
        (normally D:/pusheen-fmod-project/Assets/character/dialogue/kirby)
Needs numpy only. Deterministic (fixed seed), so re-running gives the same files.
"""
import os
import sys
import wave

import numpy as np

SR = 48000
rng = np.random.default_rng(2026)

# Vowel formants (Hz, bandwidth Hz) for an adult voice; scaled up below for a
# small, cartoonish speaker.
VOWELS = {
    "a": [(850, 90), (1220, 110), (2810, 160)],
    "i": [(300, 60), (2300, 120), (3000, 160)],
    "u": [(350, 70), (900, 100), (2300, 150)],
    "o": [(500, 80), (850, 100), (2500, 150)],
    "e": [(500, 80), (1900, 120), (2600, 160)],
}
# Kirby as a baby: the highest-pitched take on him. Pitch is about double the
# earlier "little boy" set (normal ~820 Hz, panic ~1.1 kHz), formants are raised
# a lot (a tiny vocal tract), with a gentle wobble and babbly "ba / goo / pa"
# syllables. Durations are unchanged so takes still fit the event's timeline.
# (Earlier sets: boyish = normal f0 400 / FORMANT_SCALE 1.12; girlish = 600 / 1.3.)
FORMANT_SCALE = 1.5

# mood: base pitch, pitch glide over a syllable (semitones), vibrato (Hz, semitones),
#       brightness (extra harmonics tilt), drive (tanh grit), vowels to pick from,
#       syllable length range (s), consonants
# One entry per emotion in Madeline's set (= dialogue_portrait 1..10).
MOODS = {
    "normal":     dict(f0=820, glide=+1.0, vib=(5, 0.2), tilt=1.0, drive=0.0,
                       vowels="aouaa", dur=(0.085, 0.11), cons="bgpm"),
    "distracted": dict(f0=790, glide=-0.5, vib=(4, 0.3), tilt=0.95, drive=0.0,
                       vowels="ouaa", dur=(0.10, 0.13), cons="bmh"),
    "sad":        dict(f0=700, glide=-3.0, vib=(5, 0.3), tilt=0.85, drive=0.0,
                       vowels="uaua", dur=(0.12, 0.15), cons="bmh"),
    "deadpan":    dict(f0=740, glide=0.0, vib=(0, 0), tilt=0.9, drive=0.0,
                       vowels="aou", dur=(0.09, 0.12), cons="bmh"),
    "determined": dict(f0=840, glide=-1.0, vib=(0, 0), tilt=1.15, drive=0.15,
                       vowels="aoa", dur=(0.08, 0.10), cons="bgp"),
    "surprised":  dict(f0=1000, glide=+3.5, vib=(6, 0.25), tilt=1.05, drive=0.0,
                       vowels="aoi", dur=(0.08, 0.10), cons="hpb"),
    "upset":      dict(f0=800, glide=-2.0, vib=(5, 0.2), tilt=1.2, drive=0.25,
                       vowels="aea", dur=(0.08, 0.10), cons="bgm"),
    "angry":      dict(f0=780, glide=-3.0, vib=(0, 0), tilt=1.3, drive=0.5,
                       vowels="aoa", dur=(0.07, 0.09), cons="bgp"),
    "panic":      dict(f0=1100, glide=+1.5, vib=(10, 0.5), tilt=1.0, drive=0.0,
                       vowels="aiai", dur=(0.06, 0.08), cons="pbh"),
    "sadder":     dict(f0=660, glide=-4.0, vib=(5, 0.4), tilt=0.8, drive=0.0,
                       vowels="uau", dur=(0.14, 0.17), cons="bmh"),
}
# Used only for the "That *is* nice." phrase (dialogue_portrait 11).
HAPPY = dict(f0=920, glide=+2.5, vib=(6, 0.25), tilt=1.05, drive=0.0,
             vowels="aoa", dur=(0.08, 0.10), cons="bgp")


def resonance(freqs, fc, bw):
    """Magnitude of a 2-pole resonance at fc with bandwidth bw, evaluated at freqs."""
    return 1.0 / np.sqrt(1.0 + ((freqs - fc) / (bw / 2.0)) ** 2)


def voiced(dur, f0, glide, vib, tilt, formants):
    n = int(dur * SR)
    t = np.arange(n) / SR
    # Pitch contour: glide over the syllable, plus vibrato.
    semis = glide * (t / dur)
    if vib[0] > 0:
        semis = semis + vib[1] * np.sin(2 * np.pi * vib[0] * t)
    f = f0 * 2.0 ** (semis / 12.0)
    phase = 2 * np.pi * np.cumsum(f) / SR

    out = np.zeros(n)
    k = 1
    while True:
        fk = f * k
        if fk.max() > SR * 0.45:
            break
        # Source: sawtooth-ish 1/k^tilt rolloff, shaped by the vowel formants.
        amp = (1.0 / k ** (1.0 / tilt)) * sum(
            g * resonance(fk, fc * FORMANT_SCALE, bw * FORMANT_SCALE)
            for g, (fc, bw) in zip((1.0, 0.7, 0.35), formants))
        out += amp * np.sin(k * phase)
        k += 1
    return out


def consonant(kind, n_total):
    """Short onset in front of the vowel: a plosive puff, a glide, or breath."""
    n = int({"p": 0.012, "b": 0.010, "g": 0.012, "y": 0.0, "m": 0.0, "h": 0.018}[kind] * SR)
    burst = np.zeros(n_total)
    if n == 0:
        return burst
    noise = rng.standard_normal(n)
    # Crude high-pass for p/h (differentiate), low-ish for b/g.
    if kind in "ph":
        noise = np.diff(noise, prepend=0.0)
    env = np.linspace(1.0, 0.0, n) ** 2
    gain = {"p": 0.5, "b": 0.35, "g": 0.4, "h": 0.25}[kind]
    burst[:n] = noise * env * gain
    return burst


def envelope(n, attack=0.006, release=0.03):
    env = np.ones(n)
    a = max(1, int(attack * SR))
    r = max(1, min(int(release * SR), n // 2))
    env[:a] = np.linspace(0, 1, a)
    env[-r:] = np.linspace(1, 0, r) ** 1.5
    return env


def syllable(mood, kind):
    m = MOODS[mood] if isinstance(mood, str) else mood
    falling = m["glide"] < -2.0  # sad/sadder-like moods keep falling when emphasized
    vowel = rng.choice(list(m["vowels"]))
    cons = rng.choice(list(m["cons"]))
    dur = rng.uniform(*m["dur"])
    f0 = m["f0"] * 2.0 ** (rng.uniform(-1.5, 1.5) / 12.0)
    glide = m["glide"]
    gain = 1.0

    if kind == "emph":
        # Emphasized: longer, louder, pitched up with a rise-then-fall.
        dur *= 1.6
        f0 *= 2.0 ** (3 / 12.0)
        glide = glide if falling else abs(glide) + 2.0
        gain = 1.25
    elif kind == "end":
        # Line ending: longest, clear fall in pitch (a rise for upbeat moods).
        dur = max(dur * 2.4, 0.22)
        glide = +3.0 if m["glide"] > 1.5 else -5.0

    body = voiced(dur, f0, glide, m["vib"], m["tilt"], VOWELS[vowel])
    body *= envelope(len(body), release=0.06 if kind == "end" else 0.03)
    sig = body / (np.abs(body).max() + 1e-9) + consonant(cons, len(body))
    if m["drive"] > 0:
        sig = np.tanh(sig * (1 + 3 * m["drive"])) / np.tanh(1 + 3 * m["drive"])

    # 4 ms of silence at the head, short tail, normalise to -3 dBFS.
    sig = np.concatenate([np.zeros(int(0.004 * SR)), sig, np.zeros(int(0.01 * SR))])
    sig = sig / (np.abs(sig).max() + 1e-9) * (10 ** (-3 / 20)) * min(gain, 1.0)
    return sig, f"{cons}{vowel}"


def write_wav(path, sig):
    data = (np.clip(sig, -1, 1) * 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())


GROUPS = [("mid_A", "syl"), ("mid_B", "syl"), ("mid_C", "emph"), ("per", "end")]
TAKES_PER_GROUP = 10


def main():
    if len(sys.argv) < 2:
        sys.exit("usage: python generate_voice.py <output_dir>")
    out_dir = sys.argv[1]
    os.makedirs(out_dir, exist_ok=True)
    count = 0
    for mood in MOODS:
        for group, kind in GROUPS:
            for i in range(1, TAKES_PER_GROUP + 1):
                sig, _ = syllable(mood, kind)
                write_wav(os.path.join(out_dir, f"char_dia_kirby_{mood}_{group}_{i:02d}.wav"), sig)
                count += 1
        print(f"{mood}: {len(GROUPS) * TAKES_PER_GROUP} files")

    # "That *is* nice." -- three happy syllables, the last one a rising ending (~1 s).
    parts = [syllable(HAPPY, "syl")[0], syllable(HAPPY, "emph")[0], syllable(HAPPY, "end")[0]]
    gap = np.zeros(int(0.07 * SR))
    phrase = np.concatenate([parts[0], gap, parts[1], gap, parts[2], np.zeros(int(0.25 * SR))])
    write_wav(os.path.join(out_dir, "char_dia_kirby_thatisnice_solo.wav"), phrase)
    count += 1
    print(f"thatisnice_solo: {len(phrase) / SR:.2f}s")
    print(f"{count} files in {out_dir}")


if __name__ == "__main__":
    main()
