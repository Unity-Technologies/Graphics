#!/usr/bin/python

import sys, os, subprocess


# To use with preinstall.py
# Appends pre-existing hooks to the ones we're installing.
def append_old_hooks():
	hooks = ["pre-push", "post-commit", "post-merge", "post-checkout"]
	
	root = subprocess.check_output(["git", "rev-parse", "--show-toplevel"])
	path = root.decode('utf-8').rstrip() + "/.git/hooks/"
	for hook in hooks:
		pre_installed_hook = path + hook
		tmp_hook = "./git-hook/tmp_" + hook

		with open(tmp_hook, 'r') as tmp_hook_file:
  			hook_data = tmp_hook_file.read()	  
		
		# Append tmp, aka pre-existing hook to the one created by npm install
		with open(pre_installed_hook, 'a') as pre_installed_hook_file:
		  pre_installed_hook_file.write(hook_data)
		
		os.remove(tmp_hook) 


if __name__== "__main__":
	append_old_hooks()

