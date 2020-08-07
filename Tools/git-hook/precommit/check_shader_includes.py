#!/usr/bin/python

# Script (part of the pre-commit hook suite) that checks the case sensitivity 
# of shader includes in staged files.
# Windows is case-insensitive when it comes to path management.
# This can be problematic on other platforms where we want to ensure the #included path is correct.

import sys, subprocess, re, os, pathlib
from enum import Enum
from datetime import datetime

class ShaderStatus(Enum):
	NOT_FOUND = 1
	FOUND = 2

class Shader():
	def __init__(self, filesystem_path):
		self.filesystem_path = filesystem_path
		self.status = ShaderStatus.FOUND

class ProcessedFile():
	def __init__(self, path, shader_includes_of_file):
		self.path = path
		self.shader_includes_of_file = shader_includes_of_file


def file_exists(path):
	head, tail = os.path.split(path)
	if tail == '':
		return True
	else:
		try:
			if tail not in os.listdir(head):
				return False
		except FileNotFoundError:
			return False
		return file_exists(head)
	

def find_matches_in_file(file):
	global_regex = re.compile(r'(.+)?#include\s\".+.[Hh][Ll][Ss][Ll]')
	is_comment_regex = re.compile(r'^(\/|\*).+$')
	regex = re.compile(r'(?<=#include\s\").+\.[Hh][Ll][Ss][Ll]')

	f = open(file, 'r')
	file_content = f.read()
	f.close()
	shader_includes_of_file = []
	for global_match in global_regex.finditer(file_content, re.MULTILINE):
		global_match = global_match.group(0)
		
		# Deal with comments
		is_comment_match = is_comment_regex.search(global_match)
		if is_comment_match:
			# Do not consider comments, documentation...
			continue
		match = regex.search(global_match)
		if not match:
			continue
		stripped_match = match.group(0).replace('"', '')

		# Deal with absolute VS relative includes
		match_absolute_path = re.search('Packages/', stripped_match)
		if match_absolute_path:
            # The include is "absolute", e.g. "Packages/com.unity.some-package/some-shader.hlsl"
            # Concat repository root to the filename to find the file (stripping "Packages")
			case_insensitive_path = os.path.join(srp_root, stripped_match.replace('Packages/', ''))
		else:
            # The include is "relative", e.g "./some-shader.hlsl"
            # Concat the location of the file to the filename to find the file
			current_location = os.path.dirname(file)
			case_insensitive_path = os.path.join(current_location, stripped_match)

		shader = Shader(case_insensitive_path)
		if not file_exists(case_insensitive_path):
			shader.status = ShaderStatus.NOT_FOUND
		shader_includes_of_file.append(shader)
	return shader_includes_of_file


def find_matches(files):
	monitored_extensions = ['.compute', '.shader', '.hlsl', '.json', '.cs']

	nb_shader_not_found = 0
	processed_files = []
	for file in files:
		_, extension = os.path.splitext(file)
		if extension in monitored_extensions:
			shader_includes_of_file = find_matches_in_file(file)
			processed_file = ProcessedFile(file, shader_includes_of_file)
			nb_shader_not_found += sum(1 for shader in processed_file.shader_includes_of_file if shader.status == ShaderStatus.NOT_FOUND)
			processed_files.append(processed_file)
	return nb_shader_not_found, processed_files


def write_results(nb_shader_not_found, processed_files):
	if len(processed_files) > 0:
		log_file = os.path.join(srp_root, 'check-shader-includes.log')
		if os.path.exists(log_file):
			os.remove(log_file)
		log = open(log_file, 'w')
		now = datetime.now()
		dt_string = now.strftime("%d/%m/%Y %H:%M:%S")
		print(f'Shader includes check report issued on: {dt_string}.', file=log)
		print(f'{nb_shader_not_found} shader(s) not found on the filesystem.', file=log)

		for processed_file in processed_files:
			for shader in processed_file.shader_includes_of_file:
				if shader.status == ShaderStatus.NOT_FOUND:
					print(f'[Warning] [{processed_file.path}] Found include for [{shader.filesystem_path}] and it does not match the filesystem (check the case sensitivity).', file=log)
		
		print('', file=log)
		for processed_file in processed_files:
			if len(processed_file.shader_includes_of_file) > 0:
				for shader in processed_file.shader_includes_of_file:
					if shader.status == ShaderStatus.FOUND:
						print(f'[OK] [{processed_file.path}] Found include for [{shader.filesystem_path}] and it matches the filesystem (case sensitive).', file=log)
			else:
				print(f'[OK] [{processed_file.path}] No shader include found in this file. Skipped shader includes checks.', file=log)

		print(f'FAILED - There may be an error with the shader includes in the files you\'re trying to commit. A report was generated in {log_file}', file=sys.stderr)
		exit(1)
	else:
		print(f'All shader includes were successfully tested. No report was generated.', file=sys.stderr)
		exit(0)

if len(sys.argv) > 1:
	raw_srp_root = subprocess.check_output(["git", "rev-parse", "--show-toplevel"])
	srp_root = raw_srp_root.decode('utf-8').rstrip()
	nb_shader_not_found, processed_files = find_matches(sys.argv[1:])
	write_results(nb_shader_not_found, processed_files)