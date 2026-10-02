"""Recolore uma peça GLB escolhendo no atlas o pixel mais próximo da cor desejada (cor chapada)."""
import os
import numpy as np
from PIL import Image
from pygltflib import GLTF2

GLB = "/tmp/w/Separate_assets_glb"
OUT = "/tmp/w/recolor"
_atlas = None

def nearest_uv(rgb):
    global _atlas
    if _atlas is None:
        _atlas = np.asarray(Image.open(f"{GLB}/Textures_4.png").convert("RGB").resize((256, 256), Image.BOX)).astype(int)
    d = ((_atlas - np.array(rgb)) ** 2).sum(-1)
    y, x = np.unravel_index(np.argmin(d), d.shape)
    return (x + 0.5) / 256.0, (y + 0.5) / 256.0

def recolor(part, rgb):
    os.makedirs(OUT, exist_ok=True)
    out = f"{OUT}/{part}__{rgb[0]}_{rgb[1]}_{rgb[2]}.glb"
    if os.path.exists(out):
        return out
    g = GLTF2().load(f"{GLB}/{part}.glb")
    blob = bytearray(g.binary_blob())
    u, v = nearest_uv(rgb)
    for m in g.meshes:
        for p in m.primitives:
            acc = g.accessors[p.attributes.TEXCOORD_0]
            bv = g.bufferViews[acc.bufferView]
            stride = bv.byteStride or 8
            base = (bv.byteOffset or 0) + (acc.byteOffset or 0)
            for i in range(acc.count):
                o = base + i * stride
                blob[o:o + 8] = np.array([u, v], np.float32).tobytes()
    g.set_binary_blob(bytes(blob))
    g.save_binary(out)
    return out
