#!/usr/bin/env node
/**
 * Procedural soundtrack for the hype video. Pure JavaScript, no samples, no network.
 *
 * The arrangement is derived from the storyboard: every scene contributes `bars` of music and
 * its `energy` picks the instrumentation, so the drops and breakdowns always line up with the
 * cuts. Output: tools/hype-video/build/music.wav (44.1 kHz, 16-bit stereo).
 *
 * Usage: node scripts/generate-music.mjs [--out path.wav]
 */
import { mkdir, writeFile } from 'node:fs/promises'
import { dirname, join, resolve } from 'node:path'
import { config, secondsPerBar, timeline, totalSeconds } from '../storyboard.mjs'
import { toolDir } from './static-server.mjs'

const args = process.argv.slice(2)
const outIndex = args.indexOf('--out')
const outFile = outIndex >= 0 ? resolve(args[outIndex + 1]) : join(toolDir, 'build', 'music.wav')

const SR = 44100
const beat = 60 / config.bpm
const bar = secondsPerBar()
const duration = totalSeconds()
const length = Math.ceil(duration * SR)

// ---------------------------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------------------------

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
const noise = () => random() * 2 - 1

const midiToHz = (note) => 440 * 2 ** ((note - 69) / 12)

/** Band-limited sawtooth via PolyBLEP. */
function polyBlep(phase, inc) {
  if (phase < inc) {
    const t = phase / inc
    return t + t - t * t - 1
  }
  if (phase > 1 - inc) {
    const t = (phase - 1) / inc
    return t * t + t + t + 1
  }
  return 0
}

/** Topology-preserving state-variable filter (Zavalishin). Returns lowpass by default. */
function createSvf() {
  let ic1 = 0
  let ic2 = 0
  return (input, cutoff, resonance = 0.2, mode = 'lp') => {
    const g = Math.tan((Math.PI * Math.min(cutoff, SR * 0.45)) / SR)
    const k = 2 - 2 * resonance
    const a1 = 1 / (1 + g * (g + k))
    const a2 = g * a1
    const a3 = g * a2
    const v3 = input - ic2
    const v1 = a1 * ic1 + a2 * v3
    const v2 = ic2 + a2 * ic1 + a3 * v3
    ic1 = 2 * v1 - ic1
    ic2 = 2 * v2 - ic2
    if (mode === 'hp') return input - k * v1 - v2
    if (mode === 'bp') return v1
    return v2
  }
}

function createBus() {
  return { left: new Float32Array(length), right: new Float32Array(length) }
}

function add(bus, index, left, right = left) {
  if (index < 0 || index >= length) return
  bus.left[index] += left
  bus.right[index] += right
}

// ---------------------------------------------------------------------------------------------
// Instruments
// ---------------------------------------------------------------------------------------------

function kick(bus, time, gain = 1) {
  const start = Math.round(time * SR)
  const samples = Math.round(0.45 * SR)
  let phase = 0
  for (let i = 0; i < samples; i++) {
    const t = i / SR
    const freq = 48 + 110 * Math.exp(-t * 32)
    phase += freq / SR
    const env = Math.exp(-t * 7.5)
    const click = i < 90 ? noise() * 0.25 * (1 - i / 90) : 0
    const value = Math.tanh(Math.sin(2 * Math.PI * phase) * 1.6) * env * 0.9 + click
    add(bus, start + i, value * gain)
  }
}

function clap(bus, send, time, gain = 1) {
  const start = Math.round(time * SR)
  const samples = Math.round(0.3 * SR)
  const filter = createSvf()
  for (let i = 0; i < samples; i++) {
    const t = i / SR
    // Three quick re-triggers give the classic clap smear.
    const retrigger = t < 0.03 ? Math.exp(-((t * 1000) % 10) * 0.35) : 1
    const env = Math.exp(-t * 16) * retrigger
    const body = Math.sin(2 * Math.PI * 190 * t) * Math.exp(-t * 30) * 0.4
    const value = (filter(noise(), 1800, 0.35, 'bp') * 1.8 + body) * env * gain * 0.55
    add(bus, start + i, value)
    add(send, start + i, value * 0.5)
  }
}

function hat(bus, time, gain = 1, open = false) {
  const start = Math.round(time * SR)
  const decay = open ? 9 : 45
  const samples = Math.round((open ? 0.35 : 0.08) * SR)
  const filter = createSvf()
  const pan = (random() - 0.5) * 0.5
  for (let i = 0; i < samples; i++) {
    const t = i / SR
    const value = filter(noise(), 8500, 0.1, 'hp') * Math.exp(-t * decay) * 0.22 * gain
    add(bus, start + i, value * (1 - pan), value * (1 + pan))
  }
}

function crash(bus, send, time, gain = 1) {
  const start = Math.round(time * SR)
  const samples = Math.round(2.6 * SR)
  const filter = createSvf()
  for (let i = 0; i < samples; i++) {
    const t = i / SR
    const l = filter(noise(), 5000, 0.1, 'hp') * Math.exp(-t * 1.7) * 0.28 * gain
    const r = l * 0.85 + noise() * 0.02 * Math.exp(-t * 1.7) * gain
    add(bus, start + i, l, r)
    add(send, start + i, l * 0.4, r * 0.4)
  }
  // Sub boom underneath the impact.
  for (let i = 0; i < Math.round(1.4 * SR); i++) {
    const t = i / SR
    add(bus, start + i, Math.sin(2 * Math.PI * (38 + 30 * Math.exp(-t * 6)) * t) * Math.exp(-t * 2.6) * 0.55 * gain)
  }
}

function riser(bus, send, time, seconds) {
  const start = Math.round(time * SR)
  const samples = Math.round(seconds * SR)
  const filter = createSvf()
  let phase = 0
  for (let i = 0; i < samples; i++) {
    const progress = i / samples
    const cutoff = 300 + 9000 * progress ** 2
    const noiseValue = filter(noise(), cutoff, 0.55, 'bp') * 0.5
    phase += (220 + 880 * progress ** 2) / SR
    const tone = Math.sin(2 * Math.PI * phase) * 0.08
    const env = progress ** 1.6 * 0.6
    const value = (noiseValue + tone) * env
    add(bus, start + i, value * (1 - progress * 0.3), value * (0.7 + progress * 0.3))
    add(send, start + i, value * 0.3)
  }
}

function saw(phase, inc) {
  return 2 * phase - 1 - polyBlep(phase, inc)
}

function bassNote(bus, time, note, seconds, cutoff = 900) {
  const start = Math.round(time * SR)
  const samples = Math.round(seconds * SR)
  const freq = midiToHz(note)
  const inc = freq / SR
  const filter = createSvf()
  let phase = random()
  let sub = 0
  for (let i = 0; i < samples; i++) {
    const t = i / SR
    phase = (phase + inc) % 1
    sub = (sub + inc) % 1
    const env = Math.min(1, t * 400) * Math.exp(-t * 3.2) * (i > samples - 200 ? (samples - i) / 200 : 1)
    const filtered = filter(saw(phase, inc), cutoff * (0.6 + Math.exp(-t * 14)), 0.35)
    const value = (filtered * 0.32 + Math.sin(2 * Math.PI * sub) * 0.38) * env
    add(bus, start + i, value)
  }
}

function padChord(bus, send, time, notes, seconds, cutoff, gain = 1) {
  const start = Math.round(time * SR)
  const samples = Math.round(seconds * SR)
  const detunes = [-0.12, -0.05, 0, 0.06, 0.13]
  for (const note of notes) {
    const voices = detunes.map((detune, index) => ({
      inc: midiToHz(note + detune) / SR,
      phase: random(),
      pan: (index / (detunes.length - 1)) * 2 - 1,
    }))
    const filterL = createSvf()
    const filterR = createSvf()
    for (let i = 0; i < samples; i++) {
      const t = i / SR
      let l = 0
      let r = 0
      for (const voice of voices) {
        voice.phase = (voice.phase + voice.inc) % 1
        const value = saw(voice.phase, voice.inc)
        l += value * (1 - voice.pan * 0.6)
        r += value * (1 + voice.pan * 0.6)
      }
      const attack = Math.min(1, t / 0.12)
      const release = Math.min(1, (samples - i) / (0.25 * SR))
      const env = attack * release * 0.035 * gain
      const c = typeof cutoff === 'function' ? cutoff(i / samples) : cutoff
      const outL = filterL(l, c, 0.15) * env
      const outR = filterR(r, c, 0.15) * env
      add(bus, start + i, outL, outR)
      add(send, start + i, outL * 0.6, outR * 0.6)
    }
  }
}

function pluck(bus, send, time, note, cutoff, gain = 1, pan = 0) {
  const start = Math.round(time * SR)
  const samples = Math.round(0.32 * SR)
  const inc = midiToHz(note) / SR
  const filter = createSvf()
  let phase = 0
  let phase2 = 0.5
  for (let i = 0; i < samples; i++) {
    const t = i / SR
    phase = (phase + inc) % 1
    phase2 = (phase2 + inc * 1.005) % 1
    const square = (phase < 0.5 ? 1 : -1) * 0.5 + saw(phase2, inc) * 0.5
    const env = Math.min(1, t * 600) * Math.exp(-t * 11)
    const value = filter(square, cutoff * (0.4 + 1.6 * Math.exp(-t * 20)), 0.3) * env * 0.16 * gain
    add(bus, start + i, value * (1 - pan), value * (1 + pan))
    add(send, start + i, value * 0.5, value * 0.5)
  }
}

// ---------------------------------------------------------------------------------------------
// Arrangement
// ---------------------------------------------------------------------------------------------

const drums = createBus()
const music = createBus() // ducked by the kick (sidechain)
const reverbSend = createBus()
const delaySend = createBus()
const kickTimes = []

const { root, progression } = config.music
const scenes = timeline()

function chordFor(barIndex) {
  return progression[barIndex % progression.length].map((offset) => root + offset)
}

for (const [sceneIndex, scene] of scenes.entries()) {
  const next = scenes[sceneIndex + 1]
  const nextIsBig = next && (next.energy === 'drop' || next.energy === 'outro')
  for (let local = 0; local < scene.bars; local++) {
    const barIndex = scene.startBar + local
    const t0 = scene.start + local * bar
    const chord = chordFor(barIndex)
    const lastBar = local === scene.bars - 1
    const progress = (local + 1) / scene.bars
    const energy = scene.energy

    // Pads everywhere, brightness follows energy.
    const padNotes = chord.map((n) => n + 24)
    const padCutoff = {
      intro: (p) => 500 + 2600 * ((local + p) / scene.bars) ** 2,
      build: 2500,
      drop: 4200,
      groove: 2600,
      break: 1400,
      outro: 3800,
    }[energy]
    if (!(energy === 'outro' && local === scene.bars - 1)) {
      padChord(music, reverbSend, t0, padNotes, bar + 0.05, padCutoff, energy === 'break' ? 1.2 : 1)
    } else {
      // Final chord rings out over the last bar.
      padChord(music, reverbSend, t0, [...padNotes, padNotes[0] + 12], bar, (p) => 3800 - 3000 * p, 1.3)
    }

    // Arpeggio.
    const arpTones = [...chord.map((n) => n + 24), ...chord.map((n) => n + 36)]
    const arpPattern = [0, 2, 4, 3, 1, 3, 5, 4, 2, 4, 3, 1, 0, 2, 4, 5]
    const arpOn = energy !== 'intro' || local >= 1
    if (arpOn && !(energy === 'outro' && lastBar)) {
      for (let step = 0; step < 16; step++) {
        const time = t0 + step * (beat / 4)
        const cutoff = energy === 'intro' ? 600 + 2400 * progress : energy === 'break' ? 1500 : 4200
        const gain = (step % 4 === 0 ? 1.15 : 0.85) * (energy === 'intro' ? 0.7 : 1)
        pluck(music, delaySend, time, arpTones[arpPattern[step] % arpTones.length], cutoff, gain, step % 2 ? 0.35 : -0.35)
      }
    }

    // Drums and bass.
    const full = energy === 'drop' || energy === 'groove' || (energy === 'outro' && local < scene.bars - 1)
    if (full) {
      for (let b = 0; b < 4; b++) {
        const time = t0 + b * beat
        kick(drums, time)
        kickTimes.push(time)
        if (b % 2 === 1) clap(drums, reverbSend, time, 0.9)
        hat(drums, time + beat / 2, 1)
        if (energy === 'drop') {
          hat(drums, time + beat / 4, 0.45)
          hat(drums, time + (3 * beat) / 4, 0.45)
        }
        // Off-beat octave bass.
        bassNote(music, time + beat / 2, chord[0] - 12, beat / 2 - 0.01, energy === 'drop' ? 1300 : 950)
        bassNote(music, time, chord[0] - 12, beat / 2 - 0.01, 600)
      }
      if (local % 2 === 1) hat(drums, t0 + 3.5 * beat, 0.6, true)
    } else if (energy === 'intro' && local >= scene.bars - 2) {
      // Heartbeat kick to tease the drop.
      kick(drums, t0, 0.7)
      kickTimes.push(t0)
      if (lastBar) for (let s = 0; s < 8; s++) hat(drums, t0 + s * (beat / 2), 0.3 + s * 0.08)
    } else if (energy === 'build') {
      // Four-on-the-floor kick and eighth hats, no bass, so the next drop lands harder.
      for (let b = 0; b < 4; b++) {
        kick(drums, t0 + b * beat, 0.8)
        kickTimes.push(t0 + b * beat)
        hat(drums, t0 + b * beat + beat / 2, 0.8)
      }
    } else if (energy === 'break') {
      bassNote(music, t0, chord[0] - 12, bar * 0.95, 400)
    }

    // Snare roll + riser into a drop or the outro.
    if (lastBar && nextIsBig) {
      riser(music, reverbSend, t0, bar)
      for (let s = 0; s < 16; s++) {
        const p = s / 16
        if (s >= 8 || s % 2 === 0) clap(drums, reverbSend, t0 + s * (beat / 4), 0.25 + p * 0.6)
      }
    }

    // Impacts on the first downbeat of big moments.
    if (local === 0 && (energy === 'drop' || energy === 'outro')) crash(drums, reverbSend, t0, 1)
  }
}

// ---------------------------------------------------------------------------------------------
// Effects and mixdown
// ---------------------------------------------------------------------------------------------

// Sidechain: duck the music bus on every kick.
const duck = new Float32Array(length).fill(1)
for (const time of kickTimes) {
  const start = Math.round(time * SR)
  const samples = Math.round(beat * SR)
  for (let i = 0; i < samples && start + i < length; i++) {
    const t = i / SR
    const amount = t < 0.005 ? t / 0.005 : 1
    duck[start + i] = Math.min(duck[start + i], 1 - 0.65 * amount * Math.exp(-t * 9))
  }
}

// Ping-pong dotted-eighth delay.
const delaySamples = Math.round(beat * 0.75 * SR)
const delayed = createBus()
for (let i = 0; i < length; i++) {
  const fromL = i >= delaySamples ? delayed.right[i - delaySamples] : 0
  const fromR = i >= delaySamples ? delayed.left[i - delaySamples] : 0
  delayed.left[i] = delaySend.left[i] + fromL * 0.42
  delayed.right[i] = delaySend.right[i] * 0.2 + fromR * 0.42
}

// Schroeder reverb: parallel combs into series allpasses, per channel.
function reverb(input, offset) {
  const combs = [1557, 1617, 1491, 1422].map((d) => ({ buffer: new Float32Array(d + offset), index: 0, store: 0 }))
  const allpasses = [225, 556].map((d) => ({ buffer: new Float32Array(d + offset), index: 0 }))
  const output = new Float32Array(length)
  for (let i = 0; i < length; i++) {
    let sum = 0
    for (const comb of combs) {
      const out = comb.buffer[comb.index]
      comb.store = out * 0.7 + comb.store * 0.3
      comb.buffer[comb.index] = input[i] + comb.store * 0.84
      comb.index = (comb.index + 1) % comb.buffer.length
      sum += out
    }
    let value = sum * 0.25
    for (const ap of allpasses) {
      const buffered = ap.buffer[ap.index]
      ap.buffer[ap.index] = value + buffered * 0.5
      value = buffered - value * 0.5
      ap.index = (ap.index + 1) % ap.buffer.length
    }
    output[i] = value
  }
  return output
}
const reverbL = reverb(reverbSend.left, 0)
const reverbR = reverb(reverbSend.right, 23)

const left = new Float32Array(length)
const right = new Float32Array(length)
let peak = 0
for (let i = 0; i < length; i++) {
  const d = duck[i]
  let l = drums.left[i] + (music.left[i] + delayed.left[i] * 0.5) * d + reverbL[i] * 0.35 * d
  let r = drums.right[i] + (music.right[i] + delayed.right[i] * 0.5) * d + reverbR[i] * 0.35 * d
  l = Math.tanh(l * 1.1)
  r = Math.tanh(r * 1.1)
  left[i] = l
  right[i] = r
  peak = Math.max(peak, Math.abs(l), Math.abs(r))
}

// Normalize to -1 dBFS with short fade-in and a fade-out across the final 1.5 seconds.
const target = 10 ** (-1 / 20)
const gain = peak > 0 ? target / peak : 1
const fadeIn = Math.round(0.02 * SR)
const fadeOut = Math.round(1.5 * SR)
const pcm = Buffer.alloc(length * 4)
for (let i = 0; i < length; i++) {
  let env = 1
  if (i < fadeIn) env = i / fadeIn
  if (i > length - fadeOut) env = Math.max(0, (length - i) / fadeOut) ** 1.5
  const l = Math.max(-1, Math.min(1, left[i] * gain * env))
  const r = Math.max(-1, Math.min(1, right[i] * gain * env))
  pcm.writeInt16LE(Math.round(l * 32767), i * 4)
  pcm.writeInt16LE(Math.round(r * 32767), i * 4 + 2)
}

const header = Buffer.alloc(44)
header.write('RIFF', 0)
header.writeUInt32LE(36 + pcm.length, 4)
header.write('WAVE', 8)
header.write('fmt ', 12)
header.writeUInt32LE(16, 16)
header.writeUInt16LE(1, 20) // PCM
header.writeUInt16LE(2, 22) // stereo
header.writeUInt32LE(SR, 24)
header.writeUInt32LE(SR * 4, 28)
header.writeUInt16LE(4, 32)
header.writeUInt16LE(16, 34)
header.write('data', 36)
header.writeUInt32LE(pcm.length, 40)

await mkdir(dirname(outFile), { recursive: true })
await writeFile(outFile, Buffer.concat([header, pcm]))
console.log(`music: ${duration.toFixed(1)}s at ${config.bpm} BPM -> ${outFile}`)
