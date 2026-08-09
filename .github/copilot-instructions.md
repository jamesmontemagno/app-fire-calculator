# FIRE Calculator Repository Instructions

## Repository Structure

- `web/` contains the deployed React Progressive Web App.
- `app/MyFireNumber/` contains the .NET MAUI companion app.
- `app/MyFireNumber.Core/` contains the shared, platform-neutral calculations and workbook exports.
- `app/MyFireNumber.Storage/` contains the shared SQLite persistence layer.
- `app/MyFireNumber.Tests/` contains the unit tests for the shared calculation and storage libraries.
- `MyFireNumber.slnx` is the solution entry point for the MAUI app.
- `.github/workflows/deploy.yml` builds and deploys only the web app.

Use the path-specific instructions in `.github/instructions/` for implementation details.

## Shared Product Rules

- Keep all financial calculations client-side. Do not add analytics, tracking, or backend dependencies.
- Treat financial values as sensitive user data. Do not transmit them off-device.
- Store percentages as decimals (`0.07` means 7%) and currency values in dollars.
- Keep calculation formulas and defaults consistent across web and mobile implementations.
- Preserve accessibility, responsive layouts, dark mode, and reduced-motion support.
- Include clear educational disclaimers; calculations are estimates, not financial advice.

## Change Discipline

- Keep web-only changes under `web/` and mobile-only changes under `app/` unless shared repository configuration must change.
- Do not edit or commit generated output such as `node_modules/`, `dist/`, `bin/`, or `obj/`.
- Never commit signing keys, provisioning profiles, certificates, credentials, or secrets.
- Validate the smallest affected project after changes. A web-only change should not require a MAUI build, and a mobile-only change should not require a web build.
