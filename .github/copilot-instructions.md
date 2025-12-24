# FIRE Calculator - AI Coding Instructions

## Project Overview

This is a **privacy-first, offline-capable** FIRE (Financial Independence Retire Early) calculator built as a Progressive Web App. All calculations run client-side with zero tracking, storage, or backend dependencies.

**Tech Stack:** React 19, TypeScript, Vite, Tailwind CSS v4, React Router v7, Recharts, vite-plugin-pwa

**Key Principle:** Never persist data locally—all calculator state lives in URL query parameters.

## Architecture & Data Flow

### URL-Based State Management

The entire application state is stored in URL query parameters via [useCalculatorParams.ts](src/hooks/useCalculatorParams.ts). This is fundamental to the architecture:

- All calculator inputs sync to/from URL via `useSearchParams`
- No calculator data in localStorage—only UI preferences (theme, sidebar state)
- URLs are the "database" for all financial calculations
- Users share calculations by sharing URLs (privacy-preserving)
- Browser back/forward navigation works naturally
- Pattern: `setParam('currentAge', 30)` → URL updates → component re-renders
- Use `setParamDebounced()` for high-frequency updates (sliders) to prevent excessive URL rewrites

When adding new inputs, add them to `CalculatorParams` interface and `DEFAULTS`/`PARAM_KEYS` in [useCalculatorParams.ts](src/hooks/useCalculatorParams.ts).

### Pure Calculation Functions

All financial calculations live in [calculations.ts](src/utils/calculations.ts) as pure functions:

- Input: plain objects with calculator parameters
- Output: result objects with calculated values + projection arrays
- No side effects, no React dependencies
- Functions like `calculateStandardFIRE()`, `calculateCoastFIRE()`, `futureValue()` are reusable across calculators

Each calculator page (e.g., [StandardFIRE.tsx](src/pages/StandardFIRE.tsx)) follows this pattern:

```tsx
const results = useMemo(() => {
  return calculateStandardFIRE({...params})
}, [params])
```

### Component Structure

- **Pages** ([src/pages/](src/pages/)): Each calculator is a standalone page/route
- **Inputs** ([src/components/inputs/](src/components/inputs/)): Reusable form inputs (CurrencyInput, PercentageInput, AgeInput)
- **Charts** ([src/components/charts/](src/components/charts/)): Recharts wrappers for projections
- **UI** ([src/components/ui/](src/components/ui/)): Cards, buttons, progress bars, disclaimers, accessible Tooltip
- **Layout** ([src/components/layout/](src/components/layout/)): AppLayout with sidebar navigation
- **Config** ([src/config/](src/config/)): Centralized calculator metadata (names, icons, colors, descriptions)

All input components follow [InputGroup.tsx](src/components/inputs/InputGroup.tsx) pattern with tooltips, labels, controlled inputs, and optional helper text linked via `aria-describedby`.

## Key Conventions

### Styling

- **Tailwind CSS v4** is used throughout—no CSS modules or styled-components
- Dark mode via `dark:` prefix—toggled by [ThemeContext.tsx](src/context/ThemeContext.tsx)
- System preference detection + manual override stored in localStorage (`fire-calc-theme`)
- Use design tokens: `text-fire-500`, `bg-gray-50 dark:bg-gray-800`, etc.
- Components use conditional classes for dark mode: `bg-white dark:bg-gray-800`

### Financial Calculations

- All percentages stored as decimals (0.07 = 7%)
- Currency values in dollars (no cents precision for UI)
- Use `formatCurrency()` and `formatPercent()` helpers from [calculations.ts](src/utils/calculations.ts)
- Real return = `(1 + nominal) / (1 + inflation) - 1`
- Standard FIRE formula: `fireNumber = annualExpenses / withdrawalRate`

### Progressive Web App (PWA)

Configured in [vite.config.ts](vite.config.ts) with `vite-plugin-pwa`:

- Service worker caches all assets (`**/*.{js,css,html,ico,png,svg,woff2}`)
- `registerType: 'prompt'` shows update prompt via [UpdatePrompt.tsx](src/components/UpdatePrompt.tsx)
- Manifest defines app name, icons, theme color (`#f97316` - orange)
- Works fully offline after first load

## Development Workflow

```bash
npm run dev      # Start Vite dev server (port 5173)
npm run build    # TypeScript check + Vite build
npm run preview  # Preview production build
```

- **No backend:** All logic runs in browser
- **No test suite:** Project relies on TypeScript for safety
- **No CI/CD config:** Static site deployed to GitHub Pages

## Adding New Calculators

Follow the pattern from existing pages:

1. Create page component in [src/pages/](src/pages/) (e.g., `NewCalculator.tsx`)
2. Add calculation function to [calculations.ts](src/utils/calculations.ts) with JSDoc comments
3. Add calculator metadata to [calculators.ts](src/config/calculators.ts)
4. Add route to [main.tsx](src/main.tsx) router config
5. Use `useCalculatorParams()` for state management
6. Wrap results in `useMemo()` for performance
7. Use existing input/chart/UI components for consistency

Example structure: inputs on left, results + charts on right (responsive grid).

## Component Patterns

### QuickPresets

[QuickPresets.tsx](src/components/ui/QuickPresets.tsx) provides one-click scenario loading:

- Uses `setParams()` to batch-update all inputs
- Presets include Conservative, Moderate, Aggressive, Fat FIRE
- Can be customized per-calculator by passing custom `presets` prop

### Tooltip

[Tooltip.tsx](src/components/ui/Tooltip.tsx) provides accessible help text:

- Keyboard accessible (focus/blur support)
- Proper ARIA attributes (`role="tooltip"`, `aria-describedby`)
- Visible on hover and focus for screen reader compatibility
- Used throughout all input components for contextual help

### URL Actions

[UrlActions.tsx](src/components/ui/UrlActions.tsx) provides reset/copy functionality:

- Reset clears all URL params (returns to defaults)
- Copy uses Clipboard API to copy current URL

### Responsive Design

- Mobile-first approach with `sm:`, `md:`, `lg:` breakpoints
- Grid layouts collapse to single column on mobile
- Sidebar converts to bottom navigation on mobile

## Gotchas & Common Issues

- **Don't use localStorage** for calculator data—only for UI preferences (theme, sidebar)
- **All calculator state must be serializable to URL**—no complex objects
- **Percentages are decimals**—UI shows "7%" but store as `0.07`
- **Use debouncing** for high-frequency inputs—call `setParamDebounced()` for sliders
- **Input validation**—CurrencyInput and PercentageInput handle NaN, negatives, edge cases
- **React Router v7** uses data router pattern—use `createBrowserRouter`, not `BrowserRouter`
- **Tailwind v4** has breaking changes from v3—check docs for migration
- **Dark mode requires both class and CSS**—ensure `dark:` variants are present
- **Accessibility first**—use Tooltip component, add ARIA labels, support keyboard navigation

## External Dependencies

- **Recharts** for all charting ([ProjectionChart.tsx](src/components/charts/ProjectionChart.tsx), etc.)
- **React Router v7** for routing + search params
- **@fontsource/inter** for typography
- **vite-plugin-pwa** for offline support

## Privacy & Security

- **Zero tracking:** No analytics, no cookies for tracking
- **UI preferences only:** localStorage used exclusively for theme and sidebar state—never financial data
- **No server calls:** All computation client-side
- **URL state is intentional:** Users must consciously share URLs with financial data
- Disclaimer component required on all calculator pages

## Accessibility

- All tooltips are keyboard accessible with proper ARIA attributes
- Interactive components (sidebar toggle, progress bars) have ARIA labels
- Animations respect `prefers-reduced-motion` media query
- Form inputs support optional helper text linked via `aria-describedby`
- Tooltips work with hover, focus, and keyboard navigation
