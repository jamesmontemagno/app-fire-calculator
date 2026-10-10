import { renderToStaticMarkup } from 'react-dom/server'
import { describe, expect, it } from 'vitest'
import Tooltip from '../Tooltip'

describe('Tooltip', () => {
  it('pairs its inverted background with theme-aware text and a matching arrow', () => {
    const markup = renderToStaticMarkup(<Tooltip content="Investments already available for retirement." />)

    expect(markup).toContain('bg-content text-surface-raised')
    expect(markup).toContain('border-t-content')
    expect(markup).not.toContain('text-white')
    expect(markup).toContain('role="tooltip"')
    expect(markup).toContain('Investments already available for retirement.')
  })
})
