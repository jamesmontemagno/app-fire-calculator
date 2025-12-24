import { useEffect } from 'react'
import { useLocation } from 'react-router-dom'

interface SEOProps {
  title?: string
  description?: string
  keywords?: string
  canonicalPath?: string
  ogImage?: string
  ogType?: 'website' | 'article'
}

/**
 * SEO component for dynamically updating meta tags per page
 * Updates document title and meta tags based on current route
 */
export default function SEO({
  title = 'FIRE Calculators - Free Financial Independence Calculator',
  description = 'Free FIRE calculators to plan your path to Financial Independence, Retire Early. 100% private, works offline, no tracking.',
  keywords = 'FIRE calculator, financial independence calculator, retire early calculator',
  canonicalPath = '',
  ogImage = 'https://myfirenumber.com/pwa-512x512.png',
  ogType = 'website',
}: SEOProps) {
  const location = useLocation()
  
  useEffect(() => {
    // Update document title
    document.title = title
    
    // Update or create meta tags
    const updateMetaTag = (name: string, content: string, isProperty = false) => {
      const attribute = isProperty ? 'property' : 'name'
      let element = document.querySelector(`meta[${attribute}="${name}"]`) as HTMLMetaElement
      
      if (!element) {
        element = document.createElement('meta')
        element.setAttribute(attribute, name)
        document.head.appendChild(element)
      }
      
      element.setAttribute('content', content)
    }
    
    // Build canonical URL
    const baseUrl = 'https://myfirenumber.com'
    const canonical = `${baseUrl}${canonicalPath || location.pathname}`
    
    // Update standard meta tags
    updateMetaTag('description', description)
    updateMetaTag('keywords', keywords)
    
    // Update Open Graph tags
    updateMetaTag('og:title', title, true)
    updateMetaTag('og:description', description, true)
    updateMetaTag('og:url', canonical, true)
    updateMetaTag('og:image', ogImage, true)
    updateMetaTag('og:type', ogType, true)
    
    // Update Twitter Card tags
    updateMetaTag('twitter:title', title)
    updateMetaTag('twitter:description', description)
    updateMetaTag('twitter:image', ogImage)
    updateMetaTag('twitter:url', canonical)
    
    // Update canonical link
    let canonicalLink = document.querySelector('link[rel="canonical"]') as HTMLLinkElement
    if (!canonicalLink) {
      canonicalLink = document.createElement('link')
      canonicalLink.setAttribute('rel', 'canonical')
      document.head.appendChild(canonicalLink)
    }
    canonicalLink.setAttribute('href', canonical)
    
  }, [title, description, keywords, canonicalPath, ogImage, ogType, location.pathname])
  
  return null
}
