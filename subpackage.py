import argparse
import os
import json
import textwrap
from http.client import HTTPSConnection
from base64 import b64encode
import subprocess
from pprint import pprint


def main():
    parser = argparse.ArgumentParser()

    parser.add_argument("-f", "--folder", help="the folder containing master-package.json and the sub-package folders",
                        required=True)
    parser.add_argument("-u", "--username",
                        help="bintray username, as in <username>@unity (i.e. don't write the `@unity` part)",
                        required=True)
    parser.add_argument("-k", "--key", help="bintray API key", required=True)
    parser.add_argument("-p", "--publish", action="store_true",
                        help="DANGER: actually do the publish and then clean-up. if not set, only preparation will be "
                             "done for inspection purposes")
    parser.add_argument("-F", "--filter", default="",
                        help="only packages containing the specified string will be published")
    parser.add_argument("-d", "--dirty", action="store_true",
                        help="don't clean-up files")
    parser.add_argument("-s", "--silent", action="store_true",
                        help="don't tell me what's going on here")
    parser.add_argument("--save-npmrc", action="store_true",
                        help="save the downloaded .npmrc file to the current working directory")
    args = parser.parse_args()
    silent = args.silent
    base_folder = os.path.realpath(args.folder)

    def error_print(msg):
        print("[ERROR] {}".format(msg))

    def warning_print(msg):
        print("[WARNING] {}".format(msg))

    def info_print(msg):
        if not silent:
            print(msg)

    info_print("Using folder: {}".format(base_folder))

    file_path = os.path.join(base_folder, "master-package.json")
    if os.path.isfile(file_path):
        info_print("Found master package file: {}".format(file_path))
        with open(file_path) as file:
            try:
                master_package = json.load(file)
            except json.JSONDecodeError as e:
                error_print(e)
                exit(1)

    potential_folders = master_package["subPackages"] if "subPackages" in master_package else []
    sub_packages = {}
    sub_package_folders = {}
    for item in potential_folders:
        file_path = os.path.join(base_folder, item, "sub-package.json")
        if os.path.isfile(file_path):
            info_print("Found sub-package file: {}".format(file_path))
            with open(file_path) as file:
                try:
                    sub_package = json.load(file)
                    sub_packages[sub_package["name"]] = sub_package
                    sub_package_folders[sub_package["name"]
                                       ] = os.path.join(base_folder, item)
                except json.JSONDecodeError as e:
                    error_print("Error: {}".format(e))

    if not sub_packages:
        error_print("Error: No sub-packages found.")
        exit(1)

    if "version" not in master_package:
        error_print("Master package must contain a \"version\" field")
        exit(1)
    info_print("")

    info_print("Propagating master package version to sub-packages")
    for sub_package in sub_packages.values():
        sub_package["version"] = master_package["version"]

    if "unity" in master_package:
        info_print("Propagating master package Unity version to sub-packages")
        for sub_package in sub_packages.values():
            sub_package["unity"] = master_package["unity"]

    if "dependencies" in master_package and master_package["dependencies"]:
        info_print("Propagating shared dependencies:")
        for name, version in master_package["dependencies"].items():
            info_print("  {}@{}".format(name, version))
        for sub_package in sub_packages.values():
            if "dependencies" not in sub_package or not sub_package["dependencies"]:
                sub_package["dependencies"] = {}
            for name, version in master_package["dependencies"].items():
                sub_package["dependencies"][name] = version
    info_print("")

    info_print("Creating dependency tree:")
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
        error_print(
            "Dependency tree is empty. You might have a circular reference.")
        exit(1)

    def print_dependency_tree(tree, indent):
        for key, sub_tree in tree.items():
            info_print(textwrap.indent(key, "  " * indent))
            print_dependency_tree(sub_tree, indent + 1)

    print_dependency_tree(dependency_tree, 1)
    info_print("")

    info_print("Creating publish order:")
    publish_order = []
    visited = set()

    def fill_publish_order(tree):
        for key, sub_tree in tree.items():
            if key not in visited:
                if args.filter in key:
                    publish_order.append(key)
                fill_publish_order(sub_tree)

    fill_publish_order(dependency_tree)
    for name in publish_order:
        info_print("  {}".format(name))
    info_print("")

    info_print("Resolving dependencies between sub-packages:")
    for sub_package in sub_packages.values():
        if "dependencies" not in sub_package or not sub_package["dependencies"]:
            sub_package["dependencies"] = {}
        if "subDependencies" in sub_package and sub_package["subDependencies"]:
            info_print("  {}:".format(sub_package["name"]))
            for sub_dependency in sub_package["subDependencies"]:
                sub_package["dependencies"][sub_dependency] = master_package["version"]
                info_print("    {}@{}".format(sub_dependency,
                                              sub_package["dependencies"][sub_dependency]))
            del sub_package["subDependencies"]
    info_print("")

    info_print("Writing package files:")
    for name in publish_order:
        sub_package = sub_packages[name]
        package_path = os.path.join(sub_package_folders[name], "package.json")
        info_print("  {}:".format(package_path))
        with open(package_path, 'w') as file:
            json.dump(sub_package, file, indent=4)
        with open(package_path) as file:
            info_print(textwrap.indent(file.read(), "    >"))
        info_print("")

    info_print("Downloading npm config:")
    c = HTTPSConnection("staging-packages.unity.com")
    auth = b64encode("{}@unity:{}".format(
        args.username, args.key).encode("ascii")).decode("ascii")
    c.request('GET', '/auth', headers={"Authorization": "Basic %s" % auth})
    res = c.getresponse()
    npm_config = res.read().decode(res.headers.get_content_charset("ascii"))
    print(textwrap.indent(npm_config, "  >"))
    info_print("Writing config to files:")
    for name in publish_order:
        folder = sub_package_folders[name]
        path = os.path.join(folder, ".npmrc")
        with open(path, 'w') as file:
            file.write(npm_config)
            info_print("  {}".format(path))
    if args.save_npmrc:
        path = os.path.join(os.getcwd(), ".npmrc")
        with open(path, 'w') as file:
            file.write(npm_config)
            info_print("  {}".format(path))
    info_print("")

    if args.publish:
        for name in publish_order:
            info_print("Publishing {}:".format(name))
            folder = sub_package_folders[name]
            subprocess.run(["npm", "publish"], cwd=folder, shell=True)
            info_print("")

    if not args.dirty:
        info_print("Removing temporary files:")
        files = []
        for name in publish_order:
            folder = sub_package_folders[name]
            files.append(os.path.join(folder, "package.json"))
            files.append(os.path.join(folder, ".npmrc"))
        for file in files:
            info_print("  {}".format(file))
            os.remove(file)


if __name__ == "__main__":
    main()
