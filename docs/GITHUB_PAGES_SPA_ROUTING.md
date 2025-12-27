# GitHub Pages SPA Routing Solution

## Problem

When users directly access routes like `http://myfirenumber.com/apps` or any other sub-path in this Single Page Application (SPA), GitHub Pages returns a 404 error. This happens because:

1. GitHub Pages serves static files only (no server-side routing)
2. When a user accesses `/apps`, GitHub Pages looks for a file at `/apps/index.html`
3. Since this file doesn't exist, GitHub Pages returns `404.html`
4. React Router never loads, so it can't handle the client-side routing

## Solution Overview

We implement a standard "404 redirect trick" for SPAs on GitHub Pages:

1. **404.html** captures the requested path and redirects to the root with the path encoded
2. **index.html** detects the redirect parameter and restores the original URL
3. React Router then handles the route normally

## Implementation Details

### 404.html (`public/404.html`)

When GitHub Pages can't find a file, it serves this custom 404 page:

```javascript
// Capture the path user requested (e.g., "/apps")
const path = window.location.pathname;
const search = window.location.search;

// Validate path is safe (relative path only)
if (path && path.startsWith('/') && !path.startsWith('//') && !path.includes('://')) {
  // Redirect to root with path encoded in query parameter
  window.location.replace(window.location.origin + '/?p=' + encodeURIComponent(path + search));
} else {
  // Invalid/unsafe path, redirect to home
  window.location.replace(window.location.origin);
}
```

### index.html (root page)

The root page includes a script that runs before React loads:

```javascript
const params = new URLSearchParams(window.location.search);
const redirectPath = params.get('p');

if (redirectPath) {
  // Validate path is safe before restoring
  if (redirectPath.startsWith('/') && !redirectPath.startsWith('//') && !redirectPath.includes('://')) {
    // Remove 'p' parameter
    params.delete('p');
    const newSearch = params.toString();
    const newUrl = redirectPath + (newSearch ? '?' + newSearch : '');
    
    // Restore the original URL without reload
    window.history.replaceState(null, '', newUrl);
  }
}
```

## User Flow Example

1. User clicks link or types: `http://myfirenumber.com/apps`
2. Browser requests: `GET /apps/index.html`
3. GitHub Pages can't find it, returns: `404.html`
4. 404.html script runs:
   - Captures path: `/apps`
   - Redirects to: `/?p=%2Fapps`
5. Browser loads: `http://myfirenumber.com/?p=%2Fapps`
6. index.html loads and script runs:
   - Detects `p` parameter: `%2Fapps` (decodes to `/apps`)
   - Validates path is safe
   - Updates URL to: `http://myfirenumber.com/apps` (no reload)
   - Removes redirect from browser history
7. React Router loads and sees: `/apps`
8. React Router renders the Apps page

## Security Considerations

Both scripts validate redirect paths to prevent security vulnerabilities:

### Validation Rules

✅ **ALLOWED:**
- `/apps` - Standard route
- `/standard?currentAge=30` - Route with query parameters
- `/coast#results` - Route with hash fragment

❌ **BLOCKED:**
- `http://evil.com` - External URL
- `https://evil.com` - External URL with https
- `//evil.com` - Protocol-relative URL
- `javascript:alert(1)` - JavaScript protocol
- `data:text/html,<script>alert(1)</script>` - Data URL
- `file:///etc/passwd` - File protocol

### Why This Is Safe

1. **Path validation**: Both files check `startsWith('/')` and `!startsWith('//')` and `!includes('://')`
2. **No user input**: The path comes from the browser's location, not user input fields
3. **Controlled environment**: GitHub Pages controls the hosting, so host header injection is not a concern
4. **URL encoding**: Paths are properly encoded/decoded to prevent injection attacks

## Preserving Existing Functionality

This solution maintains all existing application features:

- ✅ **URL-based state**: Calculator parameters in URLs continue to work
- ✅ **Browser navigation**: Back/forward buttons work correctly
- ✅ **PWA**: Progressive Web App features unaffected
- ✅ **Offline**: Service worker and offline functionality preserved
- ✅ **Sharing**: URLs with state can still be shared (via direct link now!)

## Testing

To test this solution locally:

```bash
npm run build
npm run preview
```

Then try accessing:
- http://localhost:4173/apps (should load Apps page)
- http://localhost:4173/standard?currentAge=30 (should load with params)

## Alternative Solutions (Not Used)

1. **Hash routing (`#/apps`)**: Changes URL structure, breaks existing shared URLs
2. **Server-side routing**: Requires a backend, not possible on GitHub Pages
3. **Separate HTML files**: Defeats the purpose of an SPA, increases bundle size

## References

- [GitHub Pages SPA routing issue](https://github.com/rafgraph/spa-github-pages)
- [Single Page Apps for GitHub Pages](https://github.com/rafgraph/spa-github-pages)
- [React Router on GitHub Pages](https://create-react-app.dev/docs/deployment/#github-pages)
