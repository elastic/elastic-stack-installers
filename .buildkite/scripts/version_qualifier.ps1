# An opinionated approach to managing the Elastic Qualifier for the DRA in a Google Bucket
# instead of using a Buildkite env variable.

if ($env:VERSION_QUALIFIER) {
    Write-Host "~~~ VERSION_QUALIFIER externally set to [$env:VERSION_QUALIFIER]"
    return
}

# DRA_BRANCH can be used for manually testing packaging with PRs
# e.g. define `DRA_BRANCH="main"` under Options/Environment Variables in the Buildkite UI after clicking new Build
$BRANCH = if ($env:DRA_BRANCH) { $env:DRA_BRANCH } else { $env:BUILDKITE_BRANCH }

$qualifier = ""
$URL = "https://storage.googleapis.com/dra-qualifier/$BRANCH"

try {
    $response = Invoke-WebRequest -Uri $URL -UseBasicParsing -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        $qualifier = $response.Content.Trim()
    }
} catch {
    Write-Host "Warning: Could not retrieve qualifier from $URL" -ForegroundColor Yellow
}

$env:VERSION_QUALIFIER = $qualifier
Write-Host "~~~ VERSION_QUALIFIER set to [$env:VERSION_QUALIFIER]"
