# Mobile Release Secrets

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
| `APPSTORE_ISSUER_ID` | App Store Connect API issuer ID |
| `APPSTORE_KEY_ID` | App Store Connect API key ID |
| `APPSTORE_PRIVATE_KEY` | Complete contents of the matching `AuthKey_KEYID.p8` file |

Create an App Store Connect API key with permission to manage provisioning profiles and upload
builds. Apple permits downloading its `.p8` private key only once, so retain it securely.

Upload the certificate and API credentials:

```bash
openssl base64 -A -in /path/to/distribution.p12 |
  gh secret set APPSTORE_CERTIFICATE_P12
gh secret set APPSTORE_CERTIFICATE_P12_PASSWORD
gh secret set APPSTORE_CODESIGN_KEY
gh secret set APPSTORE_ISSUER_ID
gh secret set APPSTORE_KEY_ID
gh secret set APPSTORE_PRIVATE_KEY < /path/to/AuthKey_KEYID.p8
```

To find the exact code-signing identity on a Mac where the certificate is installed:

```bash
security find-identity -v -p codesigning
```

Use the full Apple Distribution identity shown by that command for `APPSTORE_CODESIGN_KEY`.

## Running a Signed Build

1. Open **Actions** in GitHub.
2. Select **Build and Package Android** or **Build and Package iOS**.
3. Choose **Run workflow**.
4. Enable **Build release** and enter the display version.
5. For iOS, optionally enable **Upload to TestFlight**.

The Android workflow uploads a signed AAB artifact. The iOS workflow uploads a signed IPA
artifact and, when requested, sends that IPA to TestFlight using the App Store Connect API key.

Never commit keystores, certificates, `.p8` files, passwords, or decoded secret values.
