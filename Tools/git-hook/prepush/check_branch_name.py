#!/usr/bin/python

# Script that verifies that each branch follows the naming convention.
# Convention: 
#   - All branches in a folder (e.g. 'automation/git-hooks')
#   - All characters lowercase, except for HDRP (e.g. 'HDRP/staging')
# The convention is enforced for all newly created branches.

import sys, subprocess, re


valid_branch_regex="^((HDRP)|((?!hdrp)([a-z0-9\-_\.]+)))(\/[a-z0-9\-_\.]+)+$"

def check_norm():
	local_branch = subprocess.check_output(["git", "rev-parse", "--abbrev-ref", "HEAD"])
	local_branch = local_branch.decode('utf-8').rstrip()
	message=f"There is something wrong with your branch name. Branch names in this project must adhere to this contract: {valid_branch_regex} (e.g. 'folder/something'). Your push will be rejected. You should rename your branch to a valid name and try again."

	remote_exists = subprocess.call(["git", "ls-remote", "--exit-code", "--heads", "origin", local_branch]) != 2
	branch_follows_convention = re.search(valid_branch_regex, local_branch)

	if not remote_exists and not branch_follows_convention:
		print(message, file=sys.stderr)
		exit(1)


if __name__== "__main__":
	check_norm()