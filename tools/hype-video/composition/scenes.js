// Scene builders. Each builder creates its DOM once and returns `update(local, g)`, which must
// set every animated style purely from `local` (seconds since the scene started) so any frame
// can be rendered in any order.
import { icon } from './icons.js'

// ---------- Animation helpers ----------

export const clamp = (x, min = 0, max = 1) => Math.min(max, Math.max(min, x))
export const easeOutCubic = (x) => 1 - (1 - x) ** 3
export const easeInCubic = (x) => x ** 3
export const easeInOutCubic = (x) => (x < 0.5 ? 4 * x ** 3 : 1 - (-2 * x + 2) ** 3 / 2)
export const easeOutBack = (x) => {
  const c1 = 1.70158
  const c3 = c1 + 1
  return 1 + c3 * (x - 1) ** 3 + c1 * (x - 1) ** 2
}
/** Eased 0..1 progress of a `length`-second tween beginning at `start`. */
export const tween = (local, start, length, ease = easeOutCubic) => ease(clamp((local - start) / length))

/** Standard entrance: fade + rise + optional scale/blur, driven by eased progress `p`. */
function reveal(el, p, { x = 0, y = 50, scale = 1, blur = 10, rotate = 0 } = {}) {
  const inv = 1 - p
  el.style.opacity = clamp(p * 1.4)
  el.style.transform = `translate(${x * inv}px, ${y * inv}px) scale(${1 + (scale - 1) * inv}) rotate(${rotate * inv}deg)`
  el.style.filter = blur ? `blur(${blur * inv}px)` : ''
}

// ---------- Markup helpers ----------

const escapeHtml = (text) =>
  String(text).replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c])

/** Escapes text, then turns `*word*` into the ember gradient and fills `{tokens}`. */
function rich(text, ctx) {
  const filled = String(text).replace(/\{(\w+)\}/g, (match, key) => (key in ctx.tokens ? ctx.tokens[key] : match))
  return escapeHtml(filled).replace(/\*(.+?)\*/g, '<span class="hot">$1</span>')
}

function html(markup) {
  const template = document.createElement('template')
  template.innerHTML = markup.trim()
  return template.content.firstElementChild
}

/** Wraps each headline line so lines can enter one after another. */
function headlineLines(text, ctx) {
  return String(text)
    .split('\n')
    .map((line) => `<span class="anim line" style="display:block">${rich(line, ctx)}</span>`)
    .join('')
}

// ---------- Shots ----------

const BROWSER_BAR = 46
const PHONE_BEZEL = 14

/** Creates a framed screenshot sized to fit inside `box` ({ w, h }), centered on its anchor. */
function createShot(shot, box, ctx) {
  const image = ctx.image(shot.src)
  const aspect = image.width / image.height
  let el
  let width
  let height
  if (shot.frame === 'browser') {
    width = Math.min(box.w, (box.h - BROWSER_BAR) * aspect)
    height = width / aspect + BROWSER_BAR
    el = html(`<div class="shot browser anim">
      <div class="bar"><span class="dot"></span><span class="dot"></span><span class="dot"></span>
      <span class="url">${escapeHtml(shot.url ?? ctx.config.url)}</span></div>
      <div class="page"><img alt="" src="${image.src}"></div></div>`)
  } else if (shot.frame === 'phone-web') {
    const innerH = Math.min(box.h - PHONE_BEZEL * 2, (box.w - PHONE_BEZEL * 2) / aspect)
    width = innerH * aspect + PHONE_BEZEL * 2
    height = innerH + PHONE_BEZEL * 2
    el = html(`<div class="shot phone anim"><img alt="" src="${image.src}"></div>`)
  } else {
    width = Math.min(box.w, box.h * aspect)
    height = width / aspect
    el = html(`<div class="shot store anim"><img alt="" src="${image.src}"></div>`)
  }
  el.style.width = `${width}px`
  el.style.height = `${height}px`
  el.style.marginLeft = `${-width / 2}px`
  el.style.marginTop = `${-height / 2}px`
  return { el, width, height }
}

// ---------- Shared copy block (kicker + headline + bullets) ----------

function createCopy(scene, ctx) {
  const copy = html(`<div class="copy">
    ${scene.kicker ? `<div class="kicker anim">${rich(scene.kicker, ctx)}</div>` : ''}
    <h2 class="headline">${headlineLines(scene.headline, ctx)}</h2>
    ${
      scene.bullets?.length
        ? `<ul class="bullets">${scene.bullets
            .map((b) => `<li class="anim"><span class="check">${icon('check')}</span>${rich(b, ctx)}</li>`)
            .join('')}</ul>`
        : ''
    }</div>`)
  const kicker = copy.querySelector('.kicker')
  const lines = [...copy.querySelectorAll('.line')]
  const bullets = [...copy.querySelectorAll('.bullets li')]
  const beat = ctx.beat
  return {
    el: copy,
    update(local) {
      if (kicker) reveal(kicker, tween(local, 0, 0.45), { x: -40, y: 0, blur: 0 })
      lines.forEach((line, i) => reveal(line, tween(local, 0.08 + i * 0.16, 0.6), { y: 70, blur: 14 }))
      bullets.forEach((li, i) => reveal(li, tween(local, 0.7 + i * beat, 0.45, easeOutBack), { x: -50, y: 0, blur: 0 }))
    },
  }
}

/** Rotating shots: each gets an equal slice of the scene; slides in from the right. */
function createRotator(container, shots, box, ctx, center, duration) {
  const items = shots.map((shot) => createShot(shot, box, ctx))
  for (const item of items) container.append(item.el)
  const slice = duration / items.length
  return (local) => {
    items.forEach((item, i) => {
      const start = i * slice
      const enter = i === 0 ? tween(local, 0.05, 0.7) : tween(local, start - 0.2, 0.6)
      const leave = i === items.length - 1 ? 0 : tween(local, start + slice - 0.2, 0.5, easeInCubic)
      const visible = enter > 0 && leave < 1
      item.el.style.visibility = visible ? 'visible' : 'hidden'
      if (!visible) return
      const drift = (local - start) * 6 // slow push-in
      const x = center.x + 160 * (1 - enter) - 220 * leave
      const scale = (0.9 + 0.1 * enter) * (1 - 0.06 * leave) * (1 + drift * 0.003)
      item.el.style.left = `${x}px`
      item.el.style.top = `${center.y}px`
      item.el.style.opacity = Math.min(enter, 1 - leave)
      item.el.style.transform = `perspective(2200px) rotateY(${-9 + 4 * leave}deg) scale(${scale})`
      item.el.style.zIndex = String(10 + i)
    })
  }
}

/** Tall shots side by side, rising in one per beat. */
function createRow(container, shots, area, ctx, center) {
  const gap = 48
  const boxW = (area.w - gap * (shots.length - 1)) / shots.length
  const items = shots.map((shot) => createShot(shot, { w: boxW, h: area.h }, ctx))
  const total = items.reduce((sum, item) => sum + item.width, 0) + gap * (items.length - 1)
  let x = center.x - total / 2
  for (const item of items) {
    item.cx = x + item.width / 2
    x += item.width + gap
    container.append(item.el)
  }
  return (local, g) => {
    items.forEach((item, i) => {
      const p = tween(local, 0.15 + i * ctx.beat, 0.8, easeOutBack)
      const float = Math.sin(g.t * 1.4 + i * 1.7) * 10
      item.el.style.left = `${item.cx}px`
      item.el.style.top = `${center.y}px`
      item.el.style.opacity = clamp(p * 2)
      item.el.style.transform = `translateY(${(1 - p) * 700 + float}px) rotate(${(i - (items.length - 1) / 2) * 3}deg)`
    })
  }
}

// ---------- Scene types ----------

function intro(root, scene, ctx) {
  const { expenses, withdrawalRate } = ctx.config.example
  const target = expenses / withdrawalRate
  const format = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 })
  const caption = `${format.format(expenses)} a year ÷ ${+(withdrawalRate * 100).toFixed(2)}% withdrawal rate`
  const words = scene.hook.split(' ')
  const el = html(`<div>
    <div class="center hook-block anim">
      <div class="intro-hook">${words.map((w) => `<span class="anim">${rich(w, ctx)}</span>`).join('')}</div>
      <div class="intro-number hot anim">$0</div>
      <div class="intro-caption anim">${escapeHtml(caption)}</div>
    </div>
    <div class="center brand anim">
      <img class="logo anim" alt="" src="${ctx.image(scene.logo).src}">
      <div class="brand-name anim">${rich(scene.name, ctx)}</div>
      <div class="brand-tagline anim">${rich(scene.tagline, ctx)}</div>
    </div></div>`)
  root.append(el)
  const hookBlock = el.querySelector('.hook-block')
  const hookWords = [...el.querySelectorAll('.intro-hook span')]
  const number = el.querySelector('.intro-number')
  const captionEl = el.querySelector('.intro-caption')
  const brand = el.querySelector('.brand')
  const logo = el.querySelector('.logo')
  const name = el.querySelector('.brand-name')
  const tagline = el.querySelector('.brand-tagline')
  const bar = ctx.barSeconds

  return (local) => {
    hookWords.forEach((w, i) => reveal(w, tween(local, 0.3 + i * 0.28, 0.6), { y: 80, blur: 18 }))
    const count = tween(local, 1.6, 1.9, easeInOutCubic)
    number.textContent = format.format(Math.round((target * count) / 1000) * 1000)
    reveal(number, tween(local, 1.5, 0.5), { y: 40, scale: 0.8, blur: 10 })
    reveal(captionEl, tween(local, 2.2, 0.5), { y: 20, blur: 6 })
    const out = tween(local, bar * 2 - 0.35, 0.35, easeInCubic)
    hookBlock.style.opacity = 1 - out
    hookBlock.style.transform = `scale(${1 - 0.15 * out})`
    hookBlock.style.filter = `blur(${out * 16}px)`

    const brandIn = bar * 2
    brand.style.visibility = local >= brandIn - 0.01 ? 'visible' : 'hidden'
    reveal(logo, tween(local, brandIn, 0.7, easeOutBack), { y: 0, scale: 0.3, blur: 0, rotate: -12 })
    reveal(name, tween(local, brandIn + 0.35, 0.6), { y: 60, blur: 14 })
    reveal(tagline, tween(local, brandIn + 0.65, 0.6), { y: 30, blur: 8 })
    // The riser bar: push in, then blow out into the drop.
    const push = tween(local, bar * 3, bar, easeInCubic)
    const blow = tween(local, bar * 4 - 0.3, 0.3, easeInCubic)
    brand.style.transform = `scale(${1 + 0.12 * push + 0.8 * blow})`
    brand.style.opacity = 1 - blow
    brand.style.filter = `brightness(${1 + 0.6 * push})`
  }
}

function pillars(root, scene, ctx) {
  const wordTime = (scene.beatsPerWord ?? 2) * ctx.beat
  const el = html(`<div>
    ${scene.words
      .map(
        (w) => `<div class="pillar-word"><span class="anim pop">${icon(w.icon)}</span><span class="text anim">${rich(w.text, ctx)}</span></div>`,
      )
      .join('')}
    <div class="pillar-grid">
      <div class="kicker anim">${rich(scene.summary ?? '', ctx)}</div>
      <div class="row">${scene.words.map((w) => `<span class="chip anim">${icon(w.icon)}${rich(w.text, ctx)}</span>`).join('')}</div>
    </div></div>`)
  root.append(el)
  const words = [...el.querySelectorAll('.pillar-word')]
  const grid = el.querySelector('.pillar-grid')
  const summary = grid.querySelector('.kicker')
  const chips = [...grid.querySelectorAll('.chip')]
  const gridStart = words.length * wordTime

  return (local) => {
    words.forEach((word, i) => {
      const start = i * wordTime
      const visible = local >= start && local < start + wordTime
      word.style.visibility = visible ? 'visible' : 'hidden'
      if (!visible) return
      const d = local - start
      const slam = tween(d, 0, 0.18)
      const shake = Math.exp(-d * 14) * Math.sin(d * 90) * 14
      const text = word.querySelector('.text')
      text.style.opacity = clamp(d / 0.06)
      text.style.transform = `translate(${shake}px, 0) scale(${1.6 - 0.6 * slam + 0.04 * (d / wordTime)})`
      const pop = word.querySelector('.pop')
      reveal(pop, tween(d, 0.05, 0.4, easeOutBack), { y: 0, scale: 0, blur: 0, rotate: -25 })
      text.classList.toggle('hot', i % 2 === 1)
    })
    grid.style.visibility = local >= gridStart ? 'visible' : 'hidden'
    reveal(summary, tween(local, gridStart, 0.4), { y: -30, blur: 0 })
    chips.forEach((chip, i) => reveal(chip, tween(local, gridStart + 0.1 + i * 0.12, 0.5, easeOutBack), { y: 60, scale: 0.6, blur: 0 }))
  }
}

function showcase(root, scene, ctx, duration) {
  const el = html(`<div class="split"><div class="shots"></div></div>`)
  const copy = createCopy(scene, ctx)
  el.prepend(copy.el)
  root.append(el)
  const container = el.querySelector('.shots')
  const center = { x: 520, y: 540 }
  const allTall =
    scene.shots.length <= 3 &&
    scene.shots.every((shot) => {
      const image = ctx.image(shot.src)
      return image.width / image.height < 0.85
    })
  const animateShots = allTall
    ? createRow(container, scene.shots, { w: 980, h: 820 }, ctx, center)
    : createRotator(container, scene.shots, { w: 1000, h: 760 }, ctx, center, duration)
  return (local, g) => {
    copy.update(local)
    animateShots(local, g)
  }
}

function ticker(root, scene, ctx) {
  const el = html(`<div>
    <div class="split" style="bottom:120px"><div class="shots"></div></div>
    <div class="ticker anim" style="bottom:56px"></div>
  </div>`)
  const split = el.querySelector('.split')
  const copy = createCopy(scene, ctx)
  split.prepend(copy.el)
  const tickerRow = el.querySelector('.ticker')
  const names = [...ctx.calculators, ...ctx.calculators]
  tickerRow.innerHTML = names.map((n) => `<span class="chip">${icon('check')}${escapeHtml(n)}</span>`).join('')
  root.append(el)
  const shot = createShot(scene.shot, { w: 1000, h: 700 }, ctx)
  el.querySelector('.shots').append(shot.el)

  return (local) => {
    copy.update(local)
    const p = tween(local, 0.1, 0.9)
    shot.el.style.left = `${520 + 220 * (1 - p)}px`
    shot.el.style.top = '480px'
    shot.el.style.opacity = clamp(p * 1.5)
    shot.el.style.transform = `perspective(2200px) rotateY(${-14 + 5 * p}deg) rotateX(${4 * (1 - p)}deg) scale(${0.85 + 0.15 * p + local * 0.006})`
    const rowIn = tween(local, 0.4, 0.6)
    tickerRow.style.opacity = rowIn
    tickerRow.style.transform = `translateX(${-local * 190}px) translateY(${(1 - rowIn) * 60}px)`
  }
}

function devices(root, scene, ctx) {
  const el = html(`<div class="split"><div class="shots"></div></div>`)
  const copy = createCopy(scene, ctx)
  el.prepend(copy.el)
  root.append(el)
  const container = el.querySelector('.shots')
  const n = scene.shots.length
  const spread = Math.min(320, 640 / Math.max(1, (n - 1) / 2))
  const items = scene.shots.map((shot) => createShot(shot, { w: 520, h: 760 }, ctx))
  items.forEach((item) => container.append(item.el))

  return (local, g) => {
    copy.update(local)
    const settle = tween(local, ctx.beat * n, 1.6, easeInOutCubic)
    items.forEach((item, i) => {
      const offset = i - (n - 1) / 2
      const p = tween(local, 0.1 + i * ctx.beat * 0.75, 0.9, easeOutBack)
      const fanX = offset * (spread * 0.55 + spread * 0.45 * settle)
      const float = Math.sin(g.t * 1.3 + i * 1.3) * 10
      item.el.style.left = `${520 + fanX}px`
      item.el.style.top = `${560 + Math.abs(offset) * 34}px`
      item.el.style.zIndex = String(100 - Math.round(Math.abs(offset) * 10))
      item.el.style.opacity = clamp(p * 2)
      item.el.style.transform = `translateY(${(1 - p) * 800 + float}px) rotate(${offset * 6}deg) scale(${1 - Math.abs(offset) * 0.06})`
    })
  }
}

function statement(root, scene, ctx) {
  const el = html(`<div class="center statement">
    <div class="lock anim">${icon('lockShackle', 'icon shackle')}${icon('lockBody')}</div>
    <h2 class="headline">${headlineLines(scene.headline, ctx)}</h2>
    <div class="chip-row">${(scene.chips ?? []).map((c) => `<span class="chip anim">${icon('check')}${rich(c, ctx)}</span>`).join('')}</div>
  </div>`)
  root.append(el)
  const lock = el.querySelector('.lock')
  const shackle = el.querySelector('.shackle')
  const lines = [...el.querySelectorAll('.line')]
  const chips = [...el.querySelectorAll('.chip')]
  return (local) => {
    reveal(lock, tween(local, 0, 0.5, easeOutBack), { y: -40, scale: 0.5, blur: 0 })
    const click = tween(local, 0.55, 0.18, easeInCubic)
    shackle.style.transform = `translateY(${-22 * (1 - click)}%)`
    lock.style.filter = `drop-shadow(0 0 ${30 * Math.exp(-Math.max(0, local - 0.73) * 4) * (local > 0.73 ? 1 : 0)}px rgba(255,181,71,0.9))`
    lines.forEach((line, i) => reveal(line, tween(local, 0.2 + i * 0.2, 0.7), { y: 60, blur: 16 }))
    chips.forEach((chip, i) => reveal(chip, tween(local, 1.0 + i * 0.22, 0.45, easeOutBack), { y: 40, scale: 0.7, blur: 0 }))
  }
}

function outro(root, scene, ctx) {
  const url = ctx.config.url
  const el = html(`<div class="center outro">
    <h2 class="headline anim">${rich(scene.headline, ctx)}</h2>
    <div class="platforms">${scene.platforms.map((p) => `<span class="chip anim">${icon(p.icon)}${rich(p.name, ctx)}</span>`).join('')}</div>
    <div class="url-lockup">
      <img class="logo anim" alt="" src="${ctx.image(scene.logo).src}">
      <div class="url"><span class="typed"></span><span class="underline"></span></div>
    </div>
    <div class="repo anim">${icon('github')}Open source · ${escapeHtml(ctx.config.repoUrl)}</div>
    <div class="disclaimer anim">${escapeHtml(scene.disclaimer ?? '')}</div>
  </div>`)
  root.append(el)
  const headline = el.querySelector('.headline')
  const chips = [...el.querySelectorAll('.platforms .chip')]
  const logo = el.querySelector('.url-lockup .logo')
  const typed = el.querySelector('.typed')
  const underline = el.querySelector('.underline')
  const repo = el.querySelector('.repo')
  const disclaimer = el.querySelector('.disclaimer')
  const beat = ctx.beat
  return (local, g) => {
    reveal(headline, tween(local, 0, 0.6), { y: 70, scale: 1.15, blur: 18 })
    chips.forEach((chip, i) => reveal(chip, tween(local, 0.5 + i * beat * 0.5, 0.45, easeOutBack), { y: 50, scale: 0.6, blur: 0 }))
    const urlStart = 0.5 + chips.length * beat * 0.5 + 0.2
    reveal(logo, tween(local, urlStart, 0.6, easeOutBack), { y: 0, scale: 0.2, blur: 0, rotate: -20 })
    const chars = Math.round(url.length * tween(local, urlStart + 0.15, 0.9, (x) => x))
    typed.innerHTML = `${escapeHtml(url.slice(0, chars))}`
    typed.classList.toggle('hot', chars === url.length)
    underline.style.transform = `scaleX(${tween(local, urlStart + 1.05, 0.5)})`
    reveal(repo, tween(local, urlStart + 1.3, 0.6), { y: 20, blur: 6 })
    reveal(disclaimer, tween(local, urlStart + 1.6, 0.8), { y: 0, blur: 0 })
    const glow = 0.35 + 0.25 * g.pulse
    logo.style.boxShadow = `0 30px 90px rgba(0,0,0,.5), 0 0 ${80 + 60 * g.pulse}px rgba(253,110,45,${glow})`
  }
}

export const builders = { intro, pillars, showcase, ticker, devices, statement, outro }
