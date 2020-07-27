#!/usr/bin/python

import sys, os, subprocess, shutil


def clean_node_modules():
	shutil.rmtree("./node_modules")


# Sometimes git hooks are already existing in .git/hooks. 
# This script saves them, so that postinstall.py can append them to the ones we're installing.
def save_existing_hooks():
	hooks = ["pre-push", "post-commit", "post-merge", "post-checkout"]
	
	root = subprocess.check_output(["git", "rev-parse", "--show-toplevel"])
	path = root.decode('utf-8').rstrip() + "/.git/hooks/"
	for hook in hooks:
		pre_installed_hook = path + hook
		tmp_hook = "./git-hook/tmp_" + hook

		with open(pre_installed_hook, 'r') as pre_installed_hook_file:
  			hook_data = pre_installed_hook_file.read()	  

		with open(tmp_hook, 'w') as tmp_hook_file:
		  tmp_hook_file.write(hook_data)
		
		os.remove(pre_installed_hook) 


if __name__== "__main__":
	clean_node_modules()
	save_existing_hooks()
