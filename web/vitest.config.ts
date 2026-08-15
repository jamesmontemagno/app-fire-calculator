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
  },
})
