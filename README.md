# Word Bubble Pop (Unity, C#)

A spelling game for kids: look at the picture, pop the letter bubbles in the right order, and finish the word within 20 seconds.
The whole game is built from C# scripts. There is nothing to set up in the scene: press Play and the game builds itself.

## Run it in Unity

1. Unzip this file. You get a folder called `WordBubblePop`.
2. Open **Unity Hub** → **Projects** → **Add** → **Add project from disk** → select the `WordBubblePop` folder → **Open**.
3. Click the project in the list to open it with Unity 6.6. The first open takes a few minutes.
4. The Console shows: "Word Bubble Pop: project settings applied" (app name, app ID, portrait mode, icon and scene are set up).
5. In the **Game** tab, switch to **Simulator** and choose a phone.
6. Press ▶ **Play**. A mouse click works like a tap.

On a Mac, the Editor also speaks the words using the built-in "say" voice.

## Files

| File | Purpose |
|---|---|
| `Scripts/GameManager.cs` | The whole game: UI, timer, levels, stars, game over, saving progress |
| `Scripts/Bubble.cs` | A single letter bubble: floating, tapping, pop and shake animations |
| `Scripts/WordData.cs` | Word lists (3, 4 and 5 letters) |
| `Scripts/Sfx.cs` | Pop, wrong, tick and win sounds, generated in code (no audio files) |
| `Scripts/Speech.cs` | Speaks words: Android TextToSpeech, iOS AVSpeechSynthesizer |
| `Scripts/UIKit.cs` | UI helpers and shapes drawn in code (circle, star, rounded box) |
| `Scripts/Palette.cs` | All colours |
| `Plugins/iOS/WBPSpeech.mm` | Small native plugin for the iOS voice |
| `Editor/ProjectSetup.cs` | Sets app name, ID, portrait mode and icon (menu: Word Bubble Pop) |
| `Editor/AndroidTtsManifest.cs` | Adds the manifest entry Android 11+ needs for text-to-speech |
| `Resources/Pictures/*.png` | One picture per word (file name = word, e.g. DOG.png) |
| `Resources/Fonts/` | Baloo 2 font |

## Changing settings

At the top of `GameManager.cs`:
- `WordTime = 20f` → seconds allowed for each word
- `WordsPerLevel = 5` → number of words in a level

To add a word: add it to `WordData.cs` and put a picture at `Resources/Pictures/WORD.png`.

## Android build

1. **File → Build Profiles** (Unity 6) or **Build Settings** → **Android** → **Switch Platform**
   (install the *Android Build Support* module from Unity Hub the first time).
2. Connect an Android phone over USB (Developer options → USB debugging ON) → **Build And Run**.
   For just the APK file, press **Build**.

## iOS build

1. **Build Profiles / Build Settings** → **iOS** → **Switch Platform**
   (requires the *iOS Build Support* module from Unity Hub) → **Build** → choose a folder.
2. Open the generated `Unity-iPhone.xcodeproj` in Xcode → choose your Team under Signing → ▶ Run.
   To run in the Simulator: Player Settings → iOS → Target SDK = Simulator SDK.
