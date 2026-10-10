"""Áudio do jogo.

- Efeitos sonoros: gerados por código (sem arquivos).
- Músicas: arquivos MP3 em assets/audio, tocados como "camadas" em canais reservados.
  Cada camada tem um volume-alvo e o volume real desliza até ele (fade-in/fade-out suave).
  No duelo, as camadas "equilibrado" e "perigoso" tocam juntas e o volume de cada uma
  depende da tensão da luta (0 = equilibrado, 1 = perigoso), num crossfade de potência igual.
"""
import math
import os
import random
from array import array

import pygame

from src import settings as S

RATE = 44100
MUSIC_DIR = os.path.join(S.ASSETS_DIR, "audio")
MUSIC_FILES = {
    "menu": "musica_principal.mp3",      # lobby / menus, antes da partida
    "balanced": "dueloequilibrado.mp3",  # duelo equilibrado (início da luta)
    "danger": "duelo_perigoso.mp3",      # jogador perdendo / derrota
    "victory": "song_vitoria.mp3",       # jogador venceu
}
MUSIC_VOLUME = 0.7   # teto das músicas (multiplicado pelo volume escolhido nas configurações)
SCENE_FADE = 1.5     # segundos para trocar de música entre telas
TENSION_FADE = 3.5   # segundos para o crossfade equilibrado <-> perigoso (suave e tranquilo)


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


class _MusicLayer:
    """Uma música em loop num canal próprio, com volume que desliza até o alvo."""

    def __init__(self, sound, channel):
        self.sound = sound
        self.channel = channel
        self.vol = 0.0
        self.target = 0.0
        self.rate = MUSIC_VOLUME / SCENE_FADE  # volume por segundo

    def set_target(self, target, fade):
        self.target = max(0.0, min(1.0, target))
        self.rate = MUSIC_VOLUME / max(fade, 0.05)

    def update(self, dt, master=1.0):
        if self.target > 0 and not self.channel.get_busy():
            self.vol = 0.0
            self.channel.play(self.sound, loops=-1)
        step = self.rate * dt
        if self.vol < self.target:
            self.vol = min(self.target, self.vol + step)
        elif self.vol > self.target:
            self.vol = max(self.target, self.vol - step)
        if self.channel.get_busy():
            if self.vol <= 0.0 and self.target <= 0.0:
                self.channel.stop()  # terminou o fade-out: libera e recomeça do início na próxima vez
            else:
                self.channel.set_volume(self.vol * master)

    def stop(self):
        self.vol = 0.0
        self.channel.stop()


class AudioManager:
    def __init__(self):
        self.enabled = False
        self.music_volume = 0.7  # 0.0 (mudo) a 1.0
        self.sfx_volume = 0.8
        self._last_music = self.music_volume
        self._last_sfx = self.sfx_volume
        self.sounds = {}
        self.layers = {}
        self.mode = None
        self.tension = 0.0
        try:
            pygame.mixer.quit()
            pygame.mixer.init(frequency=RATE, size=-16, channels=2, buffer=1024, allowedchanges=0)
            pygame.mixer.set_num_channels(16)
            pygame.mixer.set_reserved(len(MUSIC_FILES))  # efeitos nunca roubam o canal da música
            self._build()
            self.enabled = True
        except Exception:  # sem dispositivo de áudio: o jogo segue mudo
            self.enabled = False

    # ---------- construção ----------
    def _sound(self, buf):
        stereo = array("h", [0]) * (len(buf) * 2)
        stereo[0::2] = buf
        stereo[1::2] = buf
        return pygame.mixer.Sound(buffer=stereo.tobytes())

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
        try:  # efeito de troca de personagem no lobby
            self.sounds["select"] = pygame.mixer.Sound(os.path.join(MUSIC_DIR, "select.ogg"))
        except Exception:
            self.sounds["select"] = self.sounds["click"]
        fallback = None
        for i, (name, filename) in enumerate(MUSIC_FILES.items()):
            try:
                sound = pygame.mixer.Sound(os.path.join(MUSIC_DIR, filename))
            except Exception:  # arquivo ausente/corrompido: usa a música gerada por código
                fallback = fallback or self._chiptune()
                sound = fallback
            self.layers[name] = _MusicLayer(sound, pygame.mixer.Channel(i))
        self.set_sfx_volume(self.sfx_volume)

    def _chiptune(self):
        bass = [110, 110, 98, 98, 87, 87, 98, 98]
        arp = [220, 262, 330, 262, 196, 247, 294, 247, 175, 220, 262, 220, 196, 247, 294, 392] * 2
        melody = _seq(arp, 0.2, 0.09, "sine", decay=False)
        low = _seq([f for f in bass for _ in range(4)], 0.2, 0.07, "square", decay=False)
        return self._sound(array("h", [max(-32767, min(32767, a + b)) for a, b in zip(melody, low)]))

    # ---------- efeitos ----------
    @property
    def sfx_on(self):
        return self.sfx_volume > 0

    def play(self, name):
        if self.enabled and self.sfx_on and name in self.sounds:
            self.sounds[name].play()

    def set_sfx_volume(self, value):
        self.sfx_volume = round(max(0.0, min(1.0, value)), 2)
        if self.sfx_volume > 0:
            self._last_sfx = self.sfx_volume
        for sound in self.sounds.values():
            sound.set_volume(self.sfx_volume)

    def set_sfx(self, on):
        self.set_sfx_volume(self._last_sfx if on else 0.0)

    # ---------- música ----------
    def play_music(self, name, fade=SCENE_FADE):
        """Faz crossfade para uma única música (menu, vitória, perigoso...)."""
        self.mode = name
        for key, layer in self.layers.items():
            layer.set_target(MUSIC_VOLUME if key == name else 0.0, fade)

    def start_battle(self):
        """Duelo começa equilibrado; a camada perigosa fica pronta, mas muda."""
        self.mode = "battle"
        self.tension = 0.0
        self._apply_tension(SCENE_FADE)

    def set_tension(self, value):
        """0 = luta equilibrada, 1 = jogador perdendo. Só vale durante o duelo."""
        value = max(0.0, min(1.0, value))
        if self.mode == "battle" and abs(value - self.tension) > 1e-3:
            self.tension = value
            self._apply_tension(TENSION_FADE)

    def _apply_tension(self, fade):
        if not self.layers:
            return
        ang = self.tension * math.pi / 2  # crossfade de potência igual: sem "buraco" de volume no meio
        targets = {"balanced": math.cos(ang) * MUSIC_VOLUME, "danger": math.sin(ang) * MUSIC_VOLUME}
        for key, layer in self.layers.items():
            layer.set_target(targets.get(key, 0.0), fade)

    def update(self, dt):
        if self.enabled and self.music_on:
            for layer in self.layers.values():
                layer.update(dt, self.music_volume)

    def start_music(self):
        self.play_music("menu")

    @property
    def music_on(self):
        return self.music_volume > 0

    def set_music_volume(self, value):
        """Volume geral da música; vale na hora, inclusive durante os fades. 0 = música desligada."""
        self.music_volume = round(max(0.0, min(1.0, value)), 2)
        if self.music_volume > 0:
            self._last_music = self.music_volume
        elif self.enabled:
            for layer in self.layers.values():
                layer.stop()

    def set_music(self, on):
        self.set_music_volume(self._last_music if on else 0.0)
