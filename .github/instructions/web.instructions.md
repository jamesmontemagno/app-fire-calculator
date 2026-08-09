---
applyTo: "web/**,.github/workflows/deploy.yml"
---

# Web App Instructions

## Stack and Commands

The web app is a React 19, TypeScript, Vite, Tailwind CSS v4, React Router v7, Recharts, and vite-plugin-pwa application.

Run commands from `web/`:

```bash
npm run dev
npm run build
npm run preview
```

Use `npm run build` for validation. It runs the TypeScript compiler before the Vite production build.

## State and Privacy

- Calculator state is synchronized with URL query parameters through `src/hooks/useCalculatorParams.ts` and `src/hooks/useDeferredCompensationParams.ts`.
- Browser-local persistence is intentional and uses namespaced localStorage keys. URL parameters take precedence when a shared calculation is opened.
- Keep all calculator state serializable. Do not put functions, class instances, or other complex runtime objects in URL or persisted state.
- Use batched parameter updates for presets and debounced updates for sliders or other high-frequency controls.
- Never send calculator values to a server or third-party service.
- localStorage is limited to calculator inputs and UI preferences. Do not add tracking identifiers or analytics.

When adding or changing inputs, update the parameter type, defaults, URL keys, parsing/validation, persistence, reset behavior, and any relevant presets together.

## Calculations

- Keep financial calculations as pure functions in `src/utils/calculations.ts` or another focused utility module.
- Calculation functions must not depend on React, browser APIs, or component state.
- Pages should derive results with `useMemo` when calculations or projection arrays are nontrivial.
- Use the shared `formatCurrency()` and `formatPercent()` helpers.
- Real return is `(1 + nominalReturn) / (1 + inflationRate) - 1`.
- Standard FIRE number is `annualExpenses / withdrawalRate`.

## Project Structure

- `src/pages/`: route-level calculator pages.
- `src/components/inputs/`: reusable controlled financial inputs.
- `src/components/charts/`: Recharts projection and breakdown charts.
- `src/components/ui/`: cards, actions, presets, results, tooltips, and disclaimers.
- `src/components/layout/`: application shell and navigation.
- `src/config/`: calculator and SEO metadata.
- `src/hooks/`: URL and persisted state coordination.
- `src/utils/`: pure calculations, exports, and storage helpers.

For a new calculator:

1. Add pure calculation logic and types.
2. Add URL/persisted parameter support.
3. Create the page using existing input, result, chart, and disclaimer components.
4. Add calculator metadata and its route in `src/main.tsx`.
5. Verify direct navigation, sharing, reset, browser back/forward, mobile layout, and dark mode.

## React and TypeScript

- Keep strict TypeScript types; do not use `any` or unsafe casts to bypass validation.
- Reuse existing components and helpers before creating new abstractions.
- Use controlled inputs and preserve accessible labels, descriptions, and keyboard behavior.
- React Router uses `createBrowserRouter`; do not replace it with `BrowserRouter`.
- Preserve the router `basename` derived from `import.meta.env.BASE_URL`.

## Styling and Accessibility

- Use Tailwind CSS v4 utilities and existing design tokens; do not add CSS modules or styled-components.
- Include appropriate `dark:` variants for new UI.
- Follow the existing mobile-first breakpoints and navigation behavior.
- Use the accessible Tooltip and InputGroup patterns for contextual help.
- Provide semantic headings, labels, ARIA descriptions, and keyboard access.
- Respect `prefers-reduced-motion`.

## PWA and Deployment

- PWA configuration lives in `vite.config.ts`.
- Keep the custom GitHub Pages SPA fallback in `public/404.html` compatible with `index.html`.
- The deployment workflow installs and builds from `web/` and uploads `web/dist`.
- Preserve offline behavior and update-prompt behavior when changing caching or service-worker settings.
- Ensure newly required static asset types are covered by the Workbox glob patterns.
