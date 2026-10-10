using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CodigoECavaleiros.EditorTools
{
    /// <summary>
    /// Sintetiza os sons e as músicas do jogo (ondas quadrada/triangular/serra/ruído) e salva como WAV
    /// em Assets/Resources/Audio. Tudo é original, então não há problema de licença.
    /// </summary>
    public static class AudioBuilder
    {
        const int Rate = 32000;
        const string OutDir = "Assets/Resources/Audio/";
        enum Wave { Sine, Square, Tri, Saw, Noise }

        static readonly System.Random Rng = new System.Random(12345);

        // ------------------------------------------------------------------ síntese
        static float Osc(Wave w, double phase)
        {
            double p = phase - Math.Floor(phase);
            switch (w)
            {
                case Wave.Sine: return (float)Math.Sin(p * 2 * Math.PI);
                case Wave.Square: return p < 0.5 ? 1f : -1f;
                case Wave.Tri: return (float)(4 * Math.Abs(p - 0.5) - 1);
                case Wave.Saw: return (float)(2 * p - 1);
                default: return (float)(Rng.NextDouble() * 2 - 1);
            }
        }

        /// <summary>Soma uma nota no buffer. f1 = f0 faz tom fixo; f1 diferente faz glissando.</summary>
        static void Note(float[] buf, double start, double dur, double f0, double f1, Wave w, float amp, double attack = 0.005, double release = 0.04)
        {
            int s = (int)(start * Rate);
            int n = (int)(dur * Rate);
            double phase = 0;
            for (int i = 0; i < n && s + i < buf.Length; i++)
            {
                double t = (double)i / Rate;
                double f = f0 + (f1 - f0) * (t / dur);
                phase += f / Rate;
                double env = 1.0;
                if (t < attack) env = t / attack;
                double left = dur - t;
                if (left < release) env *= left / release;
                buf[s + i] += Osc(w, phase) * amp * (float)env;
            }
        }

        static void Kick(float[] buf, double start, float amp)
        {
            Note(buf, start, 0.14, 140, 45, Wave.Sine, amp, 0.001, 0.06);
        }

        static void Snare(float[] buf, double start, float amp)
        {
            Note(buf, start, 0.10, 0, 0, Wave.Noise, amp, 0.001, 0.08);
        }

        static void Hat(float[] buf, double start, float amp)
        {
            Note(buf, start, 0.03, 0, 0, Wave.Noise, amp, 0.0005, 0.025);
        }

        static double Hz(int midi) { return 440.0 * Math.Pow(2.0, (midi - 69) / 12.0); }

        static void Normalize(float[] buf, float peak)
        {
            float max = 0;
            foreach (var v in buf) max = Math.Max(max, Math.Abs(v));
            if (max < 1e-5f) return;
            float k = peak / max;
            for (int i = 0; i < buf.Length; i++) buf[i] *= k;
        }

        static void Save(string name, float[] buf, float peak = 0.8f)
        {
            Normalize(buf, peak);
            Directory.CreateDirectory(OutDir);
            string path = OutDir + name + ".wav";
            using (var fs = new FileStream(path, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                int bytes = buf.Length * 2;
                bw.Write(new[] { 'R', 'I', 'F', 'F' });
                bw.Write(36 + bytes);
                bw.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                bw.Write(16); bw.Write((short)1); bw.Write((short)1);
                bw.Write(Rate); bw.Write(Rate * 2); bw.Write((short)2); bw.Write((short)16);
                bw.Write(new[] { 'd', 'a', 't', 'a' });
                bw.Write(bytes);
                foreach (var v in buf) bw.Write((short)(Mathf.Clamp(v, -1f, 1f) * 32000));
            }
        }

        static float[] Buf(double seconds) { return new float[(int)(seconds * Rate)]; }

        // ------------------------------------------------------------------ efeitos
        static void Sfx()
        {
            // clique
            var b = Buf(0.07);
            Note(b, 0, 0.07, 900, 1300, Wave.Square, 0.5f, 0.002, 0.03);
            Save("sfx_click", b, 0.5f);

            // acerto: arpejo ascendente brilhante
            b = Buf(0.55);
            int[] up = { 72, 76, 79, 84 };
            for (int i = 0; i < up.Length; i++) Note(b, i * 0.08, 0.2, Hz(up[i]), Hz(up[i]), Wave.Square, 0.35f, 0.003, 0.12);
            Note(b, 0.32, 0.22, Hz(96), Hz(96), Wave.Sine, 0.25f, 0.003, 0.18);
            Save("sfx_correct", b, 0.7f);

            // erro: dois tons descendentes graves
            b = Buf(0.5);
            Note(b, 0.0, 0.22, 220, 200, Wave.Saw, 0.4f, 0.003, 0.05);
            Note(b, 0.2, 0.30, 165, 120, Wave.Saw, 0.4f, 0.003, 0.12);
            Note(b, 0.0, 0.1, 0, 0, Wave.Noise, 0.12f, 0.001, 0.08);
            Save("sfx_wrong", b, 0.7f);

            // dor do vilão ao levar dano: estouro de ruído + queda de tom
            b = Buf(0.35);
            Note(b, 0, 0.3, 520, 90, Wave.Square, 0.35f, 0.002, 0.12);
            Note(b, 0, 0.12, 0, 0, Wave.Noise, 0.3f, 0.001, 0.1);
            Save("sfx_hit", b, 0.7f);

            // raiva: rosnado grave com tremor
            b = Buf(0.7);
            for (int i = 0; i < b.Length; i++)
            {
                double t = (double)i / Rate;
                double f = 85 + 12 * Math.Sin(t * 2 * Math.PI * 9);
                b[i] = Osc(Wave.Saw, f * t) * (float)(0.5 + 0.5 * Math.Sin(t * 2 * Math.PI * 22)) * (float)Math.Min(1.0, Math.Min(t / 0.05, (0.7 - t) / 0.2));
            }
            Note(b, 0, 0.7, 0, 0, Wave.Noise, 0.06f, 0.02, 0.3);
            Save("sfx_rage", b, 0.7f);

            // combo
            b = Buf(0.35);
            int[] cb = { 79, 83, 86, 91 };
            for (int i = 0; i < cb.Length; i++) Note(b, i * 0.05, 0.12, Hz(cb[i]), Hz(cb[i]), Wave.Tri, 0.45f, 0.002, 0.08);
            Save("sfx_combo", b, 0.6f);

            // habilidade "Depurar": varredura ascendente
            b = Buf(0.45);
            Note(b, 0, 0.4, 300, 1800, Wave.Saw, 0.25f, 0.01, 0.1);
            Note(b, 0, 0.4, 600, 3600, Wave.Sine, 0.2f, 0.01, 0.1);
            Save("sfx_skill", b, 0.6f);

            // tique do relógio (últimos segundos)
            b = Buf(0.05);
            Note(b, 0, 0.05, 1400, 1400, Wave.Square, 0.4f, 0.001, 0.03);
            Save("sfx_tick", b, 0.4f);

            // vitória: fanfarra
            b = Buf(2.0);
            int[] notes = { 67, 72, 76, 79, 76, 79, 84 };
            double[] when = { 0, 0.14, 0.28, 0.42, 0.70, 0.84, 1.0 };
            double[] len = { 0.14, 0.14, 0.14, 0.26, 0.14, 0.14, 0.9 };
            for (int i = 0; i < notes.Length; i++)
            {
                Note(b, when[i], len[i], Hz(notes[i]), Hz(notes[i]), Wave.Square, 0.28f, 0.004, 0.08);
                Note(b, when[i], len[i], Hz(notes[i] - 12), Hz(notes[i] - 12), Wave.Tri, 0.3f, 0.004, 0.08);
            }
            Save("sfx_victory", b, 0.75f);

            // derrota: melodia descendente em tom menor
            b = Buf(2.2);
            int[] dn = { 69, 65, 62, 57 };
            double[] dw = { 0, 0.42, 0.84, 1.26 };
            for (int i = 0; i < dn.Length; i++)
            {
                double d = i == dn.Length - 1 ? 0.9 : 0.4;
                Note(b, dw[i], d, Hz(dn[i]), Hz(dn[i]) * (i == dn.Length - 1 ? 0.9 : 1.0), Wave.Tri, 0.5f, 0.01, 0.2);
            }
            Save("sfx_defeat", b, 0.75f);
        }

        // ------------------------------------------------------------------ músicas
        // melodia: (midi, batidas); 0 = pausa
        static void Melody(float[] buf, double bpm, int[][] seq, Wave w, float amp, int transpose = 0, double offsetBeats = 0)
        {
            double spb = 60.0 / bpm;
            double beat = offsetBeats;
            foreach (var n in seq)
            {
                double dur = n[1] / 4.0; // n[1] em semicolcheias... n[1]/4 = batidas
                if (n[0] > 0) Note(buf, beat * spb, dur * spb * 0.95, Hz(n[0] + transpose), Hz(n[0] + transpose), w, amp, 0.006, 0.07);
                beat += dur;
            }
        }

        static int[] N(int midi, int sixteenths) { return new[] { midi, sixteenths }; }

        static void MenuMusic()
        {
            double bpm = 96, spb = 60.0 / bpm;
            double totalBeats = 32;
            var b = Buf(totalBeats * spb);

            // lead (cada bloco = 1 compasso = 16 semicolcheias)
            var lead = new[]
            {
                N(76,4),N(79,4),N(84,8),
                N(83,4),N(79,4),N(81,8),
                N(77,4),N(81,4),N(84,8),
                N(83,4),N(81,4),N(79,8),
                N(76,4),N(79,4),N(84,8),
                N(86,4),N(84,4),N(83,8),
                N(81,4),N(83,4),N(84,4),N(81,4),
                N(79,12),N(0,4),
            };
            Melody(b, bpm, lead, Wave.Square, 0.16f);
            // baixo: raiz e quinta por compasso
            int[] roots = { 48, 43, 45, 40, 41, 48, 43, 48 };
            for (int bar = 0; bar < 8; bar++)
            {
                double t0 = bar * 4 * spb;
                Note(b, t0, spb * 1.9, Hz(roots[bar]), Hz(roots[bar]), Wave.Tri, 0.32f, 0.005, 0.08);
                Note(b, t0 + 2 * spb, spb * 1.9, Hz(roots[bar] + 7), Hz(roots[bar] + 7), Wave.Tri, 0.28f, 0.005, 0.08);
                // acordes arpejados suaves
                int[] chord = { roots[bar] + 12, roots[bar] + 16, roots[bar] + 19 };
                for (int k = 0; k < 8; k++)
                    Note(b, t0 + k * spb * 0.5, spb * 0.45, Hz(chord[k % 3]), Hz(chord[k % 3]), Wave.Tri, 0.07f, 0.004, 0.05);
                Hat(b, t0 + 0.5 * spb, 0.05f); Hat(b, t0 + 1.5 * spb, 0.05f);
                Hat(b, t0 + 2.5 * spb, 0.05f); Hat(b, t0 + 3.5 * spb, 0.05f);
            }
            Save("music_menu", b, 0.55f);
        }

        static void BattleMusic()
        {
            double bpm = 150, spb = 60.0 / bpm;
            int bars = 16;
            var b = Buf(bars * 4 * spb);

            int[][] pattern =
            {
                new[] {69,72,76,72,69,72,76,81},
                new[] {65,69,72,69,65,69,72,77},
                new[] {72,76,79,76,72,76,79,84},
                new[] {67,71,74,71,67,71,74,79},
                new[] {69,72,76,72,69,72,76,81},
                new[] {64,67,71,67,64,67,71,76},
                new[] {65,69,72,77,76,72,69,65},
                new[] {64,68,71,76,71,68,64,68},
            };
            int[] roots = { 45, 41, 48, 43, 45, 40, 41, 40 };

            for (int bar = 0; bar < bars; bar++)
            {
                int pi = bar % 8;
                bool second = bar >= 8;
                double t0 = bar * 4 * spb;
                for (int k = 0; k < 8; k++)
                {
                    int note = pattern[pi][k] + (second ? 12 : 0);
                    Note(b, t0 + k * spb * 0.5, spb * 0.45, Hz(note), Hz(note), Wave.Square, second ? 0.12f : 0.14f, 0.004, 0.05);
                    // baixo em colcheias
                    int bn = roots[pi] + (k % 2 == 1 ? 12 : 0);
                    Note(b, t0 + k * spb * 0.5, spb * 0.45, Hz(bn), Hz(bn), Wave.Tri, 0.35f, 0.003, 0.05);
                    Hat(b, t0 + k * spb * 0.5, 0.05f);
                }
                Kick(b, t0, 0.5f); Kick(b, t0 + 2 * spb, 0.5f);
                if (second) Kick(b, t0 + 2.5 * spb, 0.35f);
                Snare(b, t0 + spb, 0.22f); Snare(b, t0 + 3 * spb, 0.22f);
            }
            Save("music_battle", b, 0.55f);
        }

        [MenuItem("Tools/Código & Cavaleiros/Gerar Áudio")]
        public static void BuildAll()
        {
            Sfx();
            MenuMusic();
            BattleMusic();
            AssetDatabase.Refresh();
            Debug.Log("[AudioBuilder] Áudio gerado em " + OutDir);
        }
    }
}
