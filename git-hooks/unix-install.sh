#!/bin/sh

# This script creates symbolic links from the git-hooks folder to the .git/hooks folder.
# Think of it as a copy of the files contained in git-hooks/ except every future change 
# made to these files will also be applied to the files in .git/hooks.
# You just have to run this script after cloning the repo and each time a new hook is added.

pwd_repo=`git rev-parse --show-toplevel`
source_folder="$pwd_repo/git-hooks/"
cd $source_folder
target_folder="$pwd_repo/.git/hooks"

# Create directories in .git/hooks/ to match the filesystem inside git-hooks/
find . -type d -not \( -iname \. \) -exec mkdir "$target_folder/{}" \; 2> /dev/null
# Create symlinks for all the files (recursively), excluding some that aren't hooks
find . -type f -not \( -iname \*.log -o -iname \*.md -o -iname \*.txt \) -not -path "*-install.*" -exec ln -s "$source_folder/{}" "$target_folder/{}" \; 2> /dev/null