# Plan: Review & Improvements for FIRE Calculator App

**TL;DR**: The app is well-structured with good component architecture, but has gaps in accessibility, input validation, type safety, and privacy claim accuracy. Priority improvements include fixing TypeScript errors, correcting misleading privacy claims about localStorage usage, adding input validation, and enhancing keyboard/screen reader accessibility.

## Steps

1. **Fix critical issues first**
   - Resolve TypeScript type import error in DebtItem component
   - Correct privacy claims in README.md and Home.tsx regarding localStorage usage

2. **Improve input robustness**
   - Add validation layer for calculation inputs (NaN, negative numbers, edge cases)
   - Add visual feedback for min/max input constraints in InputGroup.tsx
   - Add debouncing for URL parameter updates to prevent excessive rewrites

3. **Enhance accessibility**
   - Add keyboard navigation and ARIA labels to tooltips and progress components
   - Add `prefers-reduced-motion` media query support for sidebar animations
   - Link form helper text to inputs via `aria-describedby`
   - Add milestone text labels (not hover-dependent) to ProgressToFIRE.tsx

4. **Refactor calculations & documentation**
   - Extract calculation logic from monolithic calculations.ts (600+ lines) into smaller, testable units
   - Add JSDoc comments with mathematical explanations and input constraints to calculation functions
   - Extract calculator metadata (names, colors, descriptions) into centralized config


## Further Considerations

1. **Stabilize state across navigation** – Should monthly/annual toggle state persist via URL parameters, or accept loss on navigation? Recommend URL-persistence for consistency with other settings.


3. **Testing strategy** – Given complex financial math, should we add unit tests for calculation edge cases, or is manual testing sufficient for now?

## Detailed Issues Found

### Code Quality Issues

1. **Type Import Error (Critical)**
   - DebtItem must use type-only import due to `verbatimModuleSyntax` in tsconfig
   - **Impact**: Build fails with strict TypeScript

2. **LocalStorage Contradiction**
   - README and Home page claim "No localStorage" but the app uses it for theme and sidebar state
   - **Impact**: False privacy claims; misleading users about data handling

3. **Missing Input Validation**
   - CurrencyInput and PercentageInput don't validate edge cases (e.g., NaN, negative numbers when min=0)
   - **Impact**: Potential for invalid calculations if users manually edit fields

4. **Unsafe DOM Access**
   - AppLayout accesses document without SSR check (though mitigated by useEffect)
   - UpdatePrompt doesn't handle service worker registration failures gracefully

5. **Magic Numbers Without Constants**
   - calculations.ts has hardcoded values; 4% rule, 600 month max debt, 50 year projections aren't configurable
   - **Impact**: Harder to adjust thresholds; inconsistent with app's educational purpose

### Accessibility Concerns

1. **Tooltip Keyboard Accessibility**
   - Tooltips use hover-only triggers; not keyboard accessible
   - **Fix**: Add keyboard support with focus states and ARIA attributes

2. **Missing ARIA Labels**
   - Sidebar toggle buttons lack `aria-expanded` attribute (should reflect state)
   - ProgressToFIRE progress bar lacks `role="progressbar"` and aria-valuenow

3. **Color-Dependent Information**
   - Status messages rely on color to convey milestones (25%, 50%, 75%); milestone labels appear only on hover
   - **Impact**: Colorblind users miss milestone information

4. **Form Labels and Input Matching**
   - InputGroup properly uses labels, but helper text isn't associated with input via `aria-describedby`

5. **Sidebar Animation Accessibility**
   - Sidebar transforms without `prefers-reduced-motion` check

### UX/UI Improvements

1. **Confusing Privacy Claims**
   - README states "No localStorage" and "Nothing stored in your browser" but app stores theme and sidebar state
   - **Recommendation**: Update copy to "User preferences (theme, layout) stored locally; no financial data stored"

2. **Monthly/Annual Toggle State Loss**
   - Toggle state doesn't persist across navigation/page refresh
   - **Impact**: Users toggle to monthly, navigate away, toggle back—state is lost

3. **Unguided Error States**
   - No error handling for invalid calculation inputs (e.g., 0% return rate, negative years result)
   - ProgressToFIRE displays ">999%" progress but doesn't explain why or suggest next steps

4. **DebtListInput Unused Component**
   - Component is imported in index but not used in any calculator pages
   - **Impact**: Dead code; misleads about feature completeness

5. **Missing Input Constraints UI Feedback**
   - Min/max bounds not visually indicated; users don't know input limits until they hit them
   - No visual distinction for disabled/readonly states

6. **Quick Presets Not Explained**
   - QuickPresets component references unclear purposes

### Architecture & Design Pattern Issues

1. **URL State Management Fragility**
   - useCalculatorParams uses string concatenation for URL building
   - **Risk**: If param keys change, old bookmarked URLs break silently
   - **Better approach**: Use URL schema versioning or mapping layer

2. **Inconsistent Calculator Page Structure**
   - Each calculator page (StandardFIRE, CoastFIRE, etc.) likely duplicates input/output patterns
   - **Recommendation**: Extract common layout into reusable component factory or layout compound

3. **Calculation Logic Lacks Encapsulation**
   - calculations.ts is 600+ lines with mixed concerns (projections, debt payoff, multiple calculator types)
   - **Better approach**: Split by calculator type or create calculator class factory

4. **No Input Sanitization**
   - CurrencyInput removes non-numeric chars but doesn't validate against expected precision
   - Example: `1.999999999` will be accepted without rounding guidance

5. **Theme Context Uses Incorrect Pattern**
   - ThemeContext exposes both isDark and theme; confusing for consumers
   - Better: Single hook returning resolved value only, with internal theme preference tracking

### Performance Optimization Opportunities

1. **Unnecessary Recalculations**
   - useCalculatorParams recalculates entire projection on every param change
   - **Optimization**: Memoize projection generation separately from aggregated results

2. **Large Array Generation**
   - calculations.ts creates array even when not displayed; uncapped at 50 years
   - **Risk**: Very large projections (100+ years) not handled; could freeze UI

3. **Chart Re-renders Not Optimized**
   - Charts likely re-render on all param changes
   - **Recommendation**: Wrap with React.memo and memoize data transformations

4. **Debouncing Missing**
   - Input changes trigger immediate URL updates; no debounce for high-frequency changes
   - **Impact**: Excessive URL rewrites; potential browser history pollution

5. **Unused Dependencies**
   - `@fontsource/inter` imported but Tailwind v4 handles fonts via config
   - Could be removed to reduce bundle size

### Testing & Documentation Gaps

1. **No Test Coverage Visible**
   - Calculation logic (especially debt payoff and projection edge cases) has no tests
   - **Risk**: Refactoring breaks silent; complex financial math prone to regressions

2. **Missing Calculation Documentation**
   - calculations.ts uses closed-form mathematical solutions without comments explaining formula
   - Hard to review for correctness; no reference citations (Trinity Study mentioned in README but not linked)

3. **Component Prop Documentation Incomplete**
   - Components lack JSDoc comments explaining expected input ranges
   - Example: InputGroup doesn't document that some fields must be > 0

4. **No Calculation Validation Helpers**
   - No functions to validate inputs before passing to calculators
   - Defensive checks embedded in calculation functions instead

### Best Practice Violations

1. **Hardcoded UI Strings**
   - Calculator names, descriptions, colors defined in multiple places (Home.tsx, Sidebar.tsx)
   - **Better**: Centralized calculator registry/config

2. **Tooltip Position Unresponsive**
   - Fixed positioning in tooltip; breaks near screen edges
   - Should use CSS `anchor()` or dynamic positioning library

3. **Inconsistent Error Handling Pattern**
   - Some calculations guard against NaN, others don't
   - Functions return undefined but consumers handle inconsistently

4. **Missing i18n Structure**
   - All strings hardcoded in English; no i18n setup
   - **Risk**: Hard to localize later; hidden assumption about target market

5. **No Environment Configuration**
   - Withdrawal rate, inflation, return defaults hardcoded
   - Should be configurable per deployment/region

### Security & Privacy Edge Cases

1. **URL-Based Data Sharing Risk**
   - All calculator inputs visible in URL; if user shares link, their financial data is in browser history/links
   - **Mitigation**: Add warning on copy-to-clipboard about data visibility; suggest using private browsing

2. **No CSRF Protection Needed** (Good!)
   - Purely client-side; no backend to exploit

3. **Service Worker Update Flow**
   - Silently updates on user request; no changelog link
   - Users don't know what changed

### Missing Features

1. **No Scenario Comparison**
   - Can't easily compare "If I retire at 55 vs. 60" side-by-side
   - Would improve decision-making UX

2. **No Data Export (CSV/PDF)**
   - Projections can't be downloaded for spreadsheet analysis or sharing with advisors

3. **No Preset Templates for Common Scenarios**
   - "Average Tech Worker", "Teacher", "Entrepreneur" scenarios could accelerate onboarding

4. **Disclaimer Too Generic**
   - Footer disclaimer doesn't mention geographic limitations (US-centric calculators)
   - Should clarify: "Healthcare Gap is US-specific", "Tax implications not considered", etc.

## Priority Recommendation Matrix

| Issue | Severity | Effort | Priority |
|-------|----------|--------|----------|
| LocalStorage privacy claim contradiction | High | Low | 🔴 Critical |
| DebtItem type import error | Critical | Trivial | 🔴 Critical |
| Input validation missing | Medium | Medium | 🟡 High |
| Tooltip accessibility | Medium | Medium | 🟡 High |
| Monthly toggle state loss | Low | Low | 🟡 Medium |
| Calculation documentation | Medium | Low | 🟡 High |
| Debounce input changes | Low | Medium | 🟢 Low |
