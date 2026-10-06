#if UNITY_ANDROID
using System.IO;
using UnityEditor.Android;

namespace WordBubblePop.EditorTools
{
    /// <summary>
    /// Android 11+ only lets an app talk to the phone's text-to-speech engine
    /// if the manifest declares it. This adds that line to the generated
    /// Android project every time you build, so word pronunciation works.
    /// </summary>
    public class AndroidTtsManifest : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 100;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var manifest = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifest)) return;

            var xml = File.ReadAllText(manifest);
            if (xml.Contains("android.intent.action.TTS_SERVICE")) return;

            const string queries =
                "<queries>\n" +
                "    <intent>\n" +
                "      <action android:name=\"android.intent.action.TTS_SERVICE\" />\n" +
                "    </intent>\n" +
                "  </queries>\n  ";

            int at = xml.IndexOf("<application", System.StringComparison.Ordinal);
            if (at < 0) return;
            File.WriteAllText(manifest, xml.Insert(at, queries));
        }
    }
}
#endif
