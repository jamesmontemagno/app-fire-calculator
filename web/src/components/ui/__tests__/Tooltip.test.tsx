import { renderToStaticMarkup } from 'react-dom/server'
import { describe, expect, it } from 'vitest'
import Tooltip from '../Tooltip'

describe('Tooltip', () => {
  it('uses theme-aware colors for its background, text, and arrow', () => {
    const markup = renderToStaticMarkup(<Tooltip content="Investments already available for retirement." />)

    expect(markup).toContain('bg-surface-raised text-content')
    expect(markup).toContain('border-t-surface-raised')
    expect(markup).not.toContain('bg-content')
    expect(markup).not.toContain('text-white')
    expect(markup).toContain('role="tooltip"')
    expect(markup).toContain('Investments already available for retirement.')
  })
})
