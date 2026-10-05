#!/usr/bin/env node
/**
 * Captures fresh screenshots of the web app for the hype video.
 *
 * Builds web/ (unless a build already exists and --build is not passed), serves web/dist on
 * localhost, and screenshots every entry in `webCaptures` from storyboard.mjs into
 * tools/hype-video/assets/web/<name>.png. Nothing leaves the machine.
 *
 * Usage: node scripts/capture-web.mjs [--build] [--only name1,name2]
 */
import { spawnSync } from 'node:child_process'
import { existsSync } from 'node:fs'
import { mkdir } from 'node:fs/promises'
import { join } from 'node:path'
import { chromium } from 'playwright'
import { webCaptures } from '../storyboard.mjs'
import { repoRoot, startStaticServer, toolDir } from './static-server.mjs'

const args = process.argv.slice(2)
const forceBuild = args.includes('--build')
const onlyIndex = args.indexOf('--only')
const only = onlyIndex >= 0 ? new Set(args[onlyIndex + 1].split(',')) : null

const webDir = join(repoRoot, 'web')
const distDir = join(webDir, 'dist')
const outDir = join(toolDir, 'assets', 'web')

const viewports = {
  desktop: { width: 1440, height: 900, isMobile: false, hasTouch: false },
  phone: { width: 390, height: 844, isMobile: true, hasTouch: true },
}

function run(command, commandArgs, cwd) {
  console.log(`> ${command} ${commandArgs.join(' ')}  (in ${cwd})`)
  const result = spawnSync(command, commandArgs, { cwd, stdio: 'inherit', shell: process.platform === 'win32' })
  if (result.status !== 0) throw new Error(`${command} ${commandArgs.join(' ')} failed`)
}

if (forceBuild || !existsSync(join(distDir, 'index.html'))) {
  if (!existsSync(join(webDir, 'node_modules'))) run('npm', ['ci'], webDir)
  run('npm', ['run', 'build'], webDir)
}

await mkdir(outDir, { recursive: true })
const site = await startStaticServer(distDir, { spa: true })
const browser = await chromium.launch()

try {
  for (const capture of webCaptures) {
    if (only && !only.has(capture.name)) continue
    const viewport = viewports[capture.viewport ?? 'desktop']
    const context = await browser.newContext({
      viewport: { width: viewport.width, height: viewport.height },
      deviceScaleFactor: 2,
      isMobile: viewport.isMobile,
      hasTouch: viewport.hasTouch,
      colorScheme: capture.theme === 'dark' ? 'dark' : 'light',
      reducedMotion: 'reduce',
      serviceWorkers: 'block',
    })
    await context.addInitScript((theme) => {
      try {
        window.localStorage.setItem('fire-calc-theme', theme)
      } catch {
        // Storage can be unavailable on about:blank; the app falls back to the system theme.
      }
    }, capture.theme ?? 'light')

    const page = await context.newPage()
    await page.goto(site.url + capture.path, { waitUntil: 'networkidle' })
    await page.evaluate(() => document.fonts.ready)
    if (capture.scrollTo) {
      await page.locator(capture.scrollTo).first().evaluate((el) => {
        window.scrollTo(0, el.getBoundingClientRect().top + window.scrollY - 96)
      })
    } else if (capture.scrollY) {
      await page.evaluate((y) => window.scrollTo(0, y), capture.scrollY)
    }
    // Let charts finish their (reduced-motion) layout before capturing.
    await page.waitForTimeout(1200)
    const file = join(outDir, `${capture.name}.png`)
    await page.screenshot({ path: file })
    console.log(`captured ${capture.name} -> ${file}`)
    await context.close()
  }
} finally {
  await browser.close()
  await site.close()
}
