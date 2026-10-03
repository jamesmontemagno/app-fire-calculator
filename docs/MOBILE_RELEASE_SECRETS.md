# App Release Secrets

Pull request validation does not require repository secrets. The Android and iOS workflows only
read signing credentials when a manual workflow run enables **Build release**. GitHub Pages uses
GitHub's built-in token and does not require a custom secret.

Add release credentials under **Repository Settings > Secrets and variables > Actions**.

## Android

| Secret | Value |
| --- | --- |
| `ANDROID_KEYSTORE` | Base64-encoded Android `.keystore` or `.jks` file |
| `ANDROID_KEYSTORE_PASSWORD` | Password protecting the keystore and signing key |
| `ANDROID_KEY_ALIAS` | Alias of the signing key inside the keystore |

Encode and upload an existing keystore from the repository root:

```bash
openssl base64 -A -in /path/to/release.keystore |
  gh secret set ANDROID_KEYSTORE
gh secret set ANDROID_KEYSTORE_PASSWORD
gh secret set ANDROID_KEY_ALIAS
```

Keep the original keystore and password in a secure backup. Google Play updates must be signed
consistently; do not generate a replacement after publishing unless following the Play App
Signing key-upgrade or recovery process.

## iOS and App Store Connect

| Secret | Value |
| --- | --- |
| `APPSTORE_CERTIFICATE_P12` | Base64-encoded Apple Distribution certificate exported as `.p12` |
| `APPSTORE_CERTIFICATE_P12_PASSWORD` | Password selected when exporting the `.p12` |
| `APPSTORE_CODESIGN_KEY` | Full certificate identity, such as `Apple Distribution: Name (TEAMID)` |
| `APPLE_IOS_APPSTORE_PROFILE` | Base64-encoded App Store `.mobileprovision` profile for `com.refractored.myfirenumber` |
| `APPLE_REFRACTORED_ISSUER_ID` | App Store Connect API issuer ID |
| `APPLE_REFRACTORED_KEY_ID` | App Store Connect API key ID |
| `APPLE_REFRACTORED_P8_KEY` | Complete contents of the matching `AuthKey_KEYID.p8` file |

Create an App Store Connect API key with permission to upload builds to TestFlight. Apple permits
downloading its `.p8` private key only once, so retain it securely.

Upload the certificate and API credentials:

```bash
openssl base64 -A -in /path/to/distribution.p12 |
  gh secret set APPSTORE_CERTIFICATE_P12
openssl base64 -A -in /path/to/MyFireNumber_AppStore.mobileprovision |
  gh secret set APPLE_IOS_APPSTORE_PROFILE
gh secret set APPSTORE_CERTIFICATE_P12_PASSWORD
gh secret set APPSTORE_CODESIGN_KEY
gh secret set APPLE_REFRACTORED_ISSUER_ID
gh secret set APPLE_REFRACTORED_KEY_ID
gh secret set APPLE_REFRACTORED_P8_KEY < /path/to/AuthKey_KEYID.p8
```

To find the exact code-signing identity on a Mac where the certificate is installed:

```bash
security find-identity -v -p codesigning
```

Use the full Apple Distribution identity shown by that command for `APPSTORE_CODESIGN_KEY`.

## Mac Catalyst and Mac App Store

The Mac Catalyst workflow publishes an Apple Silicon (`maccatalyst-arm64`) package. It uses the
same App Store Connect API key as iOS, plus Mac-specific signing credentials:

| Secret | Value |
| --- | --- |
| `APPLE_MACCATALYST_APPSTORE_CERTIFICATE_P12` | Base64-encoded `.p12` containing the Apple Distribution and Mac Installer Distribution certificates and private keys |
| `APPLE_MACCATALYST_APPSTORE_CERTIFICATE_PASSWORD` | Password selected when exporting the `.p12` |
| `APPLE_MACCATALYST_APPSTORE_PROFILE` | Base64-encoded Mac App Store `.provisionprofile` for `com.refractored.myfirenumber` |

Both signing identities must be present in the P12 because the workflow signs the app with the
Apple Distribution identity and the installer package with the Mac Installer Distribution
identity. Export the identities and their private keys together from Keychain Access, then upload
the certificate and profile:

```bash
openssl base64 -A -in /path/to/maccatalyst-distribution.p12 |
  gh secret set APPLE_MACCATALYST_APPSTORE_CERTIFICATE_P12
openssl base64 -A -in /path/to/MyFireNumber_Mac_AppStore.provisionprofile |
  gh secret set APPLE_MACCATALYST_APPSTORE_PROFILE
gh secret set APPLE_MACCATALYST_APPSTORE_CERTIFICATE_PASSWORD
```

The profile must be a Mac App Store distribution profile for the production bundle identifier.
Do not use a development, Developer ID, or iOS provisioning profile.

## Running a Signed Build

1. Open **Actions** in GitHub.
2. Select **Build and Package Android**, **Build and Package iOS**, or
   **Build and Package Mac Catalyst**.
3. Choose **Run workflow**.
4. Enable **Build release** and enter the display version.
5. For iOS or Mac Catalyst, optionally enable **Upload to TestFlight**.

The Android workflow uploads a signed AAB artifact. The Apple workflows upload a signed IPA or
PKG artifact and, when requested, send it to TestFlight using the App Store Connect API key.

Never commit keystores, certificates, `.p8` files, passwords, or decoded secret values.

## Windows and Microsoft Store

The Windows workflow creates one unsigned Store upload bundle containing x64 and ARM64
packages. Microsoft signs the package after Store certification.

Create a protected GitHub environment named `microsoft-store`, then add these environment
secrets:

| Secret | Value |
| --- | --- |
| `PARTNER_CENTER_TENANT_ID` | Microsoft Entra tenant ID associated with Partner Center |
| `PARTNER_CENTER_SELLER_ID` | Numeric Partner Center seller ID |
| `PARTNER_CENTER_CLIENT_ID` | Client ID of the Partner Center Microsoft Entra application |
| `PARTNER_CENTER_CLIENT_SECRET` | Active client secret for that application |

Grant the Microsoft Entra application the **Manager** role under Partner Center's Microsoft
Entra application management. Creating an app registration in the Entra portal alone is not
enough.

Add these repository variables using the values shown under the app's **Product identity**
page in Partner Center:

| Variable | Value |
| --- | --- |
| `MICROSOFT_STORE_PRODUCT_ID` | Store product ID used by the submission API |
| `MICROSOFT_STORE_IDENTITY_NAME` | Package identity name assigned by Partner Center |
| `MICROSOFT_STORE_PUBLISHER` | Package publisher value, including the `CN=` prefix |
| `MICROSOFT_STORE_PUBLISHER_DISPLAY_NAME` | Public publisher display name |

```bash
gh variable set MICROSOFT_STORE_PRODUCT_ID
gh variable set MICROSOFT_STORE_IDENTITY_NAME
gh variable set MICROSOFT_STORE_PUBLISHER
gh variable set MICROSOFT_STORE_PUBLISHER_DISPLAY_NAME

gh secret set PARTNER_CENTER_TENANT_ID --env microsoft-store
gh secret set PARTNER_CENTER_SELLER_ID --env microsoft-store
gh secret set PARTNER_CENTER_CLIENT_ID --env microsoft-store
gh secret set PARTNER_CENTER_CLIENT_SECRET --env microsoft-store
```

Run **Windows Store Publish** manually with **Diagnose only** enabled before the first
submission. To package without publishing, supply a tag such as `v1.0.0-windows` and leave
**Publish** disabled. Enabling **Publish**, or pushing a matching release tag, submits the
generated `.msixupload` package after the `microsoft-store` environment approval.
