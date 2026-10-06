using UnityEngine;
using UnityEngine.UI;

namespace WordBubblePop
{
    /// <summary>
    /// Small helpers to build the UI from code, plus shapes (circle, rounded box,
    /// star, gradient) drawn at runtime so the project needs no image setup.
    /// </summary>
    public static class UIKit
    {
        public static Font Regular;
        public static Font Bold;

        public static Sprite Circle;
        public static Sprite Ring;
        public static Sprite SoftGlow;
        public static Sprite RoundBox;
        public static Sprite Star;
        public static Sprite WaterGradient;

        static bool ready;

        public static void Init()
        {
            if (ready) return;
            ready = true;

            Regular = Resources.Load<Font>("Fonts/Baloo2-500");
            Bold = Resources.Load<Font>("Fonts/Baloo2-800");
            if (Regular == null) Regular = BuiltinFont();
            if (Bold == null) Bold = Regular;

            Circle = MakeCircle(256, 0f, false);
            Ring = MakeCircle(256, 0.075f, false);
            SoftGlow = MakeCircle(256, 0f, true);
            RoundBox = MakeRoundBox(128, 60);
            Star = MakeStar(128);
            WaterGradient = MakeGradient(Palette.Water, Palette.WaterDeep);
        }

        static Font BuiltinFont()
        {
            try { return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { return Resources.GetBuiltinResource<Font>("Arial.ttf"); }
        }

        // ---------------------------------------------------------------- objects

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static Image Img(string name, Transform parent, Sprite sprite, Color color, bool raycast = false)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite == RoundBox)
            {
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 1.25f;
            }
            return img;
        }

        public static Text Label(string name, Transform parent, string text, int size, Color color,
            bool bold = false, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = bold ? Bold : Regular;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.lineSpacing = 0.9f;
            return t;
        }

        public static Button Btn(string name, Transform parent, string text, Color bg, Color fg, Color shadow, int fontSize = 60)
        {
            var img = Img(name, parent, RoundBox, bg, true);
            AddShadow(img.gameObject, shadow, 12);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colors = b.colors;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            b.colors = colors;
            var label = Label("Label", img.transform, text, fontSize, fg, true);
            Stretch(label.rectTransform);
            return b;
        }

        public static void AddShadow(GameObject go, Color color, float distance)
        {
            var s = go.AddComponent<Shadow>();
            s.effectColor = color;
            s.effectDistance = new Vector2(0, -distance);
        }

        public static LayoutElement Layout(Component c, float prefW = -1, float prefH = -1)
        {
            var le = c.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            if (prefW >= 0) { le.preferredWidth = prefW; le.minWidth = prefW; }
            if (prefH >= 0) { le.preferredHeight = prefH; le.minHeight = prefH; }
            return le;
        }

        // ---------------------------------------------------------------- layout

        public static void Stretch(RectTransform rt, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        /// <summary>Full width row anchored to the top of the parent.</summary>
        public static void TopRow(RectTransform rt, float y, float height, float left, float right)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(left, -y - height);
            rt.offsetMax = new Vector2(-right, -y);
        }

        // ---------------------------------------------------------------- shapes

        static Sprite ToSprite(Texture2D tex, Vector4 border)
        {
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, border);
        }

        /// <param name="ringWidth">0 = filled circle, otherwise ring thickness as a fraction of size.</param>
        static Sprite MakeCircle(int size, float ringWidth, bool soft)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            float r = size * 0.5f - 1f;
            float inner = r - size * ringWidth;
            var c = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                float a;
                if (soft)
                {
                    float t = Mathf.Clamp01(d / r);
                    a = Mathf.Clamp01(1f - t) * Mathf.Clamp01((t - 0.55f) * 4f);
                }
                else
                {
                    a = Mathf.Clamp01(r - d + 0.5f);
                    if (ringWidth > 0) a *= Mathf.Clamp01(d - inner + 0.5f);
                }
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            return ToSprite(tex, Vector4.zero);
        }

        static Sprite MakeRoundBox(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float fx = x + 0.5f, fy = y + 0.5f;
                float cx = Mathf.Clamp(fx, radius, size - radius);
                float cy = Mathf.Clamp(fy, radius, size - radius);
                float d = Vector2.Distance(new Vector2(fx, fy), new Vector2(cx, cy));
                float a = Mathf.Clamp01(radius - d + 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            return ToSprite(tex, new Vector4(radius, radius, radius, radius));
        }

        static Sprite MakeStar(int size)
        {
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float ang = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float rad = (i % 2 == 0 ? 0.48f : 0.2f) * size;
                pts[i] = new Vector2(size * 0.5f + Mathf.Cos(ang) * rad, size * 0.52f + Mathf.Sin(ang) * rad);
            }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            const int ss = 4;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < ss; sy++)
                for (int sx = 0; sx < ss; sx++)
                    if (Inside(pts, new Vector2(x + (sx + 0.5f) / ss, y + (sy + 0.5f) / ss))) hits++;
                px[y * size + x] = new Color32(255, 255, 255, (byte)(255 * hits / (ss * ss)));
            }
            tex.SetPixels32(px);
            return ToSprite(tex, Vector4.zero);
        }

        static bool Inside(Vector2[] poly, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                    p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            }
            return inside;
        }

        static Sprite MakeGradient(Color top, Color bottom)
        {
            var tex = new Texture2D(1, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++) tex.SetPixel(0, y, Color.Lerp(bottom, top, y / 63f));
            return ToSprite(tex, Vector4.zero);
        }
    }
}
