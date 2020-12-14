#!/usr/bin/python

import sys, os, subprocess, re
from os import path as os_path


# To use with preinstall.py
# Appends pre-existing hooks to the ones we're installing.
def append_old_hooks():
	hooks = ["pre-push", "post-commit", "post-merge", "post-checkout"]
	
	root = subprocess.check_output(["git", "rev-parse", "--show-toplevel"])
	path = root.decode('utf-8').rstrip() + "/.git/hooks/"
	for hook in hooks:
		newly_installed_hook = path + hook
		tmp_hook = "./git-hook/tmp_" + hook

		if not os_path.exists(tmp_hook):
			continue

		with open(newly_installed_hook, 'r') as newly_installed_hook_file:
			newly_installed_hook_data = [ x.strip('\n') for x in list(newly_installed_hook_file) ]
		with open(tmp_hook, 'r') as tmp_hook_file:
			tmp_hook_data = [ x.strip('\n') for x in list(tmp_hook_file) ]
				
		# Append only the difference
		with open(newly_installed_hook, 'a') as newly_installed_hook_file:
			newly_installed_hook_file.write('\n')
			pattern = re.compile("^#\s\s\s(At:).*$")
			for line in tmp_hook_data:
				# Don't append the time tag husky is adding (not pattern.match(line))
				if line not in newly_installed_hook_data and not pattern.match(line):
					newly_installed_hook_file.write(line + '\n')
		
		os.remove(tmp_hook) 


if __name__== "__main__":
	append_old_hooks()
