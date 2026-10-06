using UnityEngine;

namespace WordBubblePop
{
    /// <summary>Game colours (jungle river theme).</summary>
    public static class Palette
    {
        public static readonly Color Sky = Hex("#BFE9F2");
        public static readonly Color Leaf = Hex("#2F8F5B");
        public static readonly Color LeafDeep = Hex("#1D5E3C");
        public static readonly Color Water = Hex("#2A8FA3");
        public static readonly Color WaterDeep = Hex("#155B6B");
        public static readonly Color Mango = Hex("#FFC93C");
        public static readonly Color MangoDark = Hex("#C99500");
        public static readonly Color Coral = Hex("#FF6F59");
        public static readonly Color Ink = Hex("#16323A");
        public static readonly Color Paper = Hex("#FFFAF0");
        public static readonly Color Muted = Hex("#4F6D73");
        public static readonly Color SlotBorder = Hex("#9EC7B0");
        public static readonly Color Track = Hex("#E4EFE6");
        public static readonly Color SoftGreen = Hex("#EEF8E8");
        public static readonly Color PicBg = Hex("#E3F6D9");
        public static readonly Color Gold = Hex("#E0A400");
        public static readonly Color LilyPad = new Color(0.30f, 0.69f, 0.42f, 0.55f);
        public static readonly Color Dim = new Color(0.08f, 0.36f, 0.42f, 0.6f);

        public static readonly Color[] BubbleTints =
        {
            Hex("#FF8FA3"), Hex("#7BDFF2"), Hex("#9BE58A"),
            Hex("#FFD36E"), Hex("#C8A8FF"), Hex("#FFB38A")
        };

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        public static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }
}
