# changelog-generator.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$Version,
    
    [Parameter(Mandatory=$true)]
    [string]$CommitID,
    
    [Parameter(Mandatory=$false)]
    [string]$OllamaUrl = "http://localhost:11434"
)

function Get-OllamaResponse {
    param(
        [string]$Prompt,
        [string]$Model = "deepseek-v3.1:671b-cloud"
    )
    
    $body = @{
        model = $Model
        prompt = $Prompt
        stream = $false
    } | ConvertTo-Json
    
    try {
        $response = Invoke-RestMethod -Uri "$OllamaUrl/api/generate" -Method Post -Body $body -ContentType "application/json"
        return $response.response.Trim()
    }
    catch {
        Write-Error "Failed to get response from Ollama: $($_.Exception.Message)"
        return "N/A"
    }
}

function Get-ChangelogSection {
    param(
        [string]$CommitID,
        [string]$Version
    )
    
    # Get commit details
    $commitInfo = git show --no-patch --format="%aI" $CommitID 2>$null
    if (-not $commitInfo) {
        Write-Error "Commit ID $CommitID not found"
        return $null
    }
    
    $commitDate = [DateTime]::Parse($commitInfo)
    $formattedDate = $commitDate.ToString("yyyy-MM-dd HH:mm:ss")
    
    # Get commit message
    $commitMessage = git log --format=%B -n 1 $CommitID
    
    # Generate changelog content using Ollama
    $prompt = @"
Based on the following git commit message, generate a changelog entry in markdown format with three sections: Added, Enhanced, Refactored, Updated, and Removed.

Commit Message:
$commitMessage

Format requirements:
1. Use "### Added", "### Enhanced", "### Updated", "### Refactored", and "### Removed" to separate sections
2. If no changes in a section, write "N/A" for that section
3. Keep it concise and professional
4. Use bullet points for multiple items
5. Only include relevant information from the commit message
6. List the added/modified/removed on each methods/classes/functions/properties/events/etc, the reasons, and file name.
7. Mention the names of the methods, classes, functions, properties, events, etc. that changed

Respond with just the changelog content, no additional text.
"@
    
    $changelogContent = Get-OllamaResponse -Prompt $prompt
    
    # Build the final changelog section
    $changelogSection = @"
## [$Version](https://scm.idecision.ai/idecision_source_net8/idc.utility/-/commit/$CommitID) - $formattedDate

$changelogContent

---
"@
    
    return $changelogSection
}

function Update-ChangelogFile {
    param(
        [string]$NewSection,
        [string]$ChangelogFile = "CHANGELOG.md"
    )
    
    # Read existing content
    $existingContent = ""
    if (Test-Path $ChangelogFile) {
        $existingContent = Get-Content $ChangelogFile -Raw
    }
    
    # Split into header and existing changelog
    $header = "# Changelog`n`n"
    $changelogContent = $existingContent -replace "^# Changelog\s*\n\s*\n", ""
    
    # Insert new section at the beginning
    $newContent = $header + $NewSection + "`n`n" + $changelogContent
    
    # Write back to file
    $newContent | Out-File $ChangelogFile -Encoding UTF8
    
    Write-Host "Changelog updated successfully!" -ForegroundColor Green
    Write-Host "New section added for version $Version" -ForegroundColor Cyan
}

# Main execution
try {
    # Validate git repository
    if (-not (Test-Path ".git")) {
        Write-Error "Not a git repository"
        exit 1
    }
    
    # Validate commit exists
    git cat-file -e $CommitID 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Commit ID $CommitID does not exist"
        exit 1
    }
    
    # Generate changelog section
    $newSection = Get-ChangelogSection -CommitID $CommitID -Version $Version
    if ($newSection) {
        Update-ChangelogFile -NewSection $newSection
    }
}
catch {
    Write-Error "Error: $($_.Exception.Message)"
    exit 1
}