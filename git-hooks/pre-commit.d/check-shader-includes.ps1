# Script (part of the pre-commit hook suite) that checks the case sensitivity 
# of shader includes in staged files.
# Windows is case-insensitive when it comes to path management.
# This can be problematic on other platforms where we want to ensure the #included path is correct.

enum ShaderStatus {
    NotFound;
    Found;
}

function Get-CanonicalPath
{
    <# 
        .Synopsis 
            Return a case-sensitive version of the path given as input.
        .Description
            Given a specific path (e.g. C:/uSeRs), this function will return the canonical
            path as it is stored on the disk (e.g. C:/Users).
            Source: https://randombrainworks.com/2017/02/27/powershell-canonical-paths-and-case-sensitivity/
        .Example 
            Get-CanonicalPath -Path "C:/uSeRs"
    #> 
    param($Path)

    if ($Path -is [string]) {
        $Path = (Resolve-Path $Path -erroraction 'silentlycontinue').Path 
    }

    $pathInfo = [System.IO.DirectoryInfo]$Path
    $parent = $pathInfo.Parent

    # if parent is null, we're at the end of the path, e.g. C:\ portion in C:\winDows\SySTEM32
    if($null -eq $parent) {
        return $pathInfo.Name
    }

    # recursively get the canonical, properly cased, path of parent of current path
    $ParentCanonicalPath = Get-CanonicalPath $parent

    # If the current path is a directory, get the proper path using .GetDirectories()
    # else get it using .GetFiles()
    $LeafCanonicalPath = if(Test-Path -PathType Container $pathInfo.FullName) {
        $parent.GetDirectories($pathInfo.Name).Name
    } else {
        $parent.GetFiles($pathInfo.Name).Name
    }

    # combine the parent and leaf paths and return.
    return Join-Path $ParentCanonicalPath $LeafCanonicalPath
}

function Write-Results {
    <# 
        .Synopsis 
            Log results in a report file if there was an include mistake and the commit should not happen.
    #> 
    param ($Results)

    if ($Results[0] -eq $true) {
        # At least one shader was not found, so issue a report 
        $logFile = Join-Path $srpRoot "check-shader-includes.log"
        if ([System.IO.File]::Exists($logFile)) {
            "An old log file already exists. Deleting it..."
            Remove-Item $logFile
        }
        "Shader includes check report issued on: $((Get-Date).ToString())`n" | Out-File -Append $logFile
        foreach ($file in $Results[1]) {
            # Reminder 
            # $file[0] --> FilePath
            # $file[1] --> Array of {[0]: PathToShader as written in the file (case insensitive), [1]: ShaderStatus}
            if ($file[1].count -gt 0) {
                foreach ($shaderInclude in $file[1]) {
                    if ($shaderInclude."ShaderStatus" -eq [ShaderStatus]::Found) {
                        "[OK] [{0}] Found include for [{1}] and it matches the filesystem (case sensitive)." -f $file[0], $shaderInclude."PathToShader" | Out-File -Append $logFile 
                    } else {
                        "[Warning] [{0}] Found include for [{1}] and it does not match the filesystem (check the case sensitivity)." -f $file[0], $shaderInclude."PathToShader" | Out-File -Append $logFile
                    }
                }
            } else {
                "[OK] [{0}] - No shader include found in this file. Skipped shader includes checks." -f $file[0] | Out-File -Append $logFile
            }
            "`n" | Out-File -Append $logFile
        }
        "FAILED - There may be an error with the shader includes in the files you're trying to commit. A report was generated in $logFile."
        exit 1 # Block commit
    } else {
        "PASSED - All shader includes were successfully tested. No report was generated."
        exit 0 # Allow commit
    }
    
}

function Find-MatchesInFile {
    <# 
        .Synopsis 
            Find and matches pattern in given file
    #> 
    param($File)

    $regex = '(?<=#include\s\").+\.hlsl' 
    
    $searchResult = Select-String -Path $File -Pattern $regex
    [System.Collections.ArrayList]$shaderIncludesOfFile = @()

    if ($null -ne $searchResult)
    {
        foreach ($match in $searchResult.Matches) {
            $strippedFilePath = $match.Value.Replace("Packages/", "")
            $caseInsensitivePath = Join-Path $srpRoot $strippedFilePath
            [hashtable]$shaderIncludeProperty = @{}
            $shaderIncludeProperty.Add('PathToShader', $caseInsensitivePath)
            try {
                $caseSensitivePath = Get-CanonicalPath -Path "$caseInsensitivePath"
            } catch {
                $shaderIncludeProperty.Add('ShaderStatus', [ShaderStatus]::NotFound)
                continue
            }
            if (!($caseInsensitivePath -ceq $caseSensitivePath)) {
                # Case sensitive-d path does not match case insensitive path on disk
                $shaderIncludeProperty.Add('ShaderStatus', [ShaderStatus]::NotFound)
            } else {
                # Found a shader include and it matches the filesystem
                $shaderIncludeProperty.Add('ShaderStatus', [ShaderStatus]::Found)
            }
            
            $shaderInclude = New-Object -TypeName psobject -Property $shaderIncludeProperty
            $shaderIncludesOfFile.Add($shaderInclude) | Out-Null
        }
    }

    $File
    (,$shaderIncludesOfFile) # Treat array as a single output variable, instead of one variable per array item
}

function Find-Matches {
    <# 
        .Synopsis 
            Find and matches pattern in a set of files
    #> 
    param($Files)

    [System.Collections.ArrayList]$processedFiles = @()
    $atLeastOneShaderNotFound = $false # This flag will allow us to decide whether or not to issue a report 
    foreach ($file in $Files) {
        $fileResults = Find-MatchesInFile -File $file
        $processedFiles.Add($fileResults) | Out-Null
        $nbShaderNotFound = [Linq.Enumerable]::Any([object[]]$fileResults[1], [Func[object,bool]]{ param($shaderInclude) $shaderInclude."ShaderStatus" -eq [ShaderStatus]::NotFound })
        if ($nbShaderNotFound -gt 0) {
            $atLeastOneShaderNotFound = $true
        }
    }
    
    $atLeastOneShaderNotFound
    (,$processedFiles) # Treat array as a single output variable, instead of one variable per array item
}

function Get-StagedFiles
{
    <# 
        .Synopsis 
            List files in staging (files that are supposed to be committed).
    #> 
    $gitDiffCommand = "git diff --cached --name-only --diff-filter=ACMR"
    $stagedFiles = Invoke-Expression -Command $gitDiffCommand
    if ($null -eq $stagedFiles) {
        "There's no files in staging. Did not start any check."
        exit 0
    }
    $gitShowUnstagedFilesCommand = "git ls-files . --exclude-standard --others -m"
    $unstagedFiles = Invoke-Expression -Command $gitShowUnstagedFilesCommand
    foreach ($stagedFile in $stagedFiles) {
        if ($unstagedFiles.count -gt 0) {
            $fileModifiedSinceGitAdd = [Linq.Enumerable]::Any([string[]]$unstagedFiles, [Func[string,bool]]{ param($unstagedFile) $unstagedFile -eq $stagedFile })
            if ($fileModifiedSinceGitAdd -eq $true) {
                "Warning! $stagedFile was modified locally since you added it to staging. The shader includes check will pass but will be inconclusive. Please add this file to staging."
                exit 1
            }
        }
        Join-Path $srpRoot $stagedFile
    }
}
function Main 
{
    $stagedFiles = Get-StagedFiles
    $results = Find-Matches -Files $stagedFiles
    Write-Results -Results $results
}

# Make sure we're still in the repository in case of custom powershell
# configuration on the client. (e.g. profile.ps1 that cd's to a specific dir. at startup)
if ($null -ne $args[0]) {
    # If comming from the git hook, just take the argument sent by the shell script
    Set-Location -Path $args[0]
} else {
    # If powershell script executed manually from anywhere else in the repository, compute new location
    $pwdRepoCommand = "git rev-parse --show-toplevel"
    $pwdRepo = Invoke-Expression -Command $pwdRepoCommand
    Set-Location -Path $pwdRepo
}
$srpRoot = Get-Location

Main