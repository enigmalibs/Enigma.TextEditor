#!/usr/bin/env bash
#
# Publishes Enigma Text Editor (Enigma.TextEditor) for Linux x64. Runs, from wherever it is started:
#
#   dotnet publish src/Enigma.TextEditor.Desktop -c Release -r linux-x64 -p:PublishReadyToRun=true
#
# A Release build for linux-x64, compiled ReadyToRun so it starts faster, which runs on an installed
# .NET 10 runtime. The result lands in
#
#   src/Enigma.TextEditor.Desktop/bin/Release/net10.0/linux-x64/publish/
#
# and dotnet's last line names that folder. Start Enigma.TextEditor.Desktop in it, or install it for the
# current user with: ./packaging/linux/install.sh --from <that folder>

set -euo pipefail

# Resolved from this script rather than the current directory, so it runs from anywhere.
script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
readonly script_dir
readonly project="${script_dir}/../../src/Enigma.TextEditor.Desktop"

# exec: dotnet's exit code is the script's, so a failed publish fails the script.
exec dotnet publish "${project}" -c Release -r linux-x64 -p:PublishReadyToRun=true
