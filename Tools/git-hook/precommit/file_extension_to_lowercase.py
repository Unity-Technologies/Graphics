#!/usr/bin/python

# Ensures all files have their extensions in lowercase, as it can cause problems on Unix systems if not (file not found).

import sys
from standalone.file_extension_to_lowercase import convert_extension_of_files

if len(sys.argv) > 1:
	convert_extension_of_files(sys.argv[1:])
