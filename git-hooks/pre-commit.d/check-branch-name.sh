#!/bin/sh

# Script that verifies that each branch follows the naming convention.
# Convention: 
#   - All branches in a folder (e.g. 'automation/git-hooks')
#   - All characters lowercase, except for HDRP (e.g. 'HDRP/staging')
# The convention is enforced for all newly created branches.

LC_ALL=C

local_branch="$(git rev-parse --abbrev-ref HEAD)"
remote_exists="$(git ls-remote --heads origin $local_branch | wc -l)"
valid_branch_regex="^((([a-z]|-)+\/)+)([a-z]|-)+$"
message="There is something wrong with your branch name. Branch names in this project must adhere to this contract: $valid_branch_regex (e.g. 'folder/something'). Your commit will be rejected. You should rename your branch to a valid name and try again."

handle_hdrp_exception()
{
    lowercased="hdrp"
    uppercased="HDRP"
    tmpIFS=$IFS
    IFS='/' 
    read -r -a tokenized_local_branch <<< $local_branch
    IFS=$tmpIFS

    # hdrp/something does not follow the convention
    if [[ ${tokenized_local_branch[0]} == $lowercased ]]
    then
        echo "$message"
        exit 1
    fi

    # HDRP/something follows the convention
    if [[ ${tokenized_local_branch[0]} == $uppercased ]]
    then
        # Replacing HDRP by hdrp since this is an exception (This won't actually change the branch's name)
        local_branch="$(echo $local_branch | sed "s/$uppercased/$lowercased/")" 
    fi
}

check_norm()
{
    handle_hdrp_exception

    if [[ $remote_exists -ne 1 && ! $local_branch =~ $valid_branch_regex ]]
    then
        echo "$message"
        exit 1
    fi
}

echo "Checking that branch name follows the repository convention..."
check_norm
echo "Completed."
exit 0