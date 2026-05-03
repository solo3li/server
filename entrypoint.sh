#!/bin/bash
set -e

# --- Load Environment Variables from .env ---
if [ -f .env ]; then
    echo "Loading variables from .env..."
    # Use a while loop to handle values with spaces correctly
    while IFS='=' read -r key value; do
        # Skip comments and empty lines
        [[ $key =~ ^#.* ]] || [[ -z $key ]] && continue
        export "$key=$value"
    done < .env
fi

# --- Start ASP.NET Core App ---
echo "Starting Uis.Server on port ${PORT:-80}..."
export ConnectionStrings__DefaultConnection="Data Source=uis.db"
export ASPNETCORE_URLS="http://+:${PORT:-80}"

exec dotnet Uis.Server.dll
