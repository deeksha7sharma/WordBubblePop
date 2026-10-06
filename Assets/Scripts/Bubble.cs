using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WordBubblePop
{
    /// <summary>
    /// One letter bubble. It floats up the river with a gentle side-to-side
    /// wobble. Tapping it tells the GameManager which letter was chosen.
    /// </summary>
    public class Bubble : MonoBehaviour, IPointerDownHandler
    {
        public char Letter { get; private set; }
        public float Size { get; private set; }
        public float X { get; private set; }
        public float Y { get; private set; }
        public float FX { get; private set; }
        public bool Popped { get; private set; }

        GameManager game;
        RectTransform rt;
        RectTransform skin;
        CanvasGroup group;
        Image glow;
        float speed, amp, phase, wobble;
        bool hint;

        public static Bubble Create(GameManager game, RectTransform field, char letter, float size,
            float fx, float speed, Color tint)
        {
            var root = UIKit.Rect("Bubble " + letter, field);
            UIKit.Place(root, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(size, size));
            var b = root.gameObject.AddComponent<Bubble>();
            b.group = root.gameObject.AddComponent<CanvasGroup>();
            b.game = game;
            b.rt = root;
            b.Letter = letter;
            b.Size = size;
            b.FX = fx;
            b.Y = -size - 4f;
            b.speed = speed;
            b.amp = Random.Range(6f, 16f) * size / 80f;
            b.phase = Random.Range(0f, 6f);
            b.wobble = Random.Range(1.2f, 2.2f);

            // Hint glow (hidden until the child needs help)
            b.glow = UIKit.Img("Glow", root, UIKit.SoftGlow, Palette.Mango);
            UIKit.Stretch(b.glow.rectTransform, -size * 0.35f, -size * 0.35f, -size * 0.35f, -size * 0.35f);
            b.glow.gameObject.SetActive(false);

            b.skin = UIKit.Rect("Skin", root);
            UIKit.Stretch(b.skin);

            // Body (this image receives the tap)
            var body = UIKit.Img("Body", b.skin, UIKit.Circle, Color.Lerp(tint, Color.white, 0.45f), true);
            UIKit.Stretch(body.rectTransform);
            var shadow = body.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.03f, 0.18f, 0.22f, 0.35f);
            shadow.effectDistance = new Vector2(0, -size * 0.07f);

            var ring = UIKit.Img("Ring", b.skin, UIKit.Ring, new Color(1, 1, 1, 0.92f));
            UIKit.Stretch(ring.rectTransform);

            var shine = UIKit.Img("Shine", b.skin, UIKit.Circle, new Color(1, 1, 1, 0.9f));
            UIKit.Place(shine.rectTransform, new Vector2(0.3f, 0.74f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(size * 0.2f, size * 0.17f));

            var label = UIKit.Label("Letter", b.skin, letter.ToString(), Mathf.RoundToInt(size * 0.5f), Palette.Ink, true);
            UIKit.Stretch(label.rectTransform, 0, 0, 0, -size * 0.06f);

            b.UpdatePosition(field.rect.width);
            return b;
        }

        public void Tick(float dt, float fieldWidth)
        {
            if (Popped) return;
            Y += speed * dt;
            phase += dt * wobble;
            UpdatePosition(fieldWidth);

            if (hint)
            {
                float a = 0.55f + 0.45f * Mathf.Sin(Time.time * 7f);
                glow.color = Palette.WithAlpha(Palette.Mango, a);
            }
        }

        void UpdatePosition(float fieldWidth)
        {
            X = Mathf.Clamp(FX * (fieldWidth - Size) + Mathf.Sin(phase) * amp, 0, Mathf.Max(0, fieldWidth - Size));
            rt.anchoredPosition = new Vector2(X, Y);
        }

        public void SetHint(bool on)
        {
            hint = on;
            glow.gameObject.SetActive(on);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Popped) game.OnBubbleTapped(this);
        }

        // ---------------------------------------------------------------- effects

        public void Pop()
        {
            if (Popped) return;
            Popped = true;
            group.blocksRaycasts = false;
            SetHint(false);
            StartCoroutine(PopRoutine());
        }

        IEnumerator PopRoutine()
        {
            SpawnSplash();
            float t = 0;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                float k = t / 0.25f;
                skin.localScale = Vector3.one * (1f + 0.45f * k);
                group.alpha = 1f - k;
                yield return null;
            }
            Destroy(gameObject);
        }

        void SpawnSplash()
        {
            var parent = rt.parent;
            var center = rt.anchoredPosition + new Vector2(Size, Size) * 0.5f;
            for (int i = 0; i < 8; i++)
            {
                var dot = UIKit.Img("Splash", parent, UIKit.Circle, Palette.Mango);
                float s = Size * 0.13f;
                UIKit.Place(dot.rectTransform, Vector2.zero, new Vector2(0.5f, 0.5f), center, new Vector2(s, s));
                float a = i / 8f * Mathf.PI * 2f;
                game.StartCoroutine(Fly(dot.rectTransform, center, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Size * 0.6f));
            }
        }

        static IEnumerator Fly(RectTransform dot, Vector2 from, Vector2 delta)
        {
            var img = dot.GetComponent<Image>();
            float t = 0;
            while (t < 0.45f && dot != null)
            {
                t += Time.deltaTime;
                float k = t / 0.45f;
                dot.anchoredPosition = from + delta * (1f - (1f - k) * (1f - k));
                dot.localScale = Vector3.one * (1f - 0.7f * k);
                img.color = Palette.WithAlpha(Palette.Mango, 1f - k);
                yield return null;
            }
            if (dot != null) Destroy(dot.gameObject);
        }

        public void Shake()
        {
            StopCoroutine(nameof(ShakeRoutine));
            StartCoroutine(nameof(ShakeRoutine));
        }

        IEnumerator ShakeRoutine()
        {
            float t = 0;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                float k = t / 0.4f;
                skin.anchoredPosition = new Vector2(Mathf.Sin(k * Mathf.PI * 6f) * Size * 0.09f * (1f - k), 0);
                yield return null;
            }
            skin.anchoredPosition = Vector2.zero;
        }

        /// <summary>Remove quietly (word finished or bubble left the screen).</summary>
        public void Remove(bool withPop)
        {
            if (withPop) Pop();
            else Destroy(gameObject);
        }
    }
}
