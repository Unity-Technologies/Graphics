#!/bin/sh

# This script creates symbolic links from the git-hooks folder to the .git/hooks folder.
# Think of it as a copy of the files contained in git-hooks/ except every future change 
# made to these files will also be applied to the files in .git/hooks.
# You just have to run this script after cloning the repo and each time a new hook is added.

pwd_repo=`git rev-parse --show-toplevel`
if [ $? != 0 ]; then
	exit 1
fi
source_folder="$pwd_repo/Tools/git-hooks/"
cd $source_folder
target_folder="$pwd_repo/.git/hooks"

install_hook()
{
	file=$1
	# Set execution rights for the script (needed on unix systems)
	chmod +x "$source_folder/$file"; 
	# Check if the hook already exists, if yes delete it
	{ ls "$target_folder/$file" > /dev/null 2>&1 && echo "Updating $file..." && rm "$target_folder/$file"; } || echo "Installing $file..." 
	# Create the actual symlink
	ln -s "$source_folder/$file" "$target_folder/$file" 
}

# Create directories in .git/hooks/ to match the filesystem inside git-hooks/
find . -type d -not \( -iname \. \) -exec mkdir "$target_folder/{}" \; 2> /dev/null
# Create symlinks for all the files (recursively), excluding some that aren't hooks
find . -type f -not \( -iname \*.log -o -iname \*.md -o -iname \*.txt \) -not -path "*-install.*" | while read p; do install_hook "$p"; done