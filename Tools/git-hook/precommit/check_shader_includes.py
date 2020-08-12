#!/usr/bin/python

# Check the case sensitivity of shader includes in staged files.

import sys
from standalone.check_shader_includes import check_shader_includes_of_files

if len(sys.argv) > 1:
	check_shader_includes_of_files(sys.argv[1:])
