"""Monta todos os clipes usados no jogo (retargeting do KayKit + animações criadas à mão)."""
import numpy as np
from rigtools import *

def ease(x):
    x = np.clip(x, 0, 1)
    return x * x * (3 - 2 * x)

def nlerp(a, b, s):
    v = np.asarray(a, float) * (1 - s) + np.asarray(b, float) * s
    return v / np.linalg.norm(v)

def rx(deg):
    return R.from_euler("x", deg, degrees=True)

def ground_fix(tgt, want, hips_world, names, level):
    """Ajusta a altura do quadril para que 'names' fique na altura 'level' (média)."""
    ti = tgt.idx
    for _ in range(3):
        cache = {}
        hips_local = hips_world - tgt.world_of_parent(ti["Hips"], {})[1]
        ys = []
        for i in tgt.joints:
            pr, pp = tgt.world_of_parent(i, cache)
            nm = tgt.g.nodes[i].name
            lr = pr.inv() * want[nm] if nm in want else R.from_quat(tgt.rest_q[i])
            t = hips_local if nm == "Hips" else tgt.rest_t[i]
            cache[i] = (pr * lr, pp + pr.apply(t))
            if nm in names:
                ys.append(cache[i][1][1])
        hips_world = hips_world.copy(); hips_world[1] += level - float(np.mean(ys))
    return hips_world

def lament_clip(tgt, intro=27, loop=30):
    """Personagem derrotado: cai de joelhos, cabeça baixa, mãos no rosto e soluça. Retorna (frames, loop_start)."""
    ti = tgt.idx
    rw, rp = tgt.rest_wrot, tgt.rest_wpos
    def restdir(a, b): return rp[ti[b]] - rp[ti[a]]
    poses = []
    for f in range(intro + loop):
        s = float(ease(f / intro))
        ph = 2 * np.pi * (f - intro) / (loop / 2.0) if f >= intro else 0.0
        sob = np.sin(ph) if f >= intro else 0.0
        sob_amp = 1.0 if f >= intro else 0.0
        want = {}
        # tronco curvado para frente + tremor do choro
        cum = {"Hips": 8, "Spine": 18, "Spine1": 27, "Head": 27 + 32}
        tremor = {"Hips": 0, "Spine": 0.8, "Spine1": 2.2, "Head": 3.0}
        for n, a in cum.items():
            want[n] = rx(a * s + tremor[n] * sob * sob_amp) * rw[ti[n]]
        for side, sx in (("Left", 1.0), ("Right", -1.0)):
            up_kneel = np.array([0.03 * sx, -0.99, 0.12])
            sh_kneel = np.array([0.05 * sx, -0.03, -1.0])
            ft_kneel = np.array([0.0, -0.12, -1.0])
            arm_k = np.array([0.15 * sx, -0.70, 0.60])
            fore_k = np.array([-0.45 * sx, 0.85 + 0.10 * sob * sob_amp, 0.25])
            hand_k = np.array([-0.5 * sx, 0.8, 0.3])
            spec = {f"{side}UpLeg": (restdir(f"{side}UpLeg", f"{side}Leg"), up_kneel),
                    f"{side}Leg": (restdir(f"{side}Leg", f"{side}Foot"), sh_kneel),
                    f"{side}Foot": (restdir(f"{side}Foot", f"{side}ToeBase"), ft_kneel),
                    f"{side}Arm": (restdir(f"{side}Arm", f"{side}ForeArm"), arm_k),
                    f"{side}ForeArm": (restdir(f"{side}ForeArm", f"{side}Hand"), fore_k),
                    f"{side}Hand": (restdir(f"{side}Hand", f"{side}HandMiddle1"), hand_k)}
            for n, (v0, v1) in spec.items():
                want[n] = rot_between(v0, nlerp(v0, v1, s)) * rw[ti[n]]
        hips_world = rp[ti["Hips"]].copy()
        hips_world[2] += -0.05 * s
        hips_world = ground_fix(tgt, want, hips_world, ("LeftLeg", "RightLeg"), 0.075 * s + (1 - s) * rp[ti["LeftLeg"]][1])
        poses.append((want, hips_world - rp[ti["Hips"]]))
    return finish(tgt, poses, ground=False), intro

def rage_clip(tgt, n=36):
    """Vilão com raiva: pisa forte, punhos levantados tremendo e corpo inclinado para frente."""
    ti = tgt.idx; rw, rp = tgt.rest_wrot, tgt.rest_wpos
    def restdir(a, b): return rp[ti[b]] - rp[ti[a]]
    poses = []
    for f in range(n):
        s = float(ease(f / 6.0)); t = f / FPS
        sh = np.sin(2 * np.pi * 5.0 * t)          # tremor rápido
        stomp = abs(np.sin(2 * np.pi * 2.0 * t))  # pisadas
        want = {}
        cum = {"Hips": 4, "Spine": 10, "Spine1": 16, "Head": 22}
        for k, a in cum.items():
            want[k] = rx(a * s + 1.5 * sh * s) * rw[ti[k]]
        for side, sx in (("Left", 1.0), ("Right", -1.0)):
            arm = np.array([0.55 * sx, 0.30, 0.45])
            fore = np.array([0.05 * sx, 1.0, 0.25 + 0.15 * sh])
            hand = np.array([0.0, 1.0, 0.2])
            spec = {f"{side}Arm": (restdir(f"{side}Arm", f"{side}ForeArm"), arm),
                    f"{side}ForeArm": (restdir(f"{side}ForeArm", f"{side}Hand"), fore),
                    f"{side}Hand": (restdir(f"{side}Hand", f"{side}HandMiddle1"), hand)}
            for k, (v0, v1) in spec.items():
                want[k] = rot_between(v0, nlerp(v0, v1, s)) * rw[ti[k]]
        hw = rp[ti["Hips"]].copy(); hw[1] -= 0.03 * s * (1 - stomp)
        poses.append((want, hw - rp[ti["Hips"]]))
    return finish(tgt, poses, ground=True)

def cheer_hop(gen_sim, tgt, n=54):
    """Comemoração: braços para cima (KayKit 'Cheering') + pulos de alegria."""
    fr = sample_clip(gen_sim, "Cheering")
    fr = [fr[min(i, len(fr) - 1)] for i in range(n)]
    hop = [0.30 * max(0.0, np.sin(2 * np.pi * i / 18.0)) for i in range(n)]
    return retarget(gen_sim, tgt, fr, airborne=hop)

def build_all():
    tgt = Rig(BODY)
    gen = Rig(f"{KK}/Rig_Medium_General.glb")
    sim = Rig(f"{KK}/Rig_Medium_Simulation.glb")
    mov = Rig(f"{KK}/Rig_Medium_MovementBasic.glb")
    clips = {}
    clips["idle"] = retarget(gen, tgt, sample_clip(gen, "Idle_A"))
    clips["idle_b"] = retarget(gen, tgt, sample_clip(gen, "Idle_B"))
    clips["hit_a"] = retarget(gen, tgt, sample_clip(gen, "Hit_A"))
    clips["hit_b"] = retarget(gen, tgt, sample_clip(gen, "Hit_B"))
    clips["cheer_src"] = retarget(sim, tgt, sample_clip(sim, "Cheering"))
    clips["jump_src"] = retarget(mov, tgt, sample_clip(mov, "Jump_Full_Short"))
    clips["cheer"] = cheer_hop(sim, tgt)
    clips["rage"] = rage_clip(tgt)
    clips["lament"], loop_start = lament_clip(tgt)
    return clips, loop_start

if __name__ == "__main__":
    clips, ls = build_all()
    for k, v in clips.items():
        print(k, len(v), "frames")
    write_anim_glb(BODY, "/tmp/w/anim_all.glb", clips)
    print("lament loop_start", ls)
