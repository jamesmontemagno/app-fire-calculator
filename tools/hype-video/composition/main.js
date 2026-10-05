// Composition engine: builds every scene from the storyboard and renders any time `t` on demand.
// Rendering is a pure function of time, so scripts/render.mjs can step frame by frame.
import { calculators, config, secondsPerBar, timeline, totalSeconds } from '../storyboard.mjs'
import { builders, clamp } from './scenes.js'

const params = new URLSearchParams(location.search)
const preview = params.has('preview')
const stage = document.getElementById('stage')
const scenesRoot = document.getElementById('scenes')
const canvas = document.getElementById('background')
const flash = document.getElementById('flash')
const fade = document.getElementById('fade')
const paint = canvas.getContext('2d')

const beat = 60 / config.bpm
const barSeconds = secondsPerBar()
const duration = totalSeconds()
const scenes = timeline()

// ---------- Assets ----------

/** Storyboard image paths are repo-relative; `web:<name>` is a capture from capture-web.mjs. */
function resolveSrc(src) {
  const path = src.startsWith('web:') ? `tools/hype-video/assets/web/${src.slice(4)}.png` : src
  return new URL(`../../../${path}`, import.meta.url).href
}

function collectImages() {
  const sources = new Set()
  for (const scene of scenes) {
    if (scene.logo) sources.add(scene.logo)
    if (scene.shot) sources.add(scene.shot.src)
    for (const shot of scene.shots ?? []) sources.add(shot.src)
  }
  return [...sources]
}

const images = new Map()
async function loadImages() {
  await Promise.all(
    collectImages().map(async (src) => {
      const image = new Image()
      image.src = resolveSrc(src)
      try {
        await image.decode()
      } catch {
        const hint = src.startsWith('web:') ? ' Run `npm run capture` first.' : ''
        throw new Error(`Could not load image "${src}" (${image.src}).${hint}`)
      }
      images.set(src, { src: image.src, width: image.naturalWidth, height: image.naturalHeight })
    }),
  )
}

// ---------- Background ----------

function mulberry32(seed) {
  let a = seed >>> 0
  return () => {
    a = (a + 0x6d2b79f5) >>> 0
    let t = a
    t = Math.imul(t ^ (t >>> 15), t | 1)
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61)
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296
  }
}
const random = mulberry32(config.music.seed)
const embers = Array.from({ length: 90 }, () => ({
  x: random() * config.width,
  phase: random(),
  speed: 0.05 + random() * 0.09,
  size: 1.2 + random() ** 2 * 4,
  sway: 20 + random() * 50,
  flicker: random() * Math.PI * 2,
  hue: random() < 0.6 ? '253,110,45' : '255,181,71',
}))

function drawBackground(t, pulse, energy) {
  const { width, height } = config
  const base = paint.createLinearGradient(0, 0, 0, height)
  base.addColorStop(0, '#0e2f24')
  base.addColorStop(1, '#04140e')
  paint.globalCompositeOperation = 'source-over'
  paint.fillStyle = base
  paint.fillRect(0, 0, width, height)

  const calm = energy === 'break' ? 0.6 : 1
  const orbs = [
    { x: 0.25 + 0.08 * Math.sin(t * 0.31), y: 0.3 + 0.1 * Math.cos(t * 0.23), r: 0.55, color: '253,110,45', a: (0.22 + 0.18 * pulse) * calm },
    { x: 0.78 + 0.07 * Math.cos(t * 0.27), y: 0.72 + 0.08 * Math.sin(t * 0.35), r: 0.6, color: '46,140,98', a: 0.35 },
    { x: 0.6 + 0.1 * Math.sin(t * 0.19 + 1), y: 0.15 + 0.06 * Math.sin(t * 0.41), r: 0.4, color: '255,181,71', a: (0.1 + 0.08 * pulse) * calm },
  ]
  paint.globalCompositeOperation = 'lighter'
  for (const orb of orbs) {
    const gradient = paint.createRadialGradient(orb.x * width, orb.y * height, 0, orb.x * width, orb.y * height, orb.r * width)
    gradient.addColorStop(0, `rgba(${orb.color},${orb.a})`)
    gradient.addColorStop(1, `rgba(${orb.color},0)`)
    paint.fillStyle = gradient
    paint.fillRect(0, 0, width, height)
  }

  // Faint perspective grid on the floor.
  paint.strokeStyle = `rgba(255,243,214,${0.05 + 0.03 * pulse})`
  paint.lineWidth = 1
  const horizon = height * 0.62
  for (let i = -12; i <= 12; i++) {
    paint.beginPath()
    paint.moveTo(width / 2 + i * 40, horizon)
    paint.lineTo(width / 2 + i * 260, height)
    paint.stroke()
  }
  for (let i = 0; i < 8; i++) {
    const p = ((i + ((t * 0.35) % 1)) / 8) ** 2
    const y = horizon + (height - horizon) * p
    paint.beginPath()
    paint.moveTo(0, y)
    paint.lineTo(width, y)
    paint.stroke()
  }

  for (const e of embers) {
    const travel = (e.phase + t * e.speed) % 1
    const y = height + 40 - travel * (height + 80)
    const x = e.x + Math.sin(t * 0.9 + e.flicker) * e.sway
    const alpha = (0.35 + 0.45 * Math.sin(t * 6 + e.flicker) ** 2) * Math.sin(travel * Math.PI) * (0.7 + 0.5 * pulse)
    const radius = e.size * (1 + 0.3 * pulse)
    const glow = paint.createRadialGradient(x, y, 0, x, y, radius * 4)
    glow.addColorStop(0, `rgba(${e.hue},${alpha})`)
    glow.addColorStop(1, `rgba(${e.hue},0)`)
    paint.fillStyle = glow
    paint.fillRect(x - radius * 4, y - radius * 4, radius * 8, radius * 8)
  }
  paint.globalCompositeOperation = 'source-over'
}

// ---------- Scenes ----------

let built = []
function build() {
  const ctx = {
    config,
    calculators,
    beat,
    barSeconds,
    tokens: { calculatorCount: String(calculators.length) },
    image(src) {
      const image = images.get(src)
      if (!image) throw new Error(`Image "${src}" was not preloaded.`)
      return image
    },
  }
  built = scenes.map((scene) => {
    const builder = builders[scene.type]
    if (!builder) throw new Error(`Unknown scene type "${scene.type}" (scene ${scene.index + 1}).`)
    const el = document.createElement('section')
    el.className = `scene scene-${scene.type}`
    scenesRoot.append(el)
    const update = builder(el, scene, ctx, scene.duration)
    return { scene, el, update }
  })
}

const drumsOn = (scene, local) =>
  scene.energy === 'drop' || scene.energy === 'groove' || (scene.energy === 'outro' && local < scene.duration - barSeconds)

function render(t) {
  const time = clamp(t, 0, duration - 1e-6)
  const current = built.find(({ scene }) => time >= scene.start && time < scene.end) ?? built[built.length - 1]
  const local = time - current.scene.start
  const pulse = drumsOn(current.scene, local) ? Math.exp(-(time % beat) * 9) : 0
  const g = { t: time, pulse, beatIndex: Math.floor(time / beat) }

  drawBackground(time, pulse, current.scene.energy)
  for (const item of built) {
    const active = item === current
    item.el.classList.toggle('active', active)
    if (!active) continue
    item.update(local, g)
    // Quick whip between scenes: slide out at the end, slide in at the start.
    const out = clamp((local - (item.scene.duration - 0.15)) / 0.15) ** 2
    const inn = 1 - clamp(local / 0.18)
    const shift = -90 * out + 90 * inn ** 2
    const blur = 8 * out + 8 * inn ** 2
    item.el.style.transform = `translateX(${shift}px)`
    item.el.style.filter = blur > 0.05 ? `blur(${blur}px)` : ''
  }

  const big = current.scene.energy === 'drop' || current.scene.energy === 'outro'
  const sinceCut = local
  flash.style.opacity = big && current.scene.index > 0 ? 0.9 * Math.exp(-sinceCut * 7) : 0.12 * Math.exp(-sinceCut * 12)
  stage.style.transform = `${stageScale ? `scale(${stageScale})` : ''} scale(${1 + 0.006 * pulse})`
  fade.style.opacity = String(Math.max(clamp(1 - time / 0.4), clamp((time - (duration - 0.7)) / 0.7)))
}

// ---------- Preview UI ----------

let stageScale = 0
function fitStage() {
  if (!preview) return
  stageScale = Math.min(innerWidth / config.width, (innerHeight - 70) / config.height)
}

function setupPreview() {
  const controls = document.getElementById('controls')
  const play = document.getElementById('play')
  const scrub = document.getElementById('scrub')
  const output = document.getElementById('time')
  const audio = document.getElementById('music')
  controls.hidden = false
  scrub.max = String(duration)
  audio.src = new URL('../build/music.wav', import.meta.url).href
  fitStage()
  addEventListener('resize', () => {
    fitStage()
    render(Number(scrub.value))
  })
  let playing = false
  let startedAt = 0
  let offset = Number(params.get('t') ?? 0)
  const show = (t) => {
    scrub.value = String(t)
    output.textContent = `${t.toFixed(2)}s`
    render(t)
  }
  const tick = () => {
    if (!playing) return
    const t = audio.duration && !audio.paused ? audio.currentTime : offset + (performance.now() - startedAt) / 1000
    if (t >= duration) {
      playing = false
      play.textContent = 'Play'
      show(duration)
      return
    }
    show(t)
    requestAnimationFrame(tick)
  }
  const toggle = () => {
    playing = !playing
    play.textContent = playing ? 'Pause' : 'Play'
    if (playing) {
      offset = Number(scrub.value) >= duration ? 0 : Number(scrub.value)
      startedAt = performance.now()
      audio.currentTime = offset
      audio.play().catch(() => {
        // No music yet (run `npm run music`); play silently.
      })
      requestAnimationFrame(tick)
    } else {
      audio.pause()
    }
  }
  play.addEventListener('click', toggle)
  addEventListener('keydown', (event) => {
    if (event.code === 'Space') {
      event.preventDefault()
      toggle()
    }
  })
  scrub.addEventListener('input', () => {
    audio.currentTime = Number(scrub.value)
    offset = Number(scrub.value)
    startedAt = performance.now()
    show(Number(scrub.value))
  })
  show(offset)
}

// ---------- Boot ----------

const ready = (async () => {
  await document.fonts.ready
  await Promise.all(
    ['500', '700', '800', '900'].map((weight) => document.fonts.load(`${weight} 64px Inter`)),
  )
  await loadImages()
  build()
  render(Number(params.get('t') ?? 0))
  if (preview) setupPreview()
})()

ready.catch((error) => {
  console.error(error)
  const message = document.createElement('pre')
  message.style.cssText = 'position:fixed;inset:auto 16px 80px;padding:16px;background:#400;color:#fff;white-space:pre-wrap;z-index:10'
  message.textContent = String(error?.message ?? error)
  document.body.append(message)
})

window.__video = {
  config,
  duration,
  ready,
  seek(t) {
    render(t)
  },
}
