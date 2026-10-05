---
name: hype-video
description: Rebuild or update the My Fire # promo/hype video (website + apps) in tools/hype-video. Use when asked to regenerate the hype or promo video, add a new feature, calculator, platform, or screenshot to it, change its music, or render stills or sections of it.
---

# Hype video skill

The video is fully code-generated from `tools/hype-video/`. Read `tools/hype-video/README.md`
for the full reference. Never add analytics, uploads, or remote assets: captures come from a
local build of `web/` served on 127.0.0.1, and the music is synthesized in JavaScript.

## Fixed messaging (keep in every version)

- Free, open source, no accounts, no in-app purchases, 100% private (the `pillars` scene).
- Calculations stay on the device; estimates for education only, not financial advice
  (the outro `disclaimer`).
- End on availability (every platform) and `myfirenumber.com` (the final `outro` scene).

## Workflow

1. `cd tools/hype-video && npm install` (first time also `npm run setup` for Chromium).
   ffmpeg must be on PATH.
2. Edit `storyboard.mjs` only, unless a new scene type or icon is needed:
   - New web feature: add a `webCaptures` entry, then reference it as `web:<name>` in a
     scene's `shots`. Desktop captures use `frame: 'browser'` (pass `url`), phone captures use
     `frame: 'phone-web'`, pre-framed store images from `metadata/` use `frame: 'store'`.
   - New app feature: add a bullet and/or a `metadata/...` store screenshot to the `devices`
     or a `showcase` scene. Refresh store screenshots with `tools/store-screenshots/` first.
   - New calculator: add the name to `calculators` (mirrors `web/src/config/calculators.ts`).
     `{calculatorCount}` in headlines updates automatically.
   - New platform: add `{ name, icon }` to the outro `platforms`; add the icon path to
     `composition/icons.js` if missing.
   - Scene length is in `bars` (2 s each at 120 BPM). Keep the total around 45–75 s.
   - Text: `\n` breaks headline lines, `*word*` applies the ember gradient. Keep headlines to
     two short lines (about 22 characters per line) and bullets under about 40 characters so
     they fit the 760 px copy column.
3. `npm run check` — fix every error; warnings about missing captures mean run `npm run capture`.
4. `npm run capture` (add `-- --build` after web changes) and `npm run music`.
5. Review layout cheaply before a full render:
   `npm run render -- --stills <times>` writes PNGs to `build/stills/`. Pick a time in the
   middle and near the end of every scene (`npm run check` prints scene start times). Look at
   them and fix overlaps, clipping, or empty frames.
6. `npm run render` writes `metadata/video/my-fire-number-hype.mp4`. Confirm with
   `ffprobe` that the duration matches the storyboard total and there is an audio stream.
7. Commit the storyboard/composition changes and the updated MP4. Never commit `assets/`,
   `build/`, or `node_modules/`.

## New scene types

Add a builder to `composition/scenes.js` and register it in `builders`. A builder receives
`(root, scene, ctx, duration)`, creates its DOM once, and returns `update(local, g)` that sets
every animated style purely from `local` seconds (and `g.t`, `g.pulse` for beat-synced glow).
Never use CSS transitions, timers, or `Date.now()` — frames are rendered out of real time.

## Music

`scripts/generate-music.mjs` builds the arrangement from each scene's `energy`:
`intro` (pads + filtered arp, heartbeat kick), `drop` (full drums, 16th hats, crash on the
downbeat), `groove` (full drums), `break` (pads, no drums), `outro` (impact, groove, final
chord ring-out). The bar before a `drop` or `outro` gets a riser and snare roll. Change the
key, chords, or seed in `config.music`; change the tempo with `config.bpm` (affects timing of
every scene).
