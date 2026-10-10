"""Renderiza os personagens 3D (peças GLB + animações retargetadas) em sprites RGBA para o Pygame."""
import os, json, math, sys
import numpy as np
from PIL import Image
from lib_render import *
from panda3d.core import (GraphicsPipe, FrameBufferProperties, PerspectiveLens, Point3, Point2, Vec3, NodePath, WindowProperties, GraphicsOutput)
from direct.actor.Actor import Actor

SCREEN_W, SCREEN_H = 1280, 720
SS = 2
BG = (255, 0, 255)
FOCAL = 850.0   # em "pixels" de filme
CAM_POS = Point3(1.15, -4.9, 1.5)
CAM_LOOK = Point3(0.55, 3.0, 1.05)
HERO_POS = Point3(0, 0, 0)
ENEMY_POS = Point3(2.7, 4.0, 0)
ANIM_DIR = "/tmp/w/anims"

FACES = {"usual": "Male_emotion_usual_001", "happy": "Male_emotion_happy_002", "angry": "Male_emotion_angry_003"}

# (peça, deslocamento de paleta u,v)  -- o deslocamento troca a cor pelo atlas de cores
SPECS = json.load(open("/tmp/w/specs.json"))

class Renderer:
    def __init__(self):
        self.base = make_base(64, 64, bg=(BG[0]/255, BG[1]/255, BG[2]/255, 1))
        b = self.base
        lights(b.render)
        fb = FrameBufferProperties(); fb.setRgbColor(True); fb.setRgbaBits(8, 8, 8, 8); fb.setDepthBits(24)
        wp = WindowProperties.size(256, 256)
        self.buf = b.graphicsEngine.makeOutput(b.pipe, "spr", -2, fb, wp,
            GraphicsPipe.BF_refuse_window | GraphicsPipe.BF_resizeable, b.win.getGsg(), b.win)
        self.buf.setClearColor((BG[0]/255, BG[1]/255, BG[2]/255, 1)); self.buf.setClearColorActive(True)
        self.buf.setClearDepthActive(True)
        b.win.setActive(False)
        self.lens = PerspectiveLens()
        b.cam.node().setLens(self.lens)
        dr = self.buf.makeDisplayRegion(); dr.setCamera(b.cam)
        b.camera.setPos(CAM_POS); b.camera.setHpr(0, -13, 0)
        self.cache = {}

    def set_window(self, x0, y0, w, h):
        """Renderiza só o retângulo (x0,y0,w,h) do quadro 1280x720, com supersampling."""
        L = self.lens
        L.setFilmSize(w, h); L.setFocalLength(FOCAL)
        cx, cy = x0 + w / 2.0, y0 + h / 2.0
        L.setFilmOffset(cx - SCREEN_W / 2.0, SCREEN_H / 2.0 - cy)
        L.setNearFar(0.1, 60)
        self.buf.setSize(int(w * SS), int(h * SS))
        self.win_size = (int(w * SS), int(h * SS))

    def project(self, np_, pt):
        """Projeta um ponto local (em np_) para pixels do quadro 1280x720 (câmera: +Y para frente)."""
        p = self.base.cam.getRelativePoint(np_, pt)
        if p.y <= 0.05:
            return None
        return (SCREEN_W / 2 + FOCAL * p.x / p.y, SCREEN_H / 2 - FOCAL * p.z / p.y)

    def grab(self):
        b = self.base
        b.graphicsEngine.renderFrame(); b.graphicsEngine.renderFrame()
        tex = self.buf.getScreenshot()
        w, h = tex.getXSize(), tex.getYSize()
        arr = np.frombuffer(tex.getRamImageAs("RGBA"), np.uint8).reshape(h, w, 4)[::-1]
        return arr.copy()

    def build(self, key):
        spec = SPECS[key]
        root = self.base.render.attachNewNode(key)
        anims = {n: f"{ANIM_DIR}/{n}.glb" for n in spec["clips"].values()}
        parts = {}
        def mk(name, off):
            a = Actor(f"{GLB}/{name}.glb", anims)
            a.reparentTo(root)
            for t in a.findAllTextures():
                t.setFormat(Texture.F_rgb); t.setMinfilter(Texture.FT_linear); t.setMagfilter(Texture.FT_linear)
            if off:
                a.setTexOffset(TextureStage.getDefault(), off[0], off[1])
            return a
        parts["body"] = [mk("Body_010", spec.get("skin_off"))]
        for name, off in spec["parts"]:
            parts["body"].append(mk(name, off))
        faces = {k: mk(v, None) for k, v in FACES.items()}
        return root, parts["body"], faces

    def place(self, root, who):
        pos = HERO_POS if who == "hero" else ENEMY_POS
        root.setPos(pos)
        pivot = self.base.render.attachNewNode("pv"); pivot.setPos(pos)
        target = ENEMY_POS if who == "hero" else HERO_POS
        pivot.lookAt(Point3(target.x, target.y, 0))
        h = pivot.getH() + 180          # o modelo olha para -Y por padrão
        pivot.removeNode()
        root.setH(h)

    def window_for(self, root, margin=40):
        """Retângulo de tela que cobre o personagem em qualquer pose (caixa generosa)."""
        pts = []
        for dx in (-1.1, 1.1):
            for dz in (-0.05, 2.4):
                for dy in (-0.9, 0.9):
                    p = self.project(root, Point3(dx, dy, dz))
                    if p: pts.append(p)
        xs, ys = zip(*pts)
        x0, x1, y0, y1 = min(xs) - margin, max(xs) + margin, min(ys) - margin, max(ys) + margin
        x0, y0 = max(0, int(x0)), max(0, int(y0))
        return x0, y0, int(min(SCREEN_W, x1) - x0), int(min(SCREEN_H, y1) - y0)

    def render_frame(self, parts, faces, face, clip, frame):
        for a in parts:
            a.pose(clip, frame)
        for k, f in faces.items():
            if k == face: f.show(); f.pose(clip, frame)
            else: f.hide()
        return self.grab()

def to_sprite(arr):
    """RGBA bruto (com fundo magenta) -> RGBA reduzido por SS com alfa suave."""
    h, w = arr.shape[:2]
    rgb = arr[..., :3].astype(np.float32)
    mask = ~((arr[..., 0] == BG[0]) & (arr[..., 1] == BG[1]) & (arr[..., 2] == BG[2]))
    a = mask.astype(np.float32)
    pm = rgb * a[..., None]
    H, W = h // SS, w // SS
    a_s = a[:H*SS, :W*SS].reshape(H, SS, W, SS).mean((1, 3))
    c_s = pm[:H*SS, :W*SS].reshape(H, SS, W, SS, 3).mean((1, 3))
    col = np.where(a_s[..., None] > 0, c_s / np.maximum(a_s[..., None], 1e-6), 0)
    out = np.dstack([np.clip(col, 0, 255), a_s * 255]).astype(np.uint8)
    return out
