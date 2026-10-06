using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace WordBubblePop
{
    /// <summary>
    /// Speaks words out loud using the phone's own voice.
    /// Android: android.speech.tts.TextToSpeech
    /// iOS:     AVSpeechSynthesizer (Assets/Plugins/iOS/WBPSpeech.mm)
    /// Mac Editor: the built-in "say" command, so you can hear it while testing.
    /// </summary>
    public static class Speech
    {
        public static bool Enabled = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject tts;
        static volatile bool ready;
        static bool languageSet;
        static int utteranceId;

        class InitListener : AndroidJavaProxy
        {
            public InitListener() : base("android.speech.tts.TextToSpeech$OnInitListener") { }
            // Called on an Android thread: only set a flag here.
            public void onInit(int status) { ready = status == 0; }
        }

        public static void Init()
        {
            if (tts != null) return;
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                    {
                        tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, new InitListener());
                    }));
                }
            }
            catch (System.Exception e) { Debug.LogWarning("TTS init failed: " + e.Message); }
        }

        public static void Say(string text, bool flush, float rate = 0.85f)
        {
            if (!Enabled || !ready || tts == null) return;
            try
            {
                if (!languageSet)
                {
                    using (var locale = new AndroidJavaObject("java.util.Locale", "en", "US"))
                        tts.Call<int>("setLanguage", locale);
                    tts.Call<int>("setPitch", 1.15f);
                    languageSet = true;
                }
                tts.Call<int>("setSpeechRate", rate);
                using (var bundle = new AndroidJavaObject("android.os.Bundle"))
                    tts.Call<int>("speak", text, flush ? 0 : 1, bundle, "wbp" + (utteranceId++));
            }
            catch (System.Exception e) { Debug.LogWarning("TTS speak failed: " + e.Message); }
        }

        public static void Stop()
        {
            if (tts == null) return;
            try { tts.Call<int>("stop"); } catch { }
        }

        public static void Shutdown()
        {
            if (tts == null) return;
            try { tts.Call("shutdown"); } catch { }
            tts = null;
            ready = false;
        }

#elif UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void _wbpSpeak(string text, int flush, float rate);
        [DllImport("__Internal")] static extern void _wbpStop();

        public static void Init() { }

        public static void Say(string text, bool flush, float rate = 0.85f)
        {
            if (!Enabled) return;
            _wbpSpeak(text, flush ? 1 : 0, rate);
        }

        public static void Stop() { _wbpStop(); }
        public static void Shutdown() { }

#elif UNITY_EDITOR_OSX
        static System.Diagnostics.Process current;

        public static void Init() { }

        public static void Say(string text, bool flush, float rate = 0.85f)
        {
            if (!Enabled) return;
            if (flush) Stop();
            try
            {
                var safe = text.Replace("\"", "");
                var wordsPerMinute = Mathf.RoundToInt(175 * rate);
                current = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "/usr/bin/say",
                    Arguments = $"-r {wordsPerMinute} \"{safe}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch (System.Exception e) { Debug.Log("say: " + e.Message); }
        }

        public static void Stop()
        {
            try { if (current != null && !current.HasExited) current.Kill(); } catch { }
            current = null;
        }

        public static void Shutdown() { Stop(); }

#else
        public static void Init() { }
        public static void Say(string text, bool flush, float rate = 0.85f)
        {
            if (Enabled) Debug.Log("[Speech] " + text);
        }
        public static void Stop() { }
        public static void Shutdown() { }
#endif
    }
}
