#!/usr/bin/python -B
import os
import json
import logging
import shutil
import textwrap

sub_packages = {}
sub_package_folders = {}
publish_order = []

def packages_list():
    return [
        ("com.unity.shadergraph", os.path.join("MaterialGraphProject", "Assets", "UnityShaderEditor"))
    ]

# helper function for preparations of tests
def copy_path_to_project(path, repo_path, project_path, logger):
    logger.info("Copying {}".format(path))
    shutil.copytree(os.path.join(repo_path, path), os.path.join(project_path, "Assets", os.path.basename(path)))

def copy_file_to_project(path, repo_path, project_path, logger):
    logger.info("Copying {}".format(path))
    shutil.copy(os.path.join(repo_path, path), os.path.join(project_path, "Assets", path))

# Prepare an empty project for editor tests
def prepare_editor_test_project(repo_path, project_path, logger):
    copy_path_to_project(os.path.join("MaterialGraphProject", "Assets", "NewNodes"), repo_path, project_path, logger)
    copy_path_to_project(os.path.join("MaterialGraphProject", "Assets", "TestAssets"), repo_path, project_path, logger)
    copy_path_to_project(os.path.join("MaterialGraphProject", "Assets", "UnityShaderEditor", "Editor", "Testing"), repo_path, project_path, logger)

if __name__ == "__main__":
    import sys
    sys.path.insert(0, os.path.abspath(os.path.join("..", "automation-tools")))
    
    try:
        import unity_package_build
        build_log = unity_package_build.setup()
    except ImportError:
        print "No Automation Tools found."
