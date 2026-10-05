#!/usr/bin/env node
/**
 * Fast sanity check for storyboard.mjs: scene types, icons, image paths, and web captures.
 * Run after every storyboard edit: `npm run check`.
 */
import { existsSync } from 'node:fs'
import { join } from 'node:path'
import { iconNames } from '../composition/icons.js'
import { builders } from '../composition/scenes.js'
import { config, scenes, timeline, totalSeconds, webCaptures } from '../storyboard.mjs'
import { repoRoot, toolDir } from './static-server.mjs'

const problems = []
const warnings = []
const captureNames = new Set(webCaptures.map((c) => c.name))

function checkImage(src, where) {
  if (src.startsWith('web:')) {
    const name = src.slice(4)
    if (!captureNames.has(name)) problems.push(`${where}: "${src}" is not listed in webCaptures`)
    else if (!existsSync(join(toolDir, 'assets', 'web', `${name}.png`))) warnings.push(`${where}: "${src}" not captured yet (npm run capture)`)
  } else if (!existsSync(join(repoRoot, src))) {
    problems.push(`${where}: image not found at ${src}`)
  }
}

function checkIcon(name, where) {
  if (!iconNames.includes(name)) problems.push(`${where}: unknown icon "${name}" (known: ${iconNames.join(', ')})`)
}

for (const [i, scene] of scenes.entries()) {
  const where = `scene ${i + 1} (${scene.type})`
  if (!builders[scene.type]) problems.push(`${where}: unknown type`)
  if (!Number.isInteger(scene.bars) || scene.bars < 1) problems.push(`${where}: bars must be a positive integer`)
  if (!['intro', 'build', 'drop', 'groove', 'break', 'outro'].includes(scene.energy)) problems.push(`${where}: unknown energy "${scene.energy}"`)
  if (scene.logo) checkImage(scene.logo, where)
  if (scene.shot) checkImage(scene.shot.src, where)
  for (const shot of scene.shots ?? []) {
    checkImage(shot.src, where)
    if (!['browser', 'phone-web', 'store'].includes(shot.frame)) problems.push(`${where}: unknown frame "${shot.frame}"`)
  }
  for (const word of scene.words ?? []) checkIcon(word.icon, where)
  for (const platform of scene.platforms ?? []) checkIcon(platform.icon, where)
  if (scene.type === 'pillars' && scene.words.length * (scene.beatsPerWord ?? 2) > scene.bars * config.beatsPerBar - 4) {
    warnings.push(`${where}: words use almost all ${scene.bars} bars; add a bar so the summary grid has time on screen`)
  }
}
if (scenes.at(-1)?.type !== 'outro') warnings.push('the last scene is not an outro; the video should end on availability and the URL')

for (const scene of timeline()) {
  console.log(`${scene.start.toFixed(1).padStart(5)}s  ${String(scene.bars).padStart(2)} bars  ${scene.energy.padEnd(6)}  ${scene.type}`)
}
console.log(`total ${totalSeconds().toFixed(1)}s at ${config.bpm} BPM`)
for (const warning of warnings) console.warn(`warning: ${warning}`)
if (problems.length) {
  for (const problem of problems) console.error(`error: ${problem}`)
  process.exit(1)
}
console.log('storyboard OK')
