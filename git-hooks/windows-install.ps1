# This script creates symbolic links from the git-hooks folder to the .git/hooks folder.
# Think of it as a copy of the files contained in git-hooks/ except every future change 
# made to these files will also be applied to the files in .git/hooks.
# You just have to run this script after cloning the repo and each time a new hook is added.

$hooksFolder = "../.git/hooks/"

# Create directories in .git/hooks/ to match the filesystem inside git-hooks/
$folders = Get-ChildItem -Recurse -Directory -ErrorAction SilentlyContinue
foreach ($folder in $folders) {
  $pathToFolder = Join-Path $hooksFolder $folder
  if (!(Test-Path $pathToFolder -PathType Container)) {
    New-Item -Path $hooksFolder -Name $folder -ItemType "directory" | Out-Null
  }
}

$files = Get-ChildItem -Recurse -File -Exclude *.log,*.md,*.txt,*install* -ErrorAction SilentlyContinue | Resolve-Path -Relative
foreach ($file in $files) {
  $pathToFolder = $hooksFolder
  $pathToFile = Join-Path $pathToFolder $file
  if (!(Test-Path $pathToFile)) {
    New-Item -ItemType SymbolicLink -Path $pathToFolder -Name $file -Target $file | Out-Null
  }
}