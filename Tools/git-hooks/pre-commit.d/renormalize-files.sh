#!/bin/sh

# Renormalizing files tracked by git is important to avoid line endings conflicts.
# On windows line endings are represented by CRLF, on *nix by LF.
# By convention, git uses LF files. This script ensures that all committed files have
# LF line endings.

# Will match all packages folders (e.g. com.unity.shadergraph)
monitored_folders_regex="(com.unity).+"

renormalize()
{
  echo "Renormalizing files if needed (git add --renormalize)."
  for dir in ./*
  do
  	# match=`find $dir -regex $monitored_folders_regex -type d`
	match=`echo $dir | grep -E $monitored_folders_regex | wc -l`
    if [ "$match" = 1 ];
    then

      # Retrieve files that changed since last commit
      git diff --quiet HEAD -- $dir
      has_changed=$?
      if [ "$has_changed" -eq 0 ];
      then
        continue
      fi

      # Renormalize files that changed and that we monitor
      git add $dir --renormalize
      if [ "$?" -ne 0 ]
      then
        echo "Could not renormalize $dir's content."
        exit 1
      else
        echo "Renormalized $dir's content."
      fi
    fi
  done
  echo "Completed."
}

cd ../../../ # Go back to root of repository
renormalize

exit 0