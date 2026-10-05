# Hype Video

A recreatable promo video for the My Fire # website and apps. Everything is generated locally:
live web captures, a procedural soundtrack, and a frame-accurate render. No financial data,
analytics, or network services are involved — the web app is served from `web/dist` on
`127.0.0.1` only.

Output: [`metadata/video/my-fire-number-hype.mp4`](../../metadata/video/my-fire-number-hype.mp4)
(1920x1080, 30 fps, 60 s, AAC audio).

## Requirements

- Node.js 20+
- ffmpeg on `PATH` (`brew install ffmpeg`, `winget install ffmpeg`, or `sudo apt install ffmpeg`)
- Chromium for Playwright (installed by `npm run setup`)

## Build it

```bash
cd tools/hype-video
npm install
npm run setup     # once: downloads Playwright's Chromium
npm run build     # capture + music + render
```

Or step by step:

| Command | What it does | Output |
| --- | --- | --- |
| `npm run check` | Validates the storyboard and prints the timeline | — |
| `npm run capture` | Builds `web/` if needed and screenshots every `webCaptures` entry | `assets/web/*.png` |
| `npm run music` | Synthesizes the soundtrack to match the scene bars | `build/music.wav` |
| `npm run preview` | Local preview with music, scrubber, and Space to play/pause | browser |
| `npm run render` | Renders every frame with Chromium and encodes with ffmpeg | `metadata/video/…mp4` |

Useful render flags (pass after `--`, e.g. `npm run render -- --stills 3,20`):

- `--stills 3,20,55` writes PNG stills to `build/stills/` (fast layout checks)
- `--from 16 --to 30 --out build/section.mp4` renders a section
- `--fps 60`, `--crf 18`, `--no-audio`
- `npm run capture -- --build` forces a fresh web build; `--only home-dark,coast-light` recaptures a subset

`assets/` and `build/` are git-ignored. Only the final MP4 is committed.

## How it fits together

```
storyboard.mjs            ← edit this: config, web captures, calculator list, scenes
scripts/capture-web.mjs   ← Playwright screenshots of web/dist (light/dark, desktop/phone)
scripts/generate-music.mjs← pure-JS synth (kick, clap, hats, bass, supersaw pads, arp, risers)
composition/              ← HTML/CSS/JS motion graphics; render(t) is a pure function of time
scripts/render.mjs        ← seeks each frame, screenshots it, pipes JPEGs into ffmpeg + music
```

Scenes are measured in **bars** (120 BPM, 2 s per bar). The music generator reads the same
scene list, so every cut lands on a downbeat and each scene's `energy`
(`intro`, `build`, `drop`, `groove`, `break`, `outro`) picks the instrumentation. Drops get a
riser and snare roll in the bar before them plus a flash on the cut.

## Adding a feature

1. If it is on the web, add a `webCaptures` entry (`name`, `path`, `theme`, `viewport`).
2. Add a bullet to an existing scene, or add a new `showcase` scene with a `kicker`,
   `headline` (`\n` for line breaks, `*word*` for the ember highlight), `bullets`, and `shots`
   (`{ frame: 'browser' | 'phone-web' | 'store', src }`). Store screenshots come from
   `metadata/`; web captures use `web:<name>`.
3. New calculator? Add it to `calculators` — the ticker and the `{calculatorCount}` headline
   token update automatically.
4. New platform? Add it to the outro `platforms` list (icons live in `composition/icons.js`).
5. `npm run check`, then `npm run capture && npm run music && npm run render`.

Keep the final scene an `outro` so the video always ends on availability and the URL.
