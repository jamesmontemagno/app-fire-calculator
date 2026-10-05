import { createServer } from 'node:http'
import { readFile, stat } from 'node:fs/promises'
import { extname, join, normalize, resolve, sep } from 'node:path'
import { fileURLToPath } from 'node:url'

export const toolDir = resolve(fileURLToPath(new URL('..', import.meta.url)))
export const repoRoot = resolve(toolDir, '..', '..')

const types = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.mjs': 'text/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.json': 'application/json',
  '.webmanifest': 'application/manifest+json',
  '.svg': 'image/svg+xml',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
  '.ico': 'image/x-icon',
  '.woff': 'font/woff',
  '.woff2': 'font/woff2',
  '.wav': 'audio/wav',
  '.mp4': 'video/mp4',
}

async function isFile(path) {
  try {
    return (await stat(path)).isFile()
  } catch {
    return false
  }
}

/**
 * Minimal localhost-only static server. `spa` serves index.html for unknown routes so React
 * Router deep links work against a production build.
 */
export function startStaticServer(root, { port = 0, spa = false } = {}) {
  const base = resolve(root)
  const server = createServer(async (req, res) => {
    try {
      const url = new URL(req.url ?? '/', 'http://localhost')
      const relative = normalize(decodeURIComponent(url.pathname)).replace(/^([/\\])+/, '')
      let file = join(base, relative)
      if (file !== base && !file.startsWith(base + sep)) {
        res.writeHead(403).end()
        return
      }
      if (!(await isFile(file)) && (await isFile(join(file, 'index.html')))) file = join(file, 'index.html')
      if (!(await isFile(file))) {
        if (!spa || extname(relative)) {
          res.writeHead(404).end('Not found')
          return
        }
        file = join(base, 'index.html')
      }
      const body = await readFile(file)
      res.writeHead(200, {
        'Content-Type': types[extname(file).toLowerCase()] ?? 'application/octet-stream',
        'Cache-Control': 'no-store',
      })
      res.end(body)
    } catch (error) {
      res.writeHead(500).end(String(error))
    }
  })
  return new Promise((resolvePromise, reject) => {
    server.once('error', reject)
    server.listen(port, '127.0.0.1', () => {
      const { port: actual } = server.address()
      resolvePromise({ server, url: `http://127.0.0.1:${actual}`, close: () => new Promise((r) => server.close(r)) })
    })
  })
}
