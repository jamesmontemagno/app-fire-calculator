import { renderToStaticMarkup } from 'react-dom/server'
import { describe, expect, it } from 'vitest'

import MobileAppPromo from '../MobileAppPromo'

describe('app downloads', () => {
  it('links all four platforms to their store listings with accessible names', () => {
    const html = renderToStaticMarkup(<MobileAppPromo />)
    const links = html.match(/<a\b[^>]*>[\s\S]*?<\/a>/g) ?? []

    expect(links).toHaveLength(4)
    const expectedDownloads = [
      ['iPhone &amp; iPad', 'https://apps.apple.com/us/app/my-fire-number/id6799772608', 'app-store.svg'],
      ['Mac', 'https://apps.apple.com/us/app/my-fire-number/id6799772608', 'mac-app-store.svg'],
      ['Android', 'https://play.google.com/store/apps/details?id=com.refractored.myfirenumber', 'google-play.svg'],
      ['Windows', 'https://apps.microsoft.com/store/detail/9NP4DM6PKP8K', 'microsoft-store.svg'],
    ]

    expectedDownloads.forEach(([platform, url, badge], index) => {
      expect(links[index]).toContain(`href="${url}"`)
      expect(links[index]).toContain(`aria-label="Download My Fire Number for ${platform} (opens in a new tab)"`)
      expect(links[index]).toContain(`src="${import.meta.env.BASE_URL}store-badges/${badge}"`)
      expect(links[index]).toContain('target="_blank"')
      expect(links[index]).toContain('rel="noopener noreferrer"')
    })
  })

  it('uses local badge images rather than third-party images or scripts', () => {
    const html = renderToStaticMarkup(<MobileAppPromo />)

    expect(html).not.toMatch(/<img[^>]+src="https?:/)
    expect(html).not.toContain('<script')
    expect(html).toContain('flex-wrap')
    expect(html.match(/class="h-12 w-auto"/g)).toHaveLength(4)
  })

  it('preserves custom promotional copy and styling', () => {
    const html = renderToStaticMarkup(
      <MobileAppPromo title="Official app" description="Plan anywhere." className="mt-8" />,
    )

    expect(html).toContain('Official app')
    expect(html).toContain('Plan anywhere.')
    expect(html).toContain('mt-8')
  })
})
