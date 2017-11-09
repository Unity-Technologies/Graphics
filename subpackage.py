import argparse
import os
import json
import textwrap
from pprint import pprint


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("folder", help="the folder containing master-package.json and the sub-package folders")
    parser.add_argument("-v", "--verbose", action="store_true", help="tell me what's going on here")
    args = parser.parse_args()
    verbose = args.verbose
    base_folder = os.path.realpath(args.folder)

    def error_print(msg):
        print("[ERROR] {}".format(msg))

    def warning_print(msg):
        print("[WARNING] {}".format(msg))

    def info_print(msg):
        if verbose:
            print(msg)

    info_print("Using folder: {}".format(base_folder))

    file_path = os.path.join(base_folder, "master-package.json")
    if os.path.isfile(file_path):
        info_print("Found master package file: {}".format(file_path))
        with open(file_path) as file:
            try:
                master_package = json.load(file)
            except json.JSONDecodeError as e:
                error_print("Error: {}".format(e))
                exit(1)

    sub_packages = []
    sub_package_folders = []
    for item in os.listdir(base_folder):
        file_path = os.path.join(base_folder, item, "sub-package.json")
        if os.path.isfile(file_path):
            info_print("Found sub-package file: {}".format(file_path))
            with open(file_path) as file:
                try:
                    sub_packages.append(json.load(file))
                    sub_package_folders.append(os.path.join(base_folder, item))
                except json.JSONDecodeError as e:
                    error_print("Error: {}".format(e))

    if not sub_packages:
        error_print("Error: No sub-packages found.")
        exit(1)

    if "version" not in master_package:
        error_print("Master package must contain a \"version\" field")
        exit(1)

    info_print("Propagating master package version to sub-packages")
    for sub_package in sub_packages:
        sub_package["version"] = master_package["version"]

    if "unity" in master_package:
        info_print("Propagating master package Unity version to sub-packages")
        for sub_package in sub_packages:
            sub_package["unity"] = master_package["unity"]

    if "dependencies" in master_package and master_package["dependencies"]:
        info_print("Propagating shared dependencies:")
        for name, version in master_package["dependencies"].items():
            info_print("\t{}@{}".format(name, version))
        for sub_package in sub_packages:
            if "dependencies" not in sub_package or not sub_package["dependencies"]:
                sub_package["dependencies"] = {}
            for name, version in master_package["dependencies"].items():
                sub_package["dependencies"][name] = version

    info_print("Resolving dependencies between sub-packages:")
    for sub_package in sub_packages:
        if "dependencies" not in sub_package or not sub_package["dependencies"]:
            sub_package["dependencies"] = {}
        if "subDependencies" in sub_package and sub_package["subDependencies"]:
            info_print("\t{}:".format(sub_package["name"]))
            for sub_dependency in sub_package["subDependencies"]:
                sub_package["dependencies"][sub_dependency] = master_package["version"]
                info_print("\t\t{}@{}".format(sub_dependency, sub_package["dependencies"][sub_dependency]))
            del sub_package["subDependencies"]

    info_print("Writing package files:")
    for i, sub_package in enumerate(sub_packages):
        package_path = os.path.join(sub_package_folders[i], "package.json")
        info_print("\t{}".format(package_path))
        with open(package_path, 'w') as file:
            json.dump(sub_package, file, indent=4)


if __name__ == "__main__":
    main()
