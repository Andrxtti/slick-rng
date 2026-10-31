# The Dream (Unity)

The Dream ported from the web build to Unity 6, with UI Toolkit for the screens and C# for the game. The Solver stays on the web.

## Setup

1. Install **Unity 6** with **Android Build Support** (OpenJDK, Android SDK & NDK). iOS builds need a Mac or a cloud build service.
2. Create a new **Universal 3D** project, for example `C:\Dev\ConfirmedFireDream`.
3. Copy `Assets/Dream` and `Assets/WebGLTemplates` from this folder into the project's `Assets` folder.
4. Press **Play**. The game boots itself in any scene, so there is no scene setup, no prefab and no inspector wiring.
5. In the Game view, pick a portrait phone resolution (for example 1080x2400).

## Building for phones

Everything is on the **Dream** menu in Unity's top bar:

| Menu item | What you get |
|---|---|
| Apply mobile settings | App name "The Dream", ID `com.confirmedfire.dream`, portrait only, ARM64 IL2CPP, Android 10+, iOS 15+, the app icon |
| Build Web (iPhone Safari) | `C:\Dev\Builds\TheDream-Web`, a phone-first web version. Host the folder anywhere static (we use GitHub Pages under `docs/beta/app/`); on iPhone open it in Safari → Share → Add to Home Screen |
| Build Android APK | `C:\Dev\Builds\TheDream-0.1.0.apk`, installable on any Android 10+ phone |
| Build iOS Xcode project | `C:\Dev\Builds\TheDream-iOS`. Signing and shipping need an Apple Developer account and a Mac (or a cloud Mac build service) |

The same methods run headless: `Unity.exe -batchmode -quit -projectPath <project> -executeMethod CFDream.EditorTools.DreamBuild.BuildWeb` (or `BuildAndroid`, `BuildIOS`).

`Assets/Dream/link.xml` keeps the font and input types that the game looks up by name, so IL2CPP stripping can't remove them.

## What's here

```
Assets/Dream/
  Scripts/Core      Json (MiniJSON-style parser), Rng (mulberry32), number formatting, cards
  Scripts/Data      DB: loads dream_data.json and spots.json from Resources
  Scripts/Game      State (save in PlayerPrefs), Calendar, Career, Play (events and grading),
                    Progression, Fashion, Story, Game (the flow controller)
  Scripts/UI        DreamApp (bootstrap, panel, safe area, Home button), U (helpers, fonts),
                    Screens (title, creator, story scenes), Hub (calendar), Closet, CareerTab,
                    Run (table, verdict, results), Table (cards and felt), Portrait (2D stand-in avatar)
  Resources/Dream   dream_data.json, spots.json, dream.uss, DreamTheme.tss, Fonts/
  Fonts-Licenses    OFL and Apache texts for the bundled fonts
  Editor            DreamBuild: the Dream menu (mobile settings, web, Android and iOS builds)
  Art/Icon          app icon plus adaptive-icon background and foreground layers
Assets/WebGLTemplates/Dream   the web page around the game: safe area, loading bar, home-screen icon
tools/export_dream.py   re-exports the game data and the solved-spot library from the web build
```

## Updating the game data

The web build stays the source of truth for content (events, scenes, items, labels) and for the solved-spot library. After changing either one:

```
python3 unity/tools/export_dream.py <path to built dream.html> unity/Assets/Dream/Resources/Dream
```

## Notes

- The character is a 2D vector stand-in drawn from the same look and gear data. The realistic 3D character is the next big piece.
- Fonts are Inter (body), Gloock (display), IBM Plex Mono (numbers), DejaVu Sans (card suits) and Noto Color Emoji. When the Unity version supports it, the fonts are wired as TextCore font assets with suit and emoji fallbacks. The Console prints which parts worked as `[Dream] Fonts: ...`.
- The save is stored in PlayerPrefs under `cfdream.save.v1`. You can erase it from the error screen, or start a new career from the title screen.
