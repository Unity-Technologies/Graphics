#!/bin/sh

# Script that verifies that each branch follows the naming convention.
# Convention: 
#   - All branches in a folder (e.g. 'automation/git-hooks')
#   - All characters lowercase, except for HDRP (e.g. 'HDRP/staging')
# The convention is enforced for all newly created branches.

LC_ALL=C

cd ../../../
local_branch="$(git rev-parse --abbrev-ref HEAD)"
remote_exists="$(git ls-remote --heads origin $local_branch | wc -l)"
cd - > /dev/null
valid_branch_regex="^((([a-z0-9]|-|_|\.)+\/)+)([a-z0-9]|-|_|\.)+$"
message="There is something wrong with your branch name. Branch names in this project must adhere to this contract: $valid_branch_regex (e.g. 'folder/something'). Your commit will be rejected. You should rename your branch to a valid name and try again."

handle_hdrp_exception()
{
    lowercased="hdrp"
    uppercased="HDRP"

	# Retrieve first folder of the branch, e.g for "hdrp/something" it will match "hdrp"
	first_folder="$(echo $local_branch | sed -n -E -e 's/(^.+)(\/.*)/\1/p')"

    # hdrp/something does not follow the convention
    if [ "$first_folder" = "$lowercased" ];
    then
        echo "$message"
        exit 1
    fi

    # HDRP/something follows the convention
    if [ "$first_folder" = $uppercased ];
    then
        # Replacing HDRP by hdrp since this is an exception (This won't actually change the branch's name)
        local_branch=`echo $local_branch | sed "s/$uppercased/$lowercased/"`
    fi
}

check_norm()
{
    handle_hdrp_exception
	is_valid=`echo $local_branch | grep -E $valid_branch_regex | wc -l`
    if test $remote_exists -ne 1 && test $is_valid -eq 0 ;
    then
        echo "$message"
        exit 1
    fi
}

main()
{
	echo "Checking that branch name follows the repository convention..."
	check_norm
	echo "Completed."
	exit 0
}

main
