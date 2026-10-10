import sys, os, json, time
sys.path.insert(0, "/tmp/w")
from sprites import *
import sprites
from recolor import recolor

OUT = sys.argv[1] if len(sys.argv) > 1 else "/tmp/w/out"
ONLY = sys.argv[2].split(",") if len(sys.argv) > 2 else None
STEP = 2   # 30 fps -> 15 fps

CLIPS_HERO = {"idle": ("idle", "happy"), "pain": ("hit_b", "angry"), "cheer": ("cheer", "happy"), "lament": ("lament", "usual")}
CLIPS_ENEMY = {"idle": ("idle_b", "angry"), "pain": ("hit_b", "angry"), "rage": ("rage", "angry"),
               "cheer": ("cheer", "happy"), "lament": ("lament", "usual")}
NFRAMES = {"idle": 33, "idle_b": 65, "hit_b": 27, "cheer": 54, "rage": 36, "lament": 57}

def build(r, key, spec, clipmap):
    base = r.base
    root = base.render.attachNewNode(key)
    anims = {n: f"{ANIM_DIR}/{n}.glb" for n, _ in clipmap.values()}
    def mk(path, tint=None):
        a = Actor(path, anims)
        a.reparentTo(root)
        for t in a.findAllTextures():
            t.setFormat(Texture.F_rgb); t.setMinfilter(Texture.FT_linear); t.setMagfilter(Texture.FT_linear)
        return a
    parts = [mk(recolor("Body_010", tuple(spec["skin"])))]
    for name, rgb in spec["parts"]:
        parts.append(mk(recolor(name, tuple(rgb)) if rgb else f"{GLB}/{name}.glb"))
    faces = {k: mk(f"{GLB}/{v}.glb") for k, v in FACES.items()}
    root.setScale(spec["scale"])
    return root, parts, faces

def save_frames(r, root, parts, faces, clipmap, outdir, window, front=False):
    x0, y0, w, h = window
    r.set_window(x0, y0, w, h)
    manifest = {}
    for cname, (anim, face) in clipmap.items():
        n = NFRAMES[anim]
        frames = list(range(0, n, STEP))
        if frames[-1] != n - 1:
            frames.append(n - 1)
        entries = []
        for i, f in enumerate(frames):
            spr = to_sprite(r.render_frame(parts, faces, face, anim, f))
            im = Image.fromarray(spr, "RGBA")
            bb = im.getbbox()
            if bb is None:
                entries.append(None); continue
            im = im.crop(bb)
            fn = f"{cname}_{i:02d}.png"
            os.makedirs(outdir, exist_ok=True)
            im.save(f"{outdir}/{fn}", optimize=True)
            entries.append({"file": fn, "x": x0 + bb[0], "y": y0 + bb[1]})
        manifest[cname] = entries
    return manifest

def main():
    r = Renderer()
    allman = {}
    for key, spec in SPECS.items():
        if ONLY and key not in ONLY:
            continue
        t0 = time.time()
        clipmap = CLIPS_HERO if spec["kind"] == "hero" else CLIPS_ENEMY
        root, parts, faces = build(r, key, spec, clipmap)
        who = "hero" if spec["kind"] == "hero" else "enemy"
        r.place(root, who)
        feet = r.project(root, Point3(0, 0, 0))
        win = r.window_for(root, margin=30)
        m = save_frames(r, root, parts, faces, clipmap, f"{OUT}/{key}", win)
        entry = {"name": spec["name"], "kind": spec["kind"], "feet": feet, "clips": m}
        if spec["kind"] == "hero":       # retrato frontal para a tela de escolha
            root.setH(0)
            old = (sprites.CAM_POS, r.base.camera.getPos(), r.base.camera.getHpr())
            r.base.camera.setPos(0, -7.2, 1.05); r.base.camera.setHpr(0, 0, 0)
            r.lens.setFocalLength(1); 
            sprites_focal = sprites.FOCAL
            sprites.FOCAL = 1200.0
            pw = r.window_for(root, margin=20)
            mf = save_frames(r, root, parts, faces, {"front": ("idle", "happy"), "front_cheer": ("cheer", "happy")}, f"{OUT}/{key}", pw)
            entry["front"] = mf
            sprites.FOCAL = sprites_focal
            r.base.camera.setPos(sprites.CAM_POS); r.base.camera.setHpr(0, -13, 0)
        allman[key] = entry
        root.removeNode()
        print(key, "ok", round(time.time() - t0, 1), "s", flush=True)
        json.dump(allman, open(f"{OUT}/manifest_{'_'.join(ONLY) if ONLY else 'all'}.json", "w"))

main()
