/**
 * Chart colours have to be resolved to concrete values because Recharts takes
 * them as props rather than classes, so they cannot ride the `dark` variant
 * like the rest of the UI. Keeping them in one place is what stops the charts
 * drifting away from the token layer in index.css - these values are the same
 * ones the palette contrast guard checks.
 */
export interface ChartTheme {
  grid: string
  axisText: string
  axisLine: string
  tooltipBg: string
  tooltipBorder: string
  /** Primary data series. The single app accent. */
  primary: string
  /** Second series when a chart genuinely compares two things. */
  secondary: string
  positive: string
  negative: string
  neutral: string
  /** Reference lines, targets and milestone markers. */
  reference: string
}

const light: ChartTheme = {
  grid: '#e3e1df',
  axisText: '#66635e',
  axisLine: '#837f7a',
  tooltipBg: '#ffffff',
  tooltipBorder: '#e3e1df',
  primary: '#b54100',
  secondary: '#1f6cb0',
  positive: '#1d7d3e',
  negative: '#ba2b2e',
  neutral: '#706d68',
  reference: '#66635e',
}

const dark: ChartTheme = {
  grid: '#2d2a28',
  axisText: '#ada8a3',
  axisLine: '#6f6b66',
  tooltipBg: '#1a1816',
  tooltipBorder: '#2d2a28',
  primary: '#fd6e2d',
  secondary: '#70b3f7',
  positive: '#67d283',
  negative: '#f97770',
  neutral: '#94908b',
  reference: '#ada8a3',
}

export function chartTheme(isDark: boolean): ChartTheme {
  return isDark ? dark : light
}

/**
 * Categorical series for charts that must show several parts at once, such as
 * a debt breakdown. Drawn from the same harmonised set as the calculator icon
 * tints, at a fixed lightness and chroma so no one slice shouts.
 */
export const CATEGORICAL_LIGHT = [
  '#b54100', '#1f6cb0', '#1d7d3e', '#8a5a00', '#7a5eb6', '#0f7490', '#a94c7d', '#4f6f1f',
]

export const CATEGORICAL_DARK = [
  '#fd6e2d', '#70b3f7', '#67d283', '#f5b75b', '#b8a0f7', '#5cc7e0', '#ec8fbc', '#a8cf6a',
]

export function categorical(isDark: boolean): string[] {
  return isDark ? CATEGORICAL_DARK : CATEGORICAL_LIGHT
}
