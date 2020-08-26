#!/usr/bin/python

# Check the case sensitivity of shader includes in staged files.
# Windows is case-insensitive when it comes to path management.
# This can be problematic on other platforms where we want to ensure the #included path is correct.

import sys, subprocess, re, os, pathlib
from enum import Enum
from datetime import datetime


class Config():
    def __init__(self):
        raw_srp_root = subprocess.check_output(["git", "rev-parse", "--show-toplevel"])
        self.srp_root = raw_srp_root.decode('utf-8').rstrip()

        exclusion_list_file = open(os.path.join(self.srp_root, 'Tools/standalone/check-shader-includes-exclusions.txt'), 'r')
        self.exclusion_list = exclusion_list_file.read()
        exclusion_list_file.close()

        self.log_file = os.path.join(self.srp_root, 'check-shader-includes.log')



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


# This is more than a simple os.path.exists():
# On windows it ensures the check is done according to case sensitivity
def file_exists(path):
    head, tail = os.path.split(path)
    if tail == '' or head == '':
        return True
    else:
        try:
            if tail not in os.listdir(head):
                return False
        except FileNotFoundError:
            return False
        return file_exists(head)


def file_excluded(config, path):
    return path in config.exclusion_list


def find_matches_in_file(config, file):
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
            case_insensitive_path = os.path.join(config.srp_root, stripped_match.replace('Packages/', ''))
        else:
            # The include is "relative", e.g "./some-shader.hlsl"
            # Concat the location of the file to the filename to find the file
            current_location = os.path.dirname(file)
            case_insensitive_path = os.path.join(current_location, stripped_match)

        shader = Shader(case_insensitive_path)
        if not file_exists(case_insensitive_path) and not file_excluded(config, stripped_match.replace('Packages/', '')):
            shader.status = ShaderStatus.NOT_FOUND
        shader_includes_of_file.append(shader)
    return shader_includes_of_file


def find_matches(config, files):
    monitored_extensions = ['.compute', '.shader', '.hlsl', '.json', '.cs']

    nb_shader_not_found = 0
    processed_files = []
    for file in files:
        _, extension = os.path.splitext(file)
        if extension in monitored_extensions:
            shader_includes_of_file = find_matches_in_file(config, file)
            processed_file = ProcessedFile(file, shader_includes_of_file)
            nb_shader_not_found += sum(1 for shader in processed_file.shader_includes_of_file if shader.status == ShaderStatus.NOT_FOUND)
            processed_files.append(processed_file)
    return nb_shader_not_found, processed_files


def write_results(config, nb_shader_not_found, processed_files):
    if nb_shader_not_found > 0:
        if os.path.exists(config.log_file):
            os.remove(config.log_file)
        log = open(config.log_file, 'w')
        now = datetime.now()
        dt_string = now.strftime("%d/%m/%Y %H:%M:%S")
        print(f'If you think this tool reported a false positive, add the full path (starting from the repository root, only absolute paths e.g. `com.unity.render-pipelines.core/shader.hlsl`) to ./check-shader-includes-exclusions.txt!\n', file=log)
        print(f'Shader includes check report issued on: {dt_string}.', file=log)
        print(f'{nb_shader_not_found} shader(s) not found on the filesystem.', file=log)

        # First, process missing shaders
        for processed_file in processed_files:
            for shader in processed_file.shader_includes_of_file:
                if shader.status == ShaderStatus.NOT_FOUND:
                    print(f'[Warning] [{processed_file.path}] Found include for [{shader.filesystem_path}] and it does not match the filesystem (check the case sensitivity).', file=log)

        # Then, log existing shaders in the report
        print('', file=log)
        for processed_file in processed_files:
            if len(processed_file.shader_includes_of_file) > 0:
                for shader in processed_file.shader_includes_of_file:
                    if shader.status == ShaderStatus.FOUND:
                        print(f'[OK] [{processed_file.path}] Found include for [{shader.filesystem_path}] and it matches the filesystem (case sensitive).', file=log)
            else:
                print(f'[OK] [{processed_file.path}] No shader include found in this file. Skipped shader includes checks.', file=log)

        print(f'FAILED - There may be an error with the shader includes in the files you\'re trying to commit. A report was generated in {config.log_file}', file=sys.stderr)
        exit(1)
    else:
        print(f'All shader includes were successfully tested. No report was generated.', file=sys.stderr)
        exit(0)


# When executed from the git hooks this is the entry point
def check_shader_includes_of_files(files):
    config = Config()
    nb_shader_not_found, processed_files = find_matches(config, files)
    write_results(config, nb_shader_not_found, processed_files)


def check_shader_includes_of_folder(folder):
    config = Config()
    nb_shader_not_found = 0
    processed_files = []
    for root, _, files in os.walk(folder):
        tmp_shader_not_found, tmp_processed_files = find_matches(config, [os.path.join(root, file) for file in files])
        nb_shader_not_found += tmp_shader_not_found
        processed_files += tmp_processed_files
    write_results(config, nb_shader_not_found, processed_files)


if __name__ == "__main__":
    targets = sys.argv[1:]
    if len(targets) == 0:
        print(f'Usage: python {sys.argv[0]} [list of folders containing files to run the check against]')
        exit(0)
    for target in targets:
        check_shader_includes_of_folder(target)
