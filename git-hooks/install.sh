#!/bin/sh

# This script creates hard links from the git-hooks folder to the .git/hooks folder.
# Think of it as a copy of the files contained in git-hooks/ except every future change 
# made to these files will also be applied to the files in .git/hooks.
# You just have to run this script after cloning the repo and each time a new hook is added.

# Logs of the installation
log_file="install-hooks.log"

rm $log_file

# Create directories in .git/hooks/ to match the filesystem inside git-hooks/
find . -type d -exec mkdir {} "../.git/hooks/{}" \; 2> $log_file
# Create hardlinks for all the files (recursively), excluding some that aren't hooks
find . -type f -not \( -iname \*.log -o -iname \*.md -o -iname \*.txt \) -not -path "./install.sh" -exec ln {} "../.git/hooks/{}" \; 2>> $log_file