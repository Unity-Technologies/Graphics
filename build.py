#!/usr/bin/python -B
import os
import json
import logging
import platform
import shutil
import subprocess
import textwrap

sub_packages = {}
sub_package_folders = {}
publish_order = []

def packages_list():
    return [
        ("com.unity.render-pipelines.core", os.path.join("ScriptableRenderPipeline", "Core")),
        ("com.unity.render-pipelines.high-definition", os.path.join("ScriptableRenderPipeline", "HDRenderPipeline")),
        ("com.unity.render-pipelines.lightweight", os.path.join("ScriptableRenderPipeline", "LightweightPipeline"))
    ]

def prepare(logger):
    file_path = os.path.join("./ScriptableRenderPipeline", "master-package.json")
    if os.path.isfile(file_path):
        logger.info("Found master package file: {}".format(file_path))
        with open(file_path) as file:
            try:
                master_package = json.load(file)
            except json.JSONDecodeError as e:
                logger.critical(e)
                exit(1)

    potential_folders = master_package["subPackages"] if "subPackages" in master_package else []
    for item in potential_folders:
        file_path = os.path.join("./ScriptableRenderPipeline", item, "sub-package.json")
        if os.path.isfile(file_path):
            logger.info("Found sub-package file: {}".format(file_path))
            with open(file_path) as file:
                try:
                    sub_package = json.load(file)
                    sub_packages[sub_package["name"]] = sub_package
                    sub_package_folders[sub_package["name"]
                                       ] = os.path.join("./ScriptableRenderPipeline", item)
                except json.JSONDecodeError as e:
                    logger.critical("Error: {}".format(e))

    if not sub_packages:
        logger.critical("Error: No sub-packages found.")
        exit(1)

    if "version" not in master_package:
        logger.critical("Master package must contain a \"version\" field")
        exit(1)

    print("Propagating master package version to sub-packages")
    for sub_package in sub_packages.values():
        sub_package["version"] = master_package["version"]

    if "unity" in master_package:
        print("Propagating master package Unity version to sub-packages")
        for sub_package in sub_packages.values():
            sub_package["unity"] = master_package["unity"]

    if "dependencies" in master_package and master_package["dependencies"]:
        print("Propagating shared dependencies:")
        for name, version in master_package["dependencies"].items():
            logger.info("  {}@{}".format(name, version))
        for sub_package in sub_packages.values():
            if "dependencies" not in sub_package or not sub_package["dependencies"]:
                sub_package["dependencies"] = {}
            for name, version in master_package["dependencies"].items():
                sub_package["dependencies"][name] = version

    logger.info("Creating dependency tree:")
    dependency_list = {}
    dependency_tree = {}
    for i, sub_package in enumerate(sub_packages.values()):
        dependency_list[sub_package["name"]] = {}
    for i, sub_package in enumerate(sub_packages.values()):
        if "subDependencies" in sub_package and sub_package["subDependencies"]:
            for dependency in sub_package["subDependencies"]:
                dependency_list[dependency][sub_package["name"]
                                            ] = dependency_list[sub_package["name"]]
        else:
            dependency_tree[sub_package["name"]
                            ] = dependency_list[sub_package["name"]]

    if not dependency_tree:
        logger.critical("Dependency tree is empty. You might have a circular reference.")
        exit(1)

    def print_dependency_tree(tree, indent):
        for key, sub_tree in tree.items():
            print(key, "  " * indent)
            print_dependency_tree(sub_tree, indent + 1)

    print_dependency_tree(dependency_tree, 1)

    logger.info("Creating publish order:")
    visited = set()

    def fill_publish_order(tree):
        for key, sub_tree in tree.items():
            if key not in visited:
                publish_order.append(key)
                fill_publish_order(sub_tree)

    fill_publish_order(dependency_tree)
    for name in publish_order:
        logger.info("  {}".format(name))

    print "Resolving dependencies between sub-packages:"
    for sub_package in sub_packages.values():
        if "dependencies" not in sub_package or not sub_package["dependencies"]:
            sub_package["dependencies"] = {}
        if "subDependencies" in sub_package and sub_package["subDependencies"]:
            logger.info("  {}:".format(sub_package["name"]))
            for sub_dependency in sub_package["subDependencies"]:
                sub_package["dependencies"][sub_dependency] = master_package["version"]
                logger.info("    {}@{}".format(sub_dependency,
                                              sub_package["dependencies"][sub_dependency]))
            del sub_package["subDependencies"]

    logger.info("Writing package files:")
    for name in publish_order:
        sub_package = sub_packages[name]
        package_path = os.path.join(sub_package_folders[name], "package.json")
        logger.info("  {}:".format(package_path))
        with open(package_path, 'w') as file:
            json.dump(sub_package, file, indent=4, sort_keys=True)

    import unity_package_build
    for name in publish_order:
        package_path = os.path.join(sub_package_folders[name])
        unity_package_build.copy_file_to_project("LICENSE.md", ".", package_path, logger)
        unity_package_build.copy_file_to_project("CHANGELOG.md", ".", package_path, logger)

def cleanup(logger):
    logger.info("Removing temporary files:")
    files = []
    for name in publish_order:
        folder = sub_package_folders[name]
#        files.append(os.path.join(folder, "package.json"))
    for file in files:
        logger.info("  {}".format(file))
        os.remove(file)

# Prepare an empty project for editor tests
def prepare_editor_test_project(repo_path, project_path, logger):
    import unity_package_build
    dest_path = os.path.join(project_path, "Assets", "ScriptableRenderLoop")
    unity_package_build.copy_path_to_project("ImageTemplates", repo_path, dest_path, logger)
    unity_package_build.copy_path_to_project("Tests", repo_path, dest_path, logger)
    unity_package_build.copy_file_to_project("SRPMARKER", repo_path, dest_path, logger)
    unity_package_build.copy_file_to_project("SRPMARKER.meta", repo_path, dest_path, logger)
    unity_package_build.copy_file_to_project("ImageTemplates.meta", repo_path, dest_path, logger)
    unity_package_build.copy_file_to_project("Tests.meta", repo_path, dest_path, logger)

if __name__ == "__main__":
    import sys
    sys.path.insert(0, os.path.abspath(os.path.join("..", "automation-tools")))
    
    try:
        import unity_package_build
        build_log = unity_package_build.setup()
    except ImportError:
        print "No Automation Tools found."
