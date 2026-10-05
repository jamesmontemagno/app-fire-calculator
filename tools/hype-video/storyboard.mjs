/**
 * Hype video storyboard: the single file to edit when features ship.
 *
 * Every scene is measured in musical bars so cuts always land on a downbeat. The music
 * generator (scripts/generate-music.mjs) reads the same scene list, so lengthening a scene
 * automatically lengthens the soundtrack to match.
 *
 * Text fields support `*word*` (ember gradient highlight), `\n` line breaks in headlines, and
 * `{calculatorCount}` (length of `calculators`).
 *
 * Image paths are relative to the repository root. `web:<name>` points at a live web capture
 * produced by scripts/capture-web.mjs (see `webCaptures` below).
 */

export const config = {
  title: 'My Fire # hype video',
  width: 1920,
  height: 1080,
  fps: 30,
  bpm: 120,
  beatsPerBar: 4,
  url: 'myfirenumber.com',
  repoUrl: 'github.com/jamesmontemagno/app-fire-calculator',
  output: 'metadata/video/my-fire-number-hype.mp4',
  /** Drives the opening counter: FIRE number = annual expenses / withdrawal rate. */
  example: { expenses: 50000, withdrawalRate: 0.04 },
  music: {
    seed: 7,
    /** Root note (MIDI) and a one-chord-per-bar progression in semitones above the root. */
    root: 45, // A2
    progression: [
      [0, 3, 7], // Am
      [-4, 0, 3], // F
      [3, 7, 10], // C
      [-2, 2, 5], // G
    ],
  },
}

/**
 * Pages captured from a local production build of `web/` by scripts/capture-web.mjs.
 * `viewport: 'desktop'` is 1440x900, `viewport: 'phone'` is 390x844; both at 2x scale.
 */
export const webCaptures = [
  { name: 'home-dark', path: '/', theme: 'dark', viewport: 'desktop' },
  { name: 'standard-dark', path: '/standard', theme: 'dark', viewport: 'desktop' },
  { name: 'coast-light', path: '/coast', theme: 'light', viewport: 'desktop' },
  { name: 'roth-dark', path: '/roth-conversion', theme: 'dark', viewport: 'desktop' },
  { name: 'cash-flow-light', path: '/retirement-cash-flow', theme: 'light', viewport: 'desktop' },
  { name: 'debt-dark', path: '/debt-payoff', theme: 'dark', viewport: 'desktop' },
  { name: 'quiz-light', path: '/quiz', theme: 'light', viewport: 'desktop' },
  { name: 'phone-home-dark', path: '/', theme: 'dark', viewport: 'phone' },
  { name: 'phone-coast-light', path: '/coast', theme: 'light', viewport: 'phone' },
]

/** Calculator names for the scrolling ticker. Keep in sync with web/src/config/calculators.ts. */
export const calculators = [
  'Standard FIRE',
  'Coast FIRE',
  'Lean FIRE',
  'Fat FIRE',
  'Barista FIRE',
  'Reverse FIRE',
  'Savings Rate',
  'Debt Payoff',
  'Withdrawal Rate',
  'Healthcare Gap',
  '72(t) / SEPP',
  'Roth Conversion',
  'Interest Calculator',
  'Retirement Cash Flow',
]

/**
 * Scene types (implemented in composition/scenes.js):
 *  - intro:     flame ignites, hook line, then the logo.
 *  - pillars:   words slam in one per `beatsPerWord` beats, then all together.
 *  - showcase:  kicker + headline + feature bullets beside one or more shots that rotate.
 *  - ticker:    huge headline over scrolling calculator names, with a hero shot.
 *  - devices:   a fan of store screenshots across platforms.
 *  - statement: one big centered line with supporting chips.
 *  - outro:     platform availability, URL, open source line, and disclaimer.
 *
 * Icons are names from composition/icons.js.
 *
 * Shot frames: 'browser' (desktop web capture), 'phone-web' (phone web capture in a device),
 * 'store' (pre-framed store screenshot, shown as-is).
 *
 * `energy` drives the music for the scene's bars: intro | build | drop | groove | break | outro.
 */
export const scenes = [
  {
    type: 'intro',
    bars: 4,
    energy: 'intro',
    hook: "What's your *FIRE* number?",
    logo: 'web/public/my-fire-number-app-icon.png',
    name: 'My Fire #',
    tagline: 'Private FIRE planning tools',
  },
  {
    type: 'pillars',
    bars: 4,
    energy: 'drop',
    beatsPerWord: 2,
    summary: 'Everything included. Always.',
    words: [
      { text: 'Free', icon: 'gift' },
      { text: 'Open source', icon: 'code' },
      { text: 'No accounts', icon: 'userX' },
      { text: 'No in-app purchases', icon: 'cartOff' },
      { text: '100% private', icon: 'shield' },
    ],
  },
  {
    type: 'ticker',
    bars: 3,
    energy: 'groove',
    kicker: 'On the web',
    headline: '*{calculatorCount}* calculators.\nOne browser tab.',
    shot: { frame: 'browser', src: 'web:home-dark', url: 'myfirenumber.com' },
  },
  {
    type: 'showcase',
    bars: 4,
    energy: 'groove',
    kicker: 'Plan every path',
    headline: 'See your future,\nnot just a *number*.',
    bullets: [
      'Interactive projection charts',
      'Coast, Lean, Fat, Barista & Reverse FIRE',
      'Roth conversions, 72(t) & cash flow',
      'Share any plan with a link',
      'Export to Excel',
    ],
    shots: [
      { frame: 'browser', src: 'web:coast-light', url: 'myfirenumber.com/coast' },
      { frame: 'browser', src: 'web:roth-dark', url: 'myfirenumber.com/roth-conversion' },
      { frame: 'browser', src: 'web:cash-flow-light', url: 'myfirenumber.com/retirement-cash-flow' },
      { frame: 'browser', src: 'web:debt-dark', url: 'myfirenumber.com/debt-payoff' },
    ],
  },
  {
    type: 'showcase',
    bars: 2,
    energy: 'groove',
    kicker: 'Installable PWA',
    headline: 'Works *offline*.\nDark mode included.',
    bullets: ['Install from your browser', 'Runs with no connection', 'Light & dark themes'],
    shots: [
      { frame: 'phone-web', src: 'web:phone-home-dark' },
      { frame: 'phone-web', src: 'web:phone-coast-light' },
    ],
  },
  {
    type: 'devices',
    bars: 4,
    energy: 'drop',
    kicker: 'Native apps',
    headline: 'Your whole plan,\nin your *pocket*.',
    bullets: ['Track accounts & net worth', 'Monthly check-ins', 'History & trends', 'Privacy Mode hides every dollar'],
    shots: [
      { frame: 'store', src: 'metadata/iphone-6.5/01-home.png' },
      { frame: 'store', src: 'metadata/android-phone/02-accounts.png' },
      { frame: 'store', src: 'metadata/iphone-6.5/03-history.png' },
      { frame: 'store', src: 'metadata/android-phone/04-calculators.png' },
      { frame: 'store', src: 'metadata/iphone-6.5/05-coast-fire.png' },
    ],
  },
  {
    type: 'showcase',
    bars: 3,
    energy: 'groove',
    kicker: 'iPad, Mac & Windows',
    headline: 'Big screens,\nsame *private* plan.',
    bullets: ['iPad', 'Mac', 'Windows'],
    shots: [
      { frame: 'store', src: 'metadata/windows/01-home.png' },
      { frame: 'store', src: 'metadata/macos/04-coast-fire.png' },
      { frame: 'store', src: 'metadata/ipad-13/03-history.png' },
    ],
  },
  {
    type: 'statement',
    bars: 2,
    energy: 'break',
    headline: 'Your money stays\non *your device*.',
    chips: ['No accounts', 'No ads', 'No tracking', 'No in-app purchases', 'Works offline'],
  },
  {
    type: 'outro',
    bars: 4,
    energy: 'outro',
    headline: '*Free* on every platform.',
    platforms: [
      { name: 'Web', icon: 'globe' },
      { name: 'iPhone', icon: 'phone' },
      { name: 'iPad', icon: 'tablet' },
      { name: 'Android', icon: 'phone' },
      { name: 'Mac', icon: 'laptop' },
      { name: 'Windows', icon: 'monitor' },
    ],
    logo: 'web/public/my-fire-number-app-icon.png',
    disclaimer: 'Estimates for education only — not financial advice.',
  },
]

export function secondsPerBar(cfg = config) {
  return (60 / cfg.bpm) * cfg.beatsPerBar
}

/** Scenes with absolute start/end times in seconds. */
export function timeline(cfg = config, list = scenes) {
  const barSeconds = secondsPerBar(cfg)
  let start = 0
  let startBar = 0
  return list.map((scene, index) => {
    const duration = scene.bars * barSeconds
    const entry = { ...scene, index, start, duration, end: start + duration, startBar }
    start += duration
    startBar += scene.bars
    return entry
  })
}

export function totalSeconds(cfg = config, list = scenes) {
  return list.reduce((sum, scene) => sum + scene.bars, 0) * secondsPerBar(cfg)
}
