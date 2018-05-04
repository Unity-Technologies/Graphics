#!/usr/bin/python -B
import os
import logging

def packages_list():
    return [
        ("com.unity.shadergraph", "com.unity.shadergraph")
    ]

# Prepare an empty project for editor tests
def prepare_editor_test_project(repo_path, project_path, logger):
    import unity_package_build
    unity_package_build.copy_path_to_project("TestbedAssets", repo_path, project_path, logger)
    unity_package_build.copy_path_to_project("Testing", repo_path, project_path, logger)

if __name__ == "__main__":
    import sys
    sys.path.insert(0, os.path.abspath(os.path.join("..", "automation-tools")))
    
    try:
        import unity_package_build
        build_log = unity_package_build.setup()
    except ImportError:
        print "No Automation Tools found."
