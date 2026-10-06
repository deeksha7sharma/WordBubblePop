using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WordBubblePop
{
    /// <summary>
    /// Word Bubble Pop: a spelling game for kids.
    /// Look at the picture and pop the letter bubbles in the right order
    /// to spell the word before the timer runs out.
    ///
    /// This one script builds the whole game (camera, UI, screens) from code,
    /// so it runs in any empty scene. It starts automatically when you press Play.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ------------------------------------------------------------ settings
        const int WordsPerLevel = 5;
        const float WordTime = 20f;           // seconds per word
        const float ColumnWidth = 1080f;      // design width of the game column
        const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        const string PrefLevel = "wbp_level";
        const string PrefStars = "wbp_stars";
        const string PrefSound = "wbp_sound";

        static GameManager instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (instance != null) return;
            new GameObject("Word Bubble Pop").AddComponent<GameManager>();
        }

        // ------------------------------------------------------------ state
        int level = 1;
        int stars;
        List<string> words = new List<string>();
        int idx;
        int levelStars;
        string word = "DOG";
        int pos;
        int wrong;   // mistakes in this word (for stars)
        int miss;    // mistakes on the current letter (for hints)
        bool sound = true;
        bool running;
        bool locked = true;
        float timeLeft = WordTime;
        int lastSec;
        float spawnTimer;

        readonly List<Bubble> bubbles = new List<Bubble>();
        readonly Dictionary<string, Sprite> pictures = new Dictionary<string, Sprite>();
        Sfx sfx;

        // ------------------------------------------------------------ UI refs
        RectTransform canvasRt, safeRt, column, field;
        Rect lastSafeArea;
        Vector2 lastScreen;
        CanvasScaler scaler;

        Text levelText, starText, soundText, timerText, earnedText;
        Image picture, timerFill;
        RectTransform slotsRow;
        CanvasGroup earnedGroup;
        readonly List<Image> slotBoxes = new List<Image>();
        readonly List<Text> slotLetters = new List<Text>();
        readonly List<Image> dots = new List<Image>();

        GameObject startOverlay, levelOverlay, overOverlay;
        Text playText, levelTitle, levelStarsText, answerWord, overInfo;
        GameObject resetButton;
        RectTransform learnedGrid;
        Image answerPicture;

        // =====================================================================
        // Setup
        // =====================================================================

        void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;

            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            UIKit.Init();
            sfx = gameObject.AddComponent<Sfx>();

            level = Mathf.Max(1, PlayerPrefs.GetInt(PrefLevel, 1));
            stars = PlayerPrefs.GetInt(PrefStars, 0);
            sound = PlayerPrefs.GetInt(PrefSound, 1) == 1;
            sfx.Enabled = sound;
            Speech.Enabled = sound;

            EnsureCamera();
            EnsureEventSystem();
            BuildUI();
            Speech.Init();
            ShowStart();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Speech.Stop();
        }

        void OnApplicationQuit()
        {
            Speech.Shutdown();
        }

        static void EnsureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Sky;
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            // New Input System: use its UI module when the package is installed.
            var module = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (module != null)
            {
                es.AddComponent(module);
                return;
            }
#endif
            es.AddComponent<StandaloneInputModule>();
        }

        // =====================================================================
        // UI building
        // =====================================================================

        void BuildUI()
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasRt = (RectTransform)canvasGo.transform;

            var bg = UIKit.Img("Background", canvasRt, null, Palette.Sky);
            UIKit.Stretch(bg.rectTransform);

            safeRt = UIKit.Rect("SafeArea", canvasRt);
            column = UIKit.Rect("Column", safeRt);
            column.anchorMin = new Vector2(0.5f, 0);
            column.anchorMax = new Vector2(0.5f, 1);
            column.pivot = new Vector2(0.5f, 0.5f);
            column.sizeDelta = new Vector2(ColumnWidth, 0);
            UpdateLayout(true);

            BuildTopBar();
            BuildCard();
            BuildRiver();

            startOverlay = BuildStartOverlay();
            levelOverlay = BuildLevelOverlay();
            overOverlay = BuildGameOverOverlay();
        }

        void BuildTopBar()
        {
            var bar = UIKit.Rect("TopBar", column);
            UIKit.TopRow(bar, 30, 104, 40, 40);

            var levelPill = UIKit.Img("LevelPill", bar, UIKit.RoundBox, Palette.Paper);
            UIKit.AddShadow(levelPill.gameObject, Palette.LeafDeep, 8);
            UIKit.Place(levelPill.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(450, 104));
            levelText = UIKit.Label("Text", levelPill.transform, "", 46, Palette.Ink, true, TextAnchor.MiddleLeft);
            UIKit.Stretch(levelText.rectTransform, 32, 0, 16, 0);

            var soundBtn = UIKit.Btn("SoundButton", bar, "", Palette.Paper, Palette.Ink, Palette.LeafDeep, 38);
            UIKit.Place((RectTransform)soundBtn.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(250, 104));
            soundText = soundBtn.GetComponentInChildren<Text>();
            soundBtn.onClick.AddListener(ToggleSound);

            var starPill = UIKit.Img("StarPill", bar, UIKit.RoundBox, Palette.Paper);
            UIKit.AddShadow(starPill.gameObject, Palette.LeafDeep, 8);
            UIKit.Place(starPill.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-270, 0), new Vector2(200, 104));
            var star = UIKit.Img("Star", starPill.transform, UIKit.Star, Palette.Gold);
            UIKit.Place(star.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(26, 2), new Vector2(60, 60));
            starText = UIKit.Label("Count", starPill.transform, "0", 48, Palette.Ink, true, TextAnchor.MiddleLeft);
            UIKit.Stretch(starText.rectTransform, 98, 0, 10, 0);
        }

        void BuildCard()
        {
            var card = UIKit.Img("WordCard", column, UIKit.RoundBox, Palette.Paper);
            var outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = Palette.LeafDeep;
            outline.effectDistance = new Vector2(4, -4);
            UIKit.AddShadow(card.gameObject, Palette.LeafDeep, 16);
            UIKit.TopRow(card.rectTransform, 170, 600, 40, 40);
            var c = card.transform;

            // Picture (tap to hear the word)
            var picBg = UIKit.Img("PictureButton", c, UIKit.Circle, Palette.PicBg, true);
            UIKit.Place(picBg.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(34, -34), new Vector2(230, 230));
            var picBtn = picBg.gameObject.AddComponent<Button>();
            picBtn.targetGraphic = picBg;
            picBtn.onClick.AddListener(() => Speech.Say(word.ToLowerInvariant(), true, 0.8f));
            picture = UIKit.Img("Picture", picBg.transform, null, Color.white);
            picture.preserveAspect = true;
            UIKit.Place(picture.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(165, 165));

            var ask = UIKit.Label("Ask", c, "Look at the picture", 38, Palette.Muted, false, TextAnchor.MiddleLeft);
            UIKit.TopRow(ask.rectTransform, 52, 50, 294, 30);
            var what = UIKit.Label("Question", c, "What is this?", 78, Palette.LeafDeep, true, TextAnchor.MiddleLeft);
            UIKit.TopRow(what.rectTransform, 98, 104, 294, 30);
            var tap = UIKit.Label("TapHint", c, "Tap the picture to hear the word", 33, Palette.Muted, false, TextAnchor.MiddleLeft);
            UIKit.TopRow(tap.rectTransform, 204, 46, 294, 30);

            // Letter slots
            slotsRow = UIKit.Rect("Slots", c);
            UIKit.TopRow(slotsRow, 292, 170, 30, 30);

            // Timer
            var track = UIKit.Img("TimerTrack", c, UIKit.RoundBox, Palette.Track);
            track.pixelsPerUnitMultiplier = 3.6f;
            UIKit.TopRow(track.rectTransform, 500, 34, 44, 160);
            timerFill = UIKit.Img("TimerFill", track.transform, UIKit.RoundBox, Palette.Leaf);
            timerFill.pixelsPerUnitMultiplier = 3.6f;
            UIKit.Stretch(timerFill.rectTransform);
            timerText = UIKit.Label("Seconds", c, "20s", 46, Palette.LeafDeep, true, TextAnchor.MiddleRight);
            UIKit.Place(timerText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -484), new Vector2(130, 66));

            // Progress dots (5 words per level)
            var dotRow = UIKit.Rect("Progress", c);
            UIKit.TopRow(dotRow, 556, 26, 0, 0);
            for (int i = 0; i < WordsPerLevel; i++)
            {
                var d = UIKit.Img("Dot" + i, dotRow, UIKit.Circle, Palette.Track);
                UIKit.Place(d.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2((i - (WordsPerLevel - 1) / 2f) * 56f, 0), new Vector2(38, 24));
                dots.Add(d);
            }

            // "+3 ★" that floats up when a word is finished
            var earned = UIKit.Rect("Earned", c);
            UIKit.Place(earned, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -24), new Vector2(190, 70));
            earnedGroup = earned.gameObject.AddComponent<CanvasGroup>();
            earnedGroup.alpha = 0;
            earnedText = UIKit.Label("Text", earned, "+3", 56, Palette.Gold, true, TextAnchor.MiddleRight);
            UIKit.Stretch(earnedText.rectTransform, 0, 0, 74, 0);
            var es = UIKit.Img("Star", earned, UIKit.Star, Palette.Gold);
            UIKit.Place(es.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(64, 64));
        }

        void BuildRiver()
        {
            var river = UIKit.Img("River", column, UIKit.RoundBox, Palette.WaterDeep, true);
            river.pixelsPerUnitMultiplier = 0.8f;
            river.rectTransform.anchorMin = Vector2.zero;
            river.rectTransform.anchorMax = Vector2.one;
            river.rectTransform.offsetMin = new Vector2(40, -90);
            river.rectTransform.offsetMax = new Vector2(-40, -806);
            river.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            var water = UIKit.Img("Water", river.transform, UIKit.WaterGradient, Color.white);
            UIKit.Stretch(water.rectTransform);

            var lily1 = UIKit.Img("LilyPad", river.transform, UIKit.Circle, Palette.LilyPad);
            UIKit.Place(lily1.rectTransform, new Vector2(0.16f, 0.78f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130, 82));
            var lily2 = UIKit.Img("LilyPad", river.transform, UIKit.Circle, Palette.LilyPad);
            UIKit.Place(lily2.rectTransform, new Vector2(0.82f, 0.38f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104, 66));

            field = UIKit.Rect("BubbleField", river.transform);
            UIKit.Stretch(field, 0, 90, 0, 0);
        }

        // ------------------------------------------------------------ overlays

        static RectTransform Overlay(Transform parent, string name, out RectTransform panel)
        {
            var dim = UIKit.Img(name, parent, null, Palette.Dim, true);
            UIKit.Stretch(dim.rectTransform);

            var p = UIKit.Img("Panel", dim.transform, UIKit.RoundBox, Palette.Paper, true);
            var outline = p.gameObject.AddComponent<Outline>();
            outline.effectColor = Palette.LeafDeep;
            outline.effectDistance = new Vector2(4, -4);
            UIKit.AddShadow(p.gameObject, Palette.LeafDeep, 18);
            UIKit.Place(p.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920, 0));

            var v = p.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(56, 56, 60, 64);
            v.spacing = 30;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            p.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            panel = p.rectTransform;
            return dim.rectTransform;
        }

        Button PanelButton(RectTransform panel, string text, bool primary)
        {
            var b = primary
                ? UIKit.Btn("Button", panel, text, Palette.Mango, Palette.Ink, Palette.MangoDark, 62)
                : UIKit.Btn("Button", panel, text, Palette.SoftGreen, Palette.Muted, new Color(0, 0, 0, 0), 42);
            UIKit.Layout(b, -1, primary ? 140 : 96);
            return b;
        }

        GameObject BuildStartOverlay()
        {
            var root = Overlay(canvasRt, "StartScreen", out var panel);

            LogoRow(panel, "WORD BUBBLE");
            LogoRow(panel, "POP");

            UIKit.Label("Title", panel, "Pop the bubbles, make the word!", 62, Palette.LeafDeep, true);
            Step(panel, "1", "Look at the picture, like a dog or a cat.");
            Step(panel, "2", "Pop the letter bubbles in the right order: D, O, G.");
            Step(panel, "3", "You have 20 seconds for each word. Finish in time to win stars!");

            var play = PanelButton(panel, "Play", true);
            playText = play.GetComponentInChildren<Text>();
            play.onClick.AddListener(() =>
            {
                startOverlay.SetActive(false);
                StartLevel();
            });

            var reset = PanelButton(panel, "Start over", false);
            resetButton = reset.gameObject;
            reset.onClick.AddListener(() =>
            {
                level = 1;
                stars = 0;
                Save();
                startOverlay.SetActive(false);
                StartLevel();
            });

            return root.gameObject;
        }

        static void LogoRow(RectTransform panel, string text)
        {
            var row = UIKit.Rect("Logo", panel);
            UIKit.Layout(row, -1, 76);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter;
            h.spacing = 6;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            int i = 0;
            foreach (char ch in text)
            {
                if (ch == ' ')
                {
                    UIKit.Layout(UIKit.Rect("Gap", row), 22, 70);
                    continue;
                }
                var bub = UIKit.Img("L", row, UIKit.Circle, Palette.BubbleTints[i++ % Palette.BubbleTints.Length]);
                UIKit.Layout(bub, 70, 70);
                var t = UIKit.Label("T", bub.transform, ch.ToString(), 40, Palette.Ink, true);
                UIKit.Stretch(t.rectTransform);
            }
        }

        static void Step(RectTransform panel, string number, string text)
        {
            var row = UIKit.Img("Step", panel, UIKit.RoundBox, Palette.SoftGreen);
            row.pixelsPerUnitMultiplier = 2f;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(22, 22, 18, 18);
            h.spacing = 22;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;

            var num = UIKit.Img("Number", row.transform, UIKit.Circle, Palette.Leaf);
            UIKit.Layout(num, 60, 60);
            var n = UIKit.Label("N", num.transform, number, 36, Color.white, true);
            UIKit.Stretch(n.rectTransform);

            var t = UIKit.Label("Text", row.transform, text, 38, Palette.Ink, false, TextAnchor.MiddleLeft);
            var le = UIKit.Layout(t, 640);
            le.flexibleWidth = 1;
        }

        GameObject BuildLevelOverlay()
        {
            var root = Overlay(canvasRt, "LevelCompleteScreen", out var panel);

            levelTitle = UIKit.Label("Title", panel, "Well done! Level 1 complete", 66, Palette.LeafDeep, true);

            var starsRow = UIKit.Rect("Stars", panel);
            UIKit.Layout(starsRow, -1, 100);
            var star = UIKit.Img("Star", starsRow, UIKit.Star, Palette.Gold);
            UIKit.Place(star.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(96, 96));
            levelStarsText = UIKit.Label("Count", starsRow, "15", 90, Palette.Gold, true, TextAnchor.MiddleLeft);
            UIKit.Place(levelStarsText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(240, 100));

            UIKit.Label("Learned", panel, "Words you learned today:", 42, Palette.Ink);

            learnedGrid = UIKit.Rect("LearnedWords", panel);
            var grid = learnedGrid.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(250, 104);
            grid.spacing = new Vector2(14, 14);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;

            var next = PanelButton(panel, "Next level", true);
            next.onClick.AddListener(() =>
            {
                levelOverlay.SetActive(false);
                StartLevel();
            });

            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        GameObject BuildGameOverOverlay()
        {
            var root = Overlay(canvasRt, "TimesUpScreen", out var panel);

            UIKit.Label("Title", panel, "Oops! Time's up", 72, Palette.LeafDeep, true);
            UIKit.Label("Sub", panel, "That's okay! The word was:", 42, Palette.Ink);

            var answer = UIKit.Img("Answer", panel, UIKit.RoundBox, Palette.SoftGreen);
            UIKit.Layout(answer, -1, 210);
            answerPicture = UIKit.Img("Picture", answer.transform, null, Color.white);
            answerPicture.preserveAspect = true;
            UIKit.Place(answerPicture.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1, 0.5f), new Vector2(-30, 0), new Vector2(160, 160));
            answerWord = UIKit.Label("Word", answer.transform, "DOG", 96, Palette.Ink, true, TextAnchor.MiddleLeft);
            UIKit.Place(answerWord.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(380, 160));

            overInfo = UIKit.Label("Info", panel, "", 40, Palette.Ink);

            var retry = PanelButton(panel, "Play again", true);
            retry.onClick.AddListener(() =>
            {
                overOverlay.SetActive(false);
                StartLevel();
            });

            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        // =====================================================================
        // Layout (safe area + column width)
        // =====================================================================

        void UpdateLayout(bool force = false)
        {
            var screen = new Vector2(Screen.width, Screen.height);
            var sa = Screen.safeArea;
            if (!force && sa == lastSafeArea && screen == lastScreen) return;
            lastSafeArea = sa;
            lastScreen = screen;

            // Portrait phones: fit width. Wide screens (Editor, tablets): fit height.
            float aspect = screen.x / Mathf.Max(1f, screen.y);
            scaler.matchWidthOrHeight = aspect > 1080f / 1920f ? 1f : 0f;

            safeRt.anchorMin = new Vector2(sa.xMin / Mathf.Max(1, screen.x), sa.yMin / Mathf.Max(1, screen.y));
            safeRt.anchorMax = new Vector2(sa.xMax / Mathf.Max(1, screen.x), sa.yMax / Mathf.Max(1, screen.y));
            safeRt.offsetMin = Vector2.zero;
            safeRt.offsetMax = Vector2.zero;

        }

        /// <summary>Keeps the game column at most 1080 units wide, centred in the safe area.</summary>
        void UpdateColumn()
        {
            float available = safeRt.rect.width;
            float width = available > 0 ? Mathf.Min(available, ColumnWidth) : ColumnWidth;
            if (Mathf.Abs(column.sizeDelta.x - width) > 0.5f || column.sizeDelta.y != 0)
            {
                column.sizeDelta = new Vector2(width, 0);
                column.anchoredPosition = Vector2.zero;
            }
        }

        // =====================================================================
        // Game flow
        // =====================================================================

        void ShowStart()
        {
            if (level > 1)
            {
                playText.text = "Play level " + level;
                resetButton.SetActive(true);
            }
            else
            {
                playText.text = "Play";
                resetButton.SetActive(false);
            }

            // Example word on the card behind the start screen
            word = "DOG";
            picture.sprite = Picture(word);
            BuildSlots(word);
            SetSlot(0, 'D', SlotState.Filled, false);
            SetSlot(1, null, SlotState.Current, false);
            words = new List<string>(new string[WordsPerLevel]);
            idx = 0;
            RenderTop();
            RenderProgress();
            timeLeft = WordTime;
            RenderTimer();
            startOverlay.SetActive(true);
        }

        void StartLevel()
        {
            StopAllCoroutines();
            words = WordData.PickWords(level, WordsPerLevel);
            idx = 0;
            levelStars = 0;
            running = true;
            RenderTop();
            RenderProgress();
            NextWord();
        }

        void NextWord()
        {
            word = words[idx];
            pos = 0;
            wrong = 0;
            miss = 0;
            picture.sprite = Picture(word);
            BuildSlots(word);
            SetSlot(0, null, SlotState.Current, false);
            ClearBubbles(false);
            spawnTimer = SpawnInterval();
            timeLeft = WordTime;
            lastSec = Mathf.CeilToInt(WordTime);
            RenderTimer();
            locked = false;
        }

        char Need => word[pos];

        void Update()
        {
            UpdateLayout();
            UpdateColumn();
            if (!running) return;

            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            float w = field.rect.width;
            float h = field.rect.height;

            if (!locked)
            {
                timeLeft -= dt;
                int sec = Mathf.CeilToInt(timeLeft);
                if (sec != lastSec)
                {
                    lastSec = sec;
                    if (sec <= 5 && sec > 0) sfx.Tick();
                }
                RenderTimer();
                if (timeLeft <= 0f)
                {
                    GameOver();
                    return;
                }

                spawnTimer += dt;
                if (spawnTimer >= SpawnInterval() && bubbles.Count < MaxBubbles())
                {
                    spawnTimer = 0;
                    Spawn(w, h);
                }
            }

            for (int i = bubbles.Count - 1; i >= 0; i--)
            {
                var b = bubbles[i];
                if (b == null) { bubbles.RemoveAt(i); continue; }
                b.Tick(dt, w);
                if (b.Y > h + 10f)
                {
                    bubbles.RemoveAt(i);
                    b.Remove(false);
                }
            }

            // Gentle pulse on the slot waiting for the next letter
            if (!locked && pos < slotBoxes.Count)
            {
                var o = slotBoxes[pos].GetComponent<Outline>();
                if (o != null) o.effectColor = Palette.WithAlpha(Palette.Coral, 0.6f + 0.4f * Mathf.Sin(Time.time * 6f));
            }
        }

        // ------------------------------------------------------------ difficulty

        float Speed(float fieldHeight) => (34f + 6f * Mathf.Min(level, 8)) * Mathf.Max(0.75f, fieldHeight / 480f);
        float SpawnInterval() => Mathf.Max(0.65f, 1.2f - level * 0.07f);
        int MaxBubbles() => Mathf.Min(9, 5 + level);

        // ------------------------------------------------------------ bubbles

        void Spawn(float w, float h)
        {
            float size = Mathf.Clamp(w / 5.2f, 120f, 200f);
            float fx = Random.value;
            for (int t = 0; t < 6; t++)
            {
                bool crowded = false;
                foreach (var b in bubbles)
                {
                    if (b != null && !b.Popped && b.Y < size * 1.6f && Mathf.Abs(b.FX - fx) < 0.22f)
                    {
                        crowded = true;
                        break;
                    }
                }
                if (!crowded) break;
                fx = Random.value;
            }

            char letter = ChooseLetter(h);
            var tint = Palette.BubbleTints[Random.Range(0, Palette.BubbleTints.Length)];
            var bubble = Bubble.Create(this, field, letter, size, fx, Speed(h) * Random.Range(0.85f, 1.15f), tint);
            if (miss >= 2 && letter == Need) bubble.SetHint(true);
            bubbles.Add(bubble);
        }

        /// <summary>
        /// Makes sure the next letter the child needs is always on screen,
        /// mixed with other letters from the word and a few random ones.
        /// </summary>
        char ChooseLetter(float h)
        {
            char need = Need;
            bool needVisible = false;
            foreach (var b in bubbles)
            {
                if (b != null && !b.Popped && b.Letter == need && b.Y < h * 0.7f)
                {
                    needVisible = true;
                    break;
                }
            }

            float r = Random.value;
            if (!needVisible || r < 0.35f) return need;
            if (r < 0.65f) return word[Random.Range(0, word.Length)];
            return Alphabet[Random.Range(0, Alphabet.Length)];
        }

        void ClearBubbles(bool pop)
        {
            foreach (var b in bubbles)
            {
                if (b == null) continue;
                if (pop) b.Pop();
                else b.Remove(false);
            }
            bubbles.Clear();
        }

        public void OnBubbleTapped(Bubble b)
        {
            if (locked || !running || b.Popped) return;

            if (b.Letter == Need)
            {
                sfx.Pop();
                bubbles.Remove(b);
                b.Pop();
                SetSlot(pos, b.Letter, SlotState.Filled, true);
                pos++;
                miss = 0;
                foreach (var x in bubbles) if (x != null) x.SetHint(false);

                if (pos < word.Length)
                {
                    SetSlot(pos, null, SlotState.Current, false);
                    Speech.Say(b.Letter.ToString(), true, 1f);
                }
                else
                {
                    WordDone();
                }
            }
            else
            {
                sfx.Wrong();
                wrong++;
                miss++;
                b.Shake();
                if (miss >= 2)
                    foreach (var x in bubbles)
                        if (x != null && x.Letter == Need) x.SetHint(true);
            }
        }

        // ------------------------------------------------------------ results

        void SayWord()
        {
            Speech.Say(string.Join(", ", word.ToCharArray()) + ". " + word.ToLowerInvariant(), true, 0.8f);
        }

        void WordDone()
        {
            locked = true;
            int earned = wrong == 0 ? 3 : wrong <= 2 ? 2 : 1;
            stars += earned;
            levelStars += earned;
            RenderTop();
            StartCoroutine(ShowEarned(earned));
            sfx.Win();
            ClearBubbles(true);
            SayWord();
            StartCoroutine(AfterWord());
        }

        IEnumerator AfterWord()
        {
            yield return new WaitForSeconds(2.8f);
            idx++;
            RenderProgress();
            if (idx >= words.Count) LevelDone();
            else NextWord();
        }

        void LevelDone()
        {
            running = false;
            levelTitle.text = "Well done! Level " + level + " complete";
            levelStarsText.text = levelStars.ToString();

            for (int i = learnedGrid.childCount - 1; i >= 0; i--) Destroy(learnedGrid.GetChild(i).gameObject);
            foreach (var w in words)
            {
                var cell = UIKit.Img("Word", learnedGrid, UIKit.RoundBox, Palette.SoftGreen);
                cell.pixelsPerUnitMultiplier = 2f;
                var pic = UIKit.Img("Picture", cell.transform, Picture(w), Color.white);
                pic.preserveAspect = true;
                UIKit.Place(pic.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(74, 74));
                var t = UIKit.Label("Text", cell.transform, w, 38, Palette.Ink, true, TextAnchor.MiddleLeft);
                UIKit.Stretch(t.rectTransform, 98, 0, 8, 0);
            }

            levelOverlay.SetActive(true);
            StartCoroutine(Confetti(levelOverlay.transform));
            level++;
            Save();
        }

        void GameOver()
        {
            locked = true;
            running = false;
            timeLeft = 0;
            RenderTimer();
            sfx.GameOver();
            ClearBubbles(true);

            for (int i = 0; i < word.Length; i++)
                SetSlot(i, word[i], i < pos ? SlotState.Filled : SlotState.Empty, false);

            answerPicture.sprite = Picture(word);
            answerWord.text = word;
            overInfo.text = idx == 0
                ? "Try level " + level + " again. You can do it!"
                : "You finished " + idx + (idx == 1 ? " word" : " words") + " in level " + level + ". Try again!";
            StartCoroutine(ShowGameOverLater());
        }

        IEnumerator ShowGameOverLater()
        {
            yield return new WaitForSeconds(0.6f);
            overOverlay.SetActive(true);
            SayWord();
        }

        void Save()
        {
            PlayerPrefs.SetInt(PrefLevel, level);
            PlayerPrefs.SetInt(PrefStars, stars);
            PlayerPrefs.Save();
        }

        void ToggleSound()
        {
            sound = !sound;
            sfx.Enabled = sound;
            Speech.Enabled = sound;
            if (!sound) Speech.Stop();
            PlayerPrefs.SetInt(PrefSound, sound ? 1 : 0);
            RenderTop();
        }

        // =====================================================================
        // Rendering helpers
        // =====================================================================

        Sprite Picture(string w)
        {
            if (pictures.TryGetValue(w, out var s)) return s;
            var tex = Resources.Load<Texture2D>("Pictures/" + w);
            s = tex != null
                ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f)
                : null;
            pictures[w] = s;
            return s;
        }

        void RenderTop()
        {
            levelText.text = "Level " + level + "  <size=34><color=#4F6D73>" + WordData.LevelInfo(level) + "</color></size>";
            starText.text = stars.ToString();
            soundText.text = sound ? "Sound On" : "Sound Off";
        }

        void RenderProgress()
        {
            for (int i = 0; i < dots.Count; i++)
            {
                dots[i].color = i < idx ? Palette.Leaf
                    : i == idx ? Palette.Mango
                    : Palette.WithAlpha(Palette.LeafDeep, 0.18f);
            }
        }

        void RenderTimer()
        {
            float t = Mathf.Max(0f, timeLeft);
            timerFill.rectTransform.anchorMax = new Vector2(t / WordTime, 1f);
            timerText.text = Mathf.CeilToInt(t) + "s";
            bool low = t <= 5f;
            timerFill.color = low ? Palette.Coral : Palette.Leaf;
            timerText.color = low ? Palette.Coral : Palette.LeafDeep;
        }

        enum SlotState { Empty, Current, Filled }

        void BuildSlots(string w)
        {
            for (int i = slotsRow.childCount - 1; i >= 0; i--) Destroy(slotsRow.GetChild(i).gameObject);
            slotBoxes.Clear();
            slotLetters.Clear();

            int n = w.Length;
            const float gap = 18f;
            float slotW = Mathf.Min(140f, (900f - (n - 1) * gap) / n);
            float total = n * slotW + (n - 1) * gap;
            for (int i = 0; i < n; i++)
            {
                var box = UIKit.Img("Slot" + i, slotsRow, UIKit.RoundBox, Color.white);
                box.pixelsPerUnitMultiplier = 2.2f;
                var o = box.gameObject.AddComponent<Outline>();
                o.effectDistance = new Vector2(5, -5);
                UIKit.Place(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(-total / 2f + slotW / 2f + i * (slotW + gap), 0), new Vector2(slotW, 166));
                var t = UIKit.Label("Letter", box.transform, "", 96, Palette.Ink, true);
                UIKit.Stretch(t.rectTransform, 0, 0, 0, -8);
                slotBoxes.Add(box);
                slotLetters.Add(t);
                SetSlot(i, null, SlotState.Empty, false);
            }
        }

        void SetSlot(int i, char? letter, SlotState state, bool animate)
        {
            if (i < 0 || i >= slotBoxes.Count) return;
            var box = slotBoxes[i];
            var o = box.GetComponent<Outline>();
            slotLetters[i].text = letter.HasValue ? letter.Value.ToString() : "";
            switch (state)
            {
                case SlotState.Filled:
                    box.color = Palette.Mango;
                    o.effectColor = Palette.Gold;
                    break;
                case SlotState.Current:
                    box.color = Color.white;
                    o.effectColor = Palette.Coral;
                    break;
                default:
                    box.color = Color.white;
                    o.effectColor = Palette.SlotBorder;
                    break;
            }
            if (animate) StartCoroutine(PopScale(box.rectTransform));
        }

        static IEnumerator PopScale(RectTransform rt)
        {
            float t = 0;
            while (t < 0.3f && rt != null)
            {
                t += Time.deltaTime;
                float k = t / 0.3f;
                float s = k < 0.7f ? Mathf.Lerp(0.6f, 1.15f, k / 0.7f) : Mathf.Lerp(1.15f, 1f, (k - 0.7f) / 0.3f);
                rt.localScale = Vector3.one * s;
                yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
        }

        IEnumerator ShowEarned(int earned)
        {
            earnedText.text = "+" + earned;
            var rt = (RectTransform)earnedGroup.transform;
            var basePos = new Vector2(-30, -24);
            float t = 0;
            while (t < 1.4f)
            {
                t += Time.deltaTime;
                float k = t / 1.4f;
                earnedGroup.alpha = k < 0.2f ? k / 0.2f : k > 0.8f ? (1f - k) / 0.2f : 1f;
                rt.anchoredPosition = basePos + new Vector2(0, Mathf.Lerp(-10f, 24f, k));
                yield return null;
            }
            earnedGroup.alpha = 0;
            rt.anchoredPosition = basePos;
        }

        static IEnumerator Confetti(Transform parent)
        {
            var area = (RectTransform)parent;
            float w = area.rect.width;
            var pieces = new List<(RectTransform rt, float speed, float spin)>();
            var colors = new List<Color>(Palette.BubbleTints) { Palette.Mango, Palette.Coral };
            for (int i = 0; i < 40; i++)
            {
                var img = UIKit.Img("Confetti", parent, null, colors[i % colors.Count]);
                UIKit.Place(img.rectTransform, new Vector2(0, 1), new Vector2(0.5f, 0.5f),
                    new Vector2(Random.value * w, Random.Range(0f, 300f)), new Vector2(20, 30));
                pieces.Add((img.rectTransform, Random.Range(500f, 950f), Random.Range(-400f, 400f)));
            }

            float t = 0;
            while (t < 4f)
            {
                t += Time.deltaTime;
                foreach (var p in pieces)
                {
                    if (p.rt == null) continue;
                    p.rt.anchoredPosition += new Vector2(0, -p.speed * Time.deltaTime);
                    p.rt.Rotate(0, 0, p.spin * Time.deltaTime);
                }
                yield return null;
            }
            foreach (var p in pieces) if (p.rt != null) Destroy(p.rt.gameObject);
        }
    }
}
