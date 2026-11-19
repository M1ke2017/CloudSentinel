# Run in the root of the CloudSentinel repository.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$readme = Join-Path $root 'README.md'
$dir = Join-Path $root 'screenshots'

if (-not (Test-Path -LiteralPath $readme)) { throw "README.md not found: $readme" }
if (-not (Test-Path -LiteralPath $dir)) { throw "screenshots folder not found: $dir" }

$files = @(Get-ChildItem -LiteralPath $dir -File | Where-Object { $_.Extension -match '^\.(png|jpe?g|webp|gif)$' } | Sort-Object Name)
$start = '<!-- SCREENSHOTS:START -->'
$end = '<!-- SCREENSHOTS:END -->'
$content = Get-Content -LiteralPath $readme -Raw -Encoding UTF8
if (-not $content.Contains($start) -or -not $content.Contains($end)) {
    throw 'README screenshot markers were not found.'
}

$lines = [System.Collections.Generic.List[string]]::new()
if ($files.Count -eq 0) {
    $lines.Add('_No screenshot files found in `screenshots/`._')
} else {
    foreach ($file in $files) {
        $caption = [System.IO.Path]::GetFileNameWithoutExtension($file.Name) -replace '[-_]', ' '
        $caption = $caption -replace '[\[\]]', ''
        $encodedName = [System.Uri]::EscapeDataString($file.Name)
        $lines.Add("### $caption")
        $lines.Add('')
        $lines.Add("![$caption](screenshots/$encodedName)")
        $lines.Add('')
    }
}
$section = $start + "`n" + (($lines -join "`n").TrimEnd()) + "`n" + $end
$pattern = '(?s)' + [regex]::Escape($start) + '.*?' + [regex]::Escape($end)
$newContent = [regex]::Replace($content, $pattern, [System.Text.RegularExpressions.MatchEvaluator]{ param($match) $section }, 1)
[System.IO.File]::WriteAllText($readme, $newContent, [System.Text.UTF8Encoding]::new($false))
Write-Host "Updated README.md with $($files.Count) screenshot(s)."
