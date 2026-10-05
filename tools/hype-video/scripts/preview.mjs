#!/usr/bin/env node
/**
 * Live preview with music, a scrubber, and Space to play/pause. Edit storyboard.mjs or the
 * composition files and refresh the browser tab to see changes.
 *
 * Usage: node scripts/preview.mjs [--port 4321]
 */
import { repoRoot, startStaticServer } from './static-server.mjs'

const index = process.argv.indexOf('--port')
const port = index >= 0 ? Number(process.argv[index + 1]) : 4321
const server = await startStaticServer(repoRoot, { port })
console.log(`Preview: ${server.url}/tools/hype-video/composition/index.html?preview`)
console.log('Jump to a time with &t=12.5. Press Ctrl+C to stop.')
