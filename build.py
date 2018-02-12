#!/usr/bin/python -B
import os
import logging

def packages_list():
    return [
        ("com.unity.shadergraph", os.path.join("MaterialGraphProject", "Assets", "UnityShaderEditor"))
    ]

# Prepare an empty project for editor tests
def prepare_editor_test_project(repo_path, project_path, logger):
    import unity_package_build
    unity_package_build.copy_path_to_project(os.path.join("MaterialGraphProject", "Assets", "NewNodes"), repo_path, project_path, logger)
    unity_package_build.copy_path_to_project(os.path.join("MaterialGraphProject", "Assets", "TestAssets"), repo_path, project_path, logger)
    unity_package_build.copy_path_to_project(os.path.join("MaterialGraphProject", "Assets", "UnityShaderEditor", "Editor", "Testing"), repo_path, project_path, logger)

if __name__ == "__main__":
    import sys
    sys.path.insert(0, os.path.abspath(os.path.join("..", "automation-tools")))
    
    try:
        import unity_package_build
        build_log = unity_package_build.setup()
    except ImportError:
        print "No Automation Tools found."
