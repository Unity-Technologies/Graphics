#!/usr/bin/env python3

# Converts all file extensions from uppercase to lowercase for the folders passed as arguments.

import sys, os, subprocess

def git_move_file(current_name, new_name):
	print(f'git mv {current_name} {new_name}')
	subprocess.call(["git", "mv", current_name, new_name])


def convert_extension_of_file(filename, file_extension):
	current_filename = f'{filename}{file_extension}'
	correct_filename = f'{filename}{file_extension.lower()}'
	git_move_file(current_filename, correct_filename)

	# Handle associated unity meta file
	current_meta_filename = f'{current_filename}.meta'
	if os.path.exists(current_meta_filename):
		correct_meta_filename = f'{correct_filename}.meta'
		git_move_file(current_meta_filename, correct_meta_filename)


# When executed from the git hooks this is the entry point
def convert_extension_of_files(files):
	for file in files:
		filename, file_extension = os.path.splitext(file)
		if file_extension.isupper():
			convert_extension_of_file(filename, file_extension)


def convert_extensions_of_folder(folder):
	for root, directories, files in os.walk(folder):
		convert_extension_of_files(files)


if __name__ == "__main__":
	targets = sys.argv[1:]
	if len(targets) == 0:
		print(f'Usage: python {sys.argv[0]} [list of folders containing files to convert]')
		exit(0)
	for target in targets:
		convert_extensions_of_folder(target)
