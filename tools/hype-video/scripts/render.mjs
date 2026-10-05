#!/usr/bin/env node
/**
 * Renders the composition frame by frame with headless Chromium and encodes it with ffmpeg.
 *
 * Usage:
 *   node scripts/render.mjs                      # full video -> config.output
 *   node scripts/render.mjs --out out/test.mp4   # custom output path (repo-relative or absolute)
 *   node scripts/render.mjs --from 10 --to 20    # render a section (seconds)
 *   node scripts/render.mjs --stills 2,9,30      # PNG stills into build/stills/ for quick review
 *   node scripts/render.mjs --fps 60 --crf 18    # override frame rate / quality
 *   node scripts/render.mjs --no-audio           # skip the soundtrack
 */
import { spawn, spawnSync } from 'node:child_process'
import { existsSync } from 'node:fs'
import { mkdir } from 'node:fs/promises'
import { dirname, isAbsolute, join, resolve } from 'node:path'
import { chromium } from 'playwright'
import { config, totalSeconds } from '../storyboard.mjs'
import { repoRoot, startStaticServer, toolDir } from './static-server.mjs'

function option(name, fallback) {
  const index = process.argv.indexOf(`--${name}`)
  return index >= 0 ? process.argv[index + 1] : fallback
}
const flag = (name) => process.argv.includes(`--${name}`)

const fps = Number(option('fps', config.fps))
const crf = String(option('crf', 20))
const duration = totalSeconds()
const from = Math.max(0, Number(option('from', 0)))
const to = Math.min(duration, Number(option('to', duration)))
const stills = option('stills', null)
// --out is relative to the current directory; the storyboard default is relative to the repo root.
const outArg = option('out', null)
const outFile = outArg ? resolve(outArg) : isAbsolute(config.output) ? config.output : resolve(repoRoot, config.output)
const musicFile = join(toolDir, 'build', 'music.wav')
const withAudio = !flag('no-audio') && !stills

if (!stills && spawnSync('ffmpeg', ['-version']).status !== 0) {
  console.error('ffmpeg was not found on PATH. Install it (e.g. `brew install ffmpeg`, `winget install ffmpeg`, `sudo apt install ffmpeg`).')
  process.exit(1)
}
if (withAudio && !existsSync(musicFile)) {
  console.error('build/music.wav is missing. Run `npm run music` first (or pass --no-audio).')
  process.exit(1)
}

const server = await startStaticServer(repoRoot)
const browser = await chromium.launch()
const page = await browser.newPage({ viewport: { width: config.width, height: config.height }, deviceScaleFactor: 1 })
page.on('console', (message) => {
  if (message.type() === 'error') console.error(`[page] ${message.text()}`)
})
page.on('pageerror', (error) => console.error(`[page] ${error.message}`))

try {
  await page.goto(`${server.url}/tools/hype-video/composition/index.html`, { waitUntil: 'load' })
  await page.evaluate(() => window.__video.ready)

  if (stills) {
    const dir = join(toolDir, 'build', 'stills')
    await mkdir(dir, { recursive: true })
    for (const t of stills.split(',').map(Number)) {
      await page.evaluate((time) => window.__video.seek(time), t)
      const file = join(dir, `still-${t.toFixed(2)}s.png`)
      await page.screenshot({ path: file })
      console.log(`still ${t}s -> ${file}`)
    }
  } else {
    await mkdir(dirname(outFile), { recursive: true })
    const length = to - from
    const ffmpegArgs = ['-y', '-loglevel', 'error', '-f', 'image2pipe', '-framerate', String(fps), '-c:v', 'mjpeg', '-i', '-']
    if (withAudio) ffmpegArgs.push('-ss', String(from), '-t', String(length), '-i', musicFile)
    ffmpegArgs.push('-c:v', 'libx264', '-preset', 'slow', '-crf', crf, '-pix_fmt', 'yuv420p', '-r', String(fps))
    if (withAudio) ffmpegArgs.push('-c:a', 'aac', '-b:a', '192k')
    ffmpegArgs.push('-t', String(length), '-movflags', '+faststart', outFile)
    const ffmpeg = spawn('ffmpeg', ffmpegArgs, { stdio: ['pipe', 'inherit', 'inherit'] })
    const finished = new Promise((resolvePromise, reject) => {
      ffmpeg.on('error', reject)
      ffmpeg.on('close', (code) => (code === 0 ? resolvePromise() : reject(new Error(`ffmpeg exited with ${code}`))))
    })

    const frames = Math.round(length * fps)
    const startedAt = Date.now()
    for (let frame = 0; frame < frames; frame++) {
      const t = from + frame / fps
      await page.evaluate((time) => window.__video.seek(time), t)
      const jpeg = await page.screenshot({ type: 'jpeg', quality: 95 })
      if (!ffmpeg.stdin.write(jpeg)) await new Promise((r) => ffmpeg.stdin.once('drain', r))
      if (frame % fps === 0 || frame === frames - 1) {
        const elapsed = (Date.now() - startedAt) / 1000
        process.stdout.write(`\rframe ${frame + 1}/${frames}  (${t.toFixed(1)}s)  ${elapsed.toFixed(0)}s elapsed   `)
      }
    }
    ffmpeg.stdin.end()
    await finished
    console.log(`\nrendered ${length.toFixed(1)}s at ${fps} fps -> ${outFile}`)
  }
} finally {
  await browser.close()
  await server.close()
}
