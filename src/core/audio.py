"""Áudio gerado por código (sem arquivos e sem copyright)."""
import math
import random
from array import array

import pygame

RATE = 22050


def _wave(freq, dur, vol=0.3, decay=True, kind="square"):
    n = int(RATE * dur)
    buf = array("h")
    for i in range(n):
        ph = (i * freq / RATE) % 1.0
        s = (1.0 if ph < 0.5 else -1.0) if kind == "square" else math.sin(2 * math.pi * ph)
        env = 1.0 - i / n if decay else 1.0
        buf.append(int(s * vol * env * 32767))
    return buf


def _noise(dur, vol=0.3):
    n = int(RATE * dur)
    return array("h", [int(random.uniform(-1, 1) * vol * (1 - i / n) * 32767) for i in range(n)])


def _seq(freqs, dur, vol=0.3, kind="square", decay=True):
    out = array("h")
    for f in freqs:
        out.extend(_wave(f, dur, vol, decay, kind) if f else array("h", [0] * int(RATE * dur)))
    return out


class AudioManager:
    def __init__(self):
        self.enabled = False
        self.music_on = True
        self.sfx_on = True
        self.sounds = {}
        self.music = None
        try:
            pygame.mixer.quit()
            pygame.mixer.init(frequency=RATE, size=-16, channels=1, buffer=512, allowedchanges=0)
            self._build()
            self.enabled = True
        except Exception:  # sem dispositivo de áudio: o jogo segue mudo
            self.enabled = False

    def _sound(self, buf):
        return pygame.mixer.Sound(buffer=buf.tobytes())

    def _build(self):
        self.sounds = {
            "click": self._sound(_wave(900, 0.05, 0.25)),
            "correct": self._sound(_seq([660, 880], 0.09, 0.3)),
            "wrong": self._sound(_seq([180, 130], 0.14, 0.35)),
            "hit": self._sound(_noise(0.14, 0.4)),
            "win": self._sound(_seq([523, 659, 784, 1047], 0.13, 0.3)),
            "lose": self._sound(_seq([392, 330, 262, 196], 0.2, 0.3)),
            "skill": self._sound(_seq([700, 900, 1200], 0.06, 0.25)),
        }
        bass = [110, 110, 98, 98, 87, 87, 98, 98]
        arp = [220, 262, 330, 262, 196, 247, 294, 247, 175, 220, 262, 220, 196, 247, 294, 392] * 2
        melody = _seq(arp, 0.2, 0.09, "sine", decay=False)
        low = _seq([f for f in bass for _ in range(4)], 0.2, 0.07, "square", decay=False)
        mix = array("h", [max(-32767, min(32767, a + b)) for a, b in zip(melody, low)])
        self.music = self._sound(mix)
        self.music.set_volume(0.35)

    def play(self, name):
        if self.enabled and self.sfx_on and name in self.sounds:
            self.sounds[name].play()

    def start_music(self):
        if self.enabled and self.music_on and self.music:
            self.music.play(loops=-1)

    def set_music(self, on):
        self.music_on = on
        if self.enabled and self.music:
            self.music.play(loops=-1) if on else self.music.stop()

    def set_sfx(self, on):
        self.sfx_on = on
