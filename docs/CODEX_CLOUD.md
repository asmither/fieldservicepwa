# Codex Cloud Setup

Use this repo with a Codex cloud environment for repeatable remote testing.

## Environment Variables

Add these in Codex environment settings for `asmither/fieldservicepwa`.

Required:

- `ICS_API_BASE_URL` - API endpoint used by the mobile app, for example a staging API URL.
- `ICS_IMAGE_BASE_URL` - image/blob base URL used by the mobile app.

Optional:

- `ICS_ENVIRONMENT` - `Debug`, `Stage`, or `Production`. Defaults to `Stage` in the setup script.
- `ICS_AZURE_MAPS_KEY` - test-only Azure Maps key for map images and geocoding.
- `ICS_BLUON_API_KEY` - leave blank unless Bluon is intentionally re-enabled.
- `ICS_BLUON_API_ROOT` - defaults to the existing Bluon gateway URL.

Use test-only, revocable values. Do not paste production `local.settings.json` values into Codex.

## Setup Script

Use this as the Codex cloud setup script:

```bash
bash scripts/setup-codex-cloud.sh
```

The script writes `ICS.Mobile/wwwroot/appsettings.json` from Codex environment variables, then restores and builds `FieldServicePwa.sln`.

`appsettings.json` is ignored by git. `ICS.Mobile/wwwroot/appsettings.example.json` documents the shape without storing real credentials.
