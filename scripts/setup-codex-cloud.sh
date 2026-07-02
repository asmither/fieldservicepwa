#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
appsettings_path="$repo_root/ICS.Mobile/wwwroot/appsettings.json"

python3 - "$appsettings_path" <<'PY'
import json
import os
import sys

path = sys.argv[1]

config = {
    "Ics": {
        "Environment": os.environ.get("ICS_ENVIRONMENT", "Stage"),
        "ApiUrl": os.environ.get("ICS_API_BASE_URL", ""),
        "ImageBaseUrl": os.environ.get("ICS_IMAGE_BASE_URL", ""),
        "AzureMapsKey": os.environ.get("ICS_AZURE_MAPS_KEY", ""),
        "BluonApiKey": os.environ.get("ICS_BLUON_API_KEY", ""),
        "BluonApiRoot": os.environ.get(
            "ICS_BLUON_API_ROOT",
            "https://hub.bluon.com/gateway/interior-climate-solutions",
        ),
    }
}

missing = [
    name
    for name, value in {
        "ICS_API_BASE_URL": config["Ics"]["ApiUrl"],
        "ICS_IMAGE_BASE_URL": config["Ics"]["ImageBaseUrl"],
    }.items()
    if not value
]

if missing:
    raise SystemExit(f"Missing required environment variables: {', '.join(missing)}")

with open(path, "w", encoding="utf-8") as f:
    json.dump(config, f, indent=2)
    f.write("\n")
PY

dotnet restore "$repo_root/FieldServicePwa.sln"
dotnet build "$repo_root/FieldServicePwa.sln" --configuration Release --no-restore
