using UnityEngine;

namespace WordBubblePop
{
    /// <summary>
    /// Sound effects made from code (no audio files needed):
    /// pop, wrong, tick, win and game over.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        const int Rate = 44100;

        public bool Enabled = true;

        AudioSource source;
        AudioClip pop, boop, tick, win, over;

        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;

            pop = Sweep("pop", 520, 1500, 0.12f, Wave.Sine, 0.45f);
            boop = Sweep("boop", 240, 140, 0.22f, Wave.Triangle, 0.4f);
            tick = Sweep("tick", 900, 880, 0.05f, Wave.Square, 0.12f);
            win = Notes("win", new float[] { 523, 659, 784, 1047 }, 0.11f, 0.2f, 0.35f);
            over = Notes("over", new float[] { 392, 330, 262 }, 0.18f, 0.28f, 0.35f);
        }

        public void Pop() => Play(pop);
        public void Wrong() => Play(boop);
        public void Tick() => Play(tick);
        public void Win() => Play(win);
        public void GameOver() => Play(over);

        void Play(AudioClip clip)
        {
            if (Enabled && clip != null) source.PlayOneShot(clip);
        }

        enum Wave { Sine, Triangle, Square }

        static float Osc(Wave w, float phase)
        {
            float p = phase - Mathf.Floor(phase);
            switch (w)
            {
                case Wave.Triangle: return 4f * Mathf.Abs(p - 0.5f) - 1f;
                case Wave.Square: return p < 0.5f ? 1f : -1f;
                default: return Mathf.Sin(p * 2f * Mathf.PI);
            }
        }

        /// <summary>Tone that slides from f1 to f2 and fades out.</summary>
        static void Render(float[] buf, int start, float f1, float f2, float dur, Wave w, float vol)
        {
            int n = Mathf.Min((int)(dur * Rate), buf.Length - start);
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float k = t / dur;
                float f = f1 * Mathf.Pow(f2 / f1, k);
                phase += f / Rate;
                float env = Mathf.Pow(0.001f, k) * Mathf.Clamp01(i / 60f);
                buf[start + i] += Osc(w, phase) * vol * env;
            }
        }

        static AudioClip Sweep(string name, float f1, float f2, float dur, Wave w, float vol)
        {
            var buf = new float[(int)((dur + 0.02f) * Rate)];
            Render(buf, 0, f1, f2, dur, w, vol);
            return Make(name, buf);
        }

        static AudioClip Notes(string name, float[] freqs, float gap, float dur, float vol)
        {
            var buf = new float[(int)((gap * freqs.Length + dur + 0.05f) * Rate)];
            for (int i = 0; i < freqs.Length; i++)
                Render(buf, (int)(i * gap * Rate), freqs[i], freqs[i] * 1.01f, dur, Wave.Triangle, vol);
            return Make(name, buf);
        }

        static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
