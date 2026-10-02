"""Ferramentas de rig: FK, retargeting do KayKit para o esqueleto dos personagens e escrita de GLB animado."""
import numpy as np
from pygltflib import (GLTF2, Animation, AnimationSampler, AnimationChannel, AnimationChannelTarget,
                       Accessor, BufferView, FLOAT)
from scipy.spatial.transform import Rotation as R, Slerp

KK = "/tmp/w/KayKit_Character_Animations_1.1/Animations/gltf/Rig_Medium"
BODY = "/tmp/w/Separate_assets_glb/Body_010.glb"
FPS = 30

# ---------- leitura de gltf ----------
def accessor_data(g, idx):
    acc = g.accessors[idx]
    bv = g.bufferViews[acc.bufferView]
    blob = g.binary_blob()
    comps = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4, "MAT4": 16}[acc.type]
    dt = {5126: np.float32, 5123: np.uint16, 5125: np.uint32}[acc.componentType]
    off = (bv.byteOffset or 0) + (acc.byteOffset or 0)
    n = acc.count * comps
    arr = np.frombuffer(blob, dtype=dt, count=n, offset=off).astype(np.float64)
    return arr.reshape(acc.count, comps) if comps > 1 else arr

class Rig:
    def __init__(self, path, joint_names=None):
        self.g = GLTF2().load(path)
        g = self.g
        self.idx = {n.name: i for i, n in enumerate(g.nodes)}
        self.parent = {}
        for i, n in enumerate(g.nodes):
            for c in (n.children or []):
                self.parent[c] = i
        skin_joints = set(g.skins[0].joints) if g.skins else set()
        # esqueleto = nós que são juntas (ou descendentes de juntas)
        self.joints = [i for i in self._order() if i in skin_joints or self._is_bone(i, skin_joints)]
        self.names = [g.nodes[i].name for i in self.joints]
        self.jset = set(self.joints)
        self.rest_t = {i: np.array(g.nodes[i].translation or [0, 0, 0], float) for i in self.joints}
        self.rest_q = {i: np.array(g.nodes[i].rotation or [0, 0, 0, 1], float) for i in self.joints}
        self.rest_wrot, self.rest_wpos = self.fk({}, None)

    def _is_bone(self, i, sj):
        p = self.parent.get(i)
        while p is not None:
            if p in sj:
                return True
            p = self.parent.get(p)
        return False

    def _order(self):
        roots = [i for i in range(len(self.g.nodes)) if i not in self.parent]
        out = []
        def walk(i):
            out.append(i)
            for c in (self.g.nodes[i].children or []):
                walk(c)
        for r in roots:
            walk(r)
        return out

    def world_of_parent(self, i, cache):
        p = self.parent.get(i)
        if p is None:
            return R.identity(), np.zeros(3)
        if p in cache:
            return cache[p]
        return R.identity(), np.zeros(3)

    def fk(self, local_q, hips_pos, hips_name=None):
        """local_q: {idx: quat xyzw}; hips_pos: nova posição local do quadril (ou None). Retorna dicts world rot/pos."""
        cache, wrot, wpos = {}, {}, {}
        for i in self.joints:
            pr, pp = self.world_of_parent(i, cache)
            q = local_q.get(i, self.rest_q[i])
            t = self.rest_t[i]
            if hips_pos is not None and self.g.nodes[i].name.lower() == "hips":
                t = hips_pos
            lr = R.from_quat(q)
            wr = pr * lr
            wp = pp + pr.apply(t)
            cache[i] = (wr, wp)
            wrot[i], wpos[i] = wr, wp
        return wrot, wpos

def sample_clip(rig, anim_name):
    """Amostra um clipe do KayKit a 30 fps. Retorna lista de (local_q dict, hips_pos)."""
    g = rig.g
    a = [x for x in g.animations if x.name == anim_name][0]
    tracks = {}
    tmax = 0.0
    for ch in a.channels:
        s = a.samplers[ch.sampler]
        times = accessor_data(g, s.input)
        vals = accessor_data(g, s.output)
        tracks[(ch.target.node, ch.target.path)] = (times, vals)
        tmax = max(tmax, times[-1])
    n = max(2, int(round(tmax * FPS)) + 1)
    frames = []
    for f in range(n):
        t = min(f / FPS, tmax)
        lq, hp = {}, None
        for (node, path), (times, vals) in tracks.items():
            if node not in rig.jset:
                continue
            if path == "rotation":
                lq[node] = Slerp(times, R.from_quat(vals))(np.clip(t, times[0], times[-1])).as_quat() if len(times) > 1 else vals[0]
            elif path == "translation" and rig.g.nodes[node].name.lower() == "hips":
                hp = np.array([np.interp(t, times, vals[:, k]) for k in range(3)])
        frames.append((lq, hp))
    return frames

# ---------- retarget ----------
def rot_between(a, b):
    a = a / np.linalg.norm(a); b = b / np.linalg.norm(b)
    c = np.cross(a, b); d = float(np.dot(a, b))
    if np.linalg.norm(c) < 1e-8:
        if d > 0:
            return R.identity()
        ax = np.cross(a, [1, 0, 0]); 
        if np.linalg.norm(ax) < 1e-6: ax = np.cross(a, [0, 0, 1])
        return R.from_rotvec(ax / np.linalg.norm(ax) * np.pi)
    ang = np.arctan2(np.linalg.norm(c), d)
    return R.from_rotvec(c / np.linalg.norm(c) * ang)

TORSO = {"Hips": "hips", "Spine": "spine", "Spine1": "chest", "Head": "head"}
def _limbs():
    m = {}
    for S, s in (("Left", "l"), ("Right", "r")):
        m[f"{S}Arm"] = (f"upperarm.{s}", f"lowerarm.{s}", f"{S}ForeArm")
        m[f"{S}ForeArm"] = (f"lowerarm.{s}", f"wrist.{s}", f"{S}Hand")
        m[f"{S}Hand"] = (f"wrist.{s}", f"hand.{s}", f"{S}HandMiddle1")
        m[f"{S}UpLeg"] = (f"upperleg.{s}", f"lowerleg.{s}", f"{S}Leg")
        m[f"{S}Leg"] = (f"lowerleg.{s}", f"foot.{s}", f"{S}Foot")
        m[f"{S}Foot"] = (f"foot.{s}", f"toes.{s}", f"{S}ToeBase")
    return m
LIMBS = _limbs()

def retarget(src, tgt, frames, ground=True, airborne=None):
    """Converte frames do rig KayKit para o rig do personagem. Retorna lista (quats {nome:xyzw}, hips_pos[3])."""
    si, ti = src.idx, tgt.idx
    hips_t, hips_s = ti["Hips"], si["hips"]
    scale = tgt.rest_wpos[hips_t][1] / src.rest_wpos[hips_s][1]
    out = []
    lowest_rest = min(tgt.rest_wpos[ti[n]][1] for n in ("LeftFoot", "RightFoot", "LeftToeBase", "RightToeBase"))
    for lq, hp in frames:
        swr, swp = src.fk(lq, hp)
        want = {}   # nome alvo -> rotação mundial desejada
        for tn, sn in TORSO.items():
            d = swr[si[sn]] * src.rest_wrot[si[sn]].inv()
            want[tn] = d * tgt.rest_wrot[ti[tn]]
        for tn, (a, b, child) in LIMBS.items():
            vs = swp[si[b]] - swp[si[a]]
            vt = tgt.rest_wpos[ti[child]] - tgt.rest_wpos[ti[tn]]
            want[tn] = rot_between(vt, vs) * tgt.rest_wrot[ti[tn]]
        hp_w = (swp[hips_s] - src.rest_wpos[hips_s]) * scale
        out.append((want, hp_w))
    return finish(tgt, out, ground, airborne)

def finish(tgt, poses, ground=True, airborne=None):
    """poses: lista (want_world_rot por nome, delta_pos_mundial_do_quadril). Resolve rotações locais e ajusta o chão."""
    ti = tgt.idx
    res = []
    for k, (want, dpos) in enumerate(poses):
        hips_world = tgt.rest_wpos[ti["Hips"]] + dpos
        for _ in range(2 if ground else 1):
            lq, wrot, wpos = {}, {}, {}
            hips_local = hips_world - tgt.world_of_parent(ti["Hips"], {})[1]
            cache = {}
            for i in tgt.joints:
                pr, pp = tgt.world_of_parent(i, cache)
                name = tgt.g.nodes[i].name
                if name in want:
                    lr = pr.inv() * want[name]
                else:
                    lr = R.from_quat(tgt.rest_q[i])
                t = hips_local if name == "Hips" else tgt.rest_t[i]
                wr = pr * lr
                wp = pp + pr.apply(t)
                cache[i] = (wr, wp)
                lq[name] = lr.as_quat(); wpos[name] = wp
            if ground:
                low = min(wpos[n][1] for n in ("LeftFoot", "RightFoot", "LeftToeBase", "RightToeBase"))
                rest_low = min(tgt.rest_wpos[ti[n]][1] for n in ("LeftFoot", "RightFoot", "LeftToeBase", "RightToeBase"))
                hips_world = hips_world.copy(); hips_world[1] += (rest_low - low)
        extra = 0.0 if airborne is None else airborne[k]
        hips_world = hips_world.copy(); hips_world[1] += extra
        hips_local = hips_world - tgt.world_of_parent(ti["Hips"], {})[1]
        res.append((lq, hips_local))
    return res

# ---------- escrita de GLB animado ----------
def write_anim_glb(tgt_path, out_path, clips):
    """clips: {nome: [(quats{nome:xyzw}, hips_local)]} -> GLB com o esqueleto do personagem + animações."""
    g = GLTF2().load(tgt_path)
    blob = bytearray(g.binary_blob())
    idx = {n.name: i for i, n in enumerate(g.nodes)}
    def add(arr, typ, count):
        nonlocal blob
        while len(blob) % 4: blob.append(0)
        off = len(blob)
        data = np.asarray(arr, np.float32).tobytes()
        blob += data
        g.bufferViews.append(BufferView(buffer=0, byteOffset=off, byteLength=len(data)))
        acc = Accessor(bufferView=len(g.bufferViews) - 1, componentType=FLOAT, count=count, type=typ)
        if typ == "SCALAR":
            acc.min = [float(np.min(arr))]; acc.max = [float(np.max(arr))]
        g.accessors.append(acc)
        return len(g.accessors) - 1
    g.animations = []
    for cname, frames in clips.items():
        n = len(frames)
        times = np.arange(n, dtype=np.float32) / FPS
        tin = add(times, "SCALAR", n)
        anim = Animation(name=cname, samplers=[], channels=[])
        for name in frames[0][0]:
            q = np.array([f[0][name] for f in frames], np.float32)
            out = add(q, "VEC4", n)
            anim.samplers.append(AnimationSampler(input=tin, output=out, interpolation="LINEAR"))
            anim.channels.append(AnimationChannel(sampler=len(anim.samplers) - 1,
                                 target=AnimationChannelTarget(node=idx[name], path="rotation")))
        tr = np.array([f[1] for f in frames], np.float32)
        out = add(tr, "VEC3", n)
        anim.samplers.append(AnimationSampler(input=tin, output=out, interpolation="LINEAR"))
        anim.channels.append(AnimationChannel(sampler=len(anim.samplers) - 1,
                             target=AnimationChannelTarget(node=idx["Hips"], path="translation")))
        g.animations.append(anim)
    g.buffers[0].byteLength = len(blob)
    g.set_binary_blob(bytes(blob))
    g.save_binary(out_path)
