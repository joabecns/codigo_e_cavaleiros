import os
from panda3d.core import (loadPrcFileData, AmbientLight, DirectionalLight, Texture, PNMImage,
                          TextureStage, Filename)
loadPrcFileData("", "window-type offscreen\nload-display p3tinydisplay\naudio-library-name null\nframebuffer-alpha 1\nnotify-level error\ndefault-directnotify-level error\n")
from direct.showbase.ShowBase import ShowBase

GLB = "/tmp/w/Separate_assets_glb"
ATLAS = None

def make_base(w, h, bg=(0.4,0.65,0.95,1)):
    loadPrcFileData("", f"win-size {w} {h}")
    base = ShowBase()
    base.setBackgroundColor(*bg)
    base.disableMouse()
    return base

def atlas_texture():
    global ATLAS
    if ATLAS is None:
        img = PNMImage()
        img.read(Filename(f"{GLB}/Textures_4.png"))
        tex = Texture("atlas")
        tex.load(img)
        tex.setFormat(Texture.F_rgb)
        tex.setMinfilter(Texture.FT_linear); tex.setMagfilter(Texture.FT_linear)
        ATLAS = tex
    return ATLAS

def lights(render):
    al = AmbientLight("a"); al.setColor((0.62,0.62,0.62,1)); render.setLight(render.attachNewNode(al))
    dl = DirectionalLight("d"); dl.setColor((0.75,0.75,0.7,1))
    n = render.attachNewNode(dl); n.setHpr(-35,-35,0); render.setLight(n)

def load_part(base, name, parent, offset=None):
    m = base.loader.loadModel(f"{GLB}/{name}.glb")
    m.reparentTo(parent)
    for t in m.findAllTextures():
        t.setFormat(Texture.F_rgb)
        t.setMinfilter(Texture.FT_linear); t.setMagfilter(Texture.FT_linear)
    if offset:
        m.setTexOffset(TextureStage.getDefault(), *offset)
    return m
