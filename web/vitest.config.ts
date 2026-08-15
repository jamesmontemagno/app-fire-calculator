import { defineConfig } from 'vitest/config'

/**
 * Standalone from `vite.config.ts` on purpose.
 *
 * The app config instantiates the React and PWA plugins, neither of which the calculation suite
 * needs — these tests are pure functions plus `react-dom/server` string rendering, so they run in
 * the `node` environment with no DOM and no service-worker generation.
 *
 * Test files live under `src/` so the existing `tsconfig.json` (`include: ["src"]`) type-checks
 * them during `npm run build`. That is deliberate: several calculator entry points take long
 * positional argument lists, so a miswired call is caught by `tsc` before it can produce a
 * confidently wrong number.
 */
export default defineConfig({
  test: {
    environment: 'node',
    include: ['src/**/*.test.ts', 'src/**/*.test.tsx'],
    /**
     * `css: true` so `designInvariants.test.ts` can read the stylesheet it guards.
     *
     * Vitest defaults this to `false`, which stubs CSS modules to an empty string — and it does so
     * even when the import carries `?raw`. Measured here: `import './index.css?raw'` yields 0 bytes
     * under the default and 6306 under `css: true`, against 6306 on disk. Left at the default, the
     * design guard would have reported the token file clean without reading a byte of it, which is
     * the silent-pass failure that suite exists to prevent. No test imports CSS for its styles, so
     * this only affects whether the bytes are readable.
     */
    css: true,
  },
})
