import argparse
import sys
import os
import json

# place this file in Graphics/TestProjects/ before running


def parse_args(flags):
    parser = argparse.ArgumentParser()

    # yamato job id, find it in the URl before /job
    parser.add_argument("--package-name", required=True)
    parser.add_argument("--new-version", required=True)
    parser.add_argument("--target-folder", default='.')

    args = parser.parse_args(flags)
    return args


def update_manifest(package_name, new_version, target_folder):
    for subdir, dirs, files in os.walk(target_folder):
        for file in files:
            if file == "manifest.json":
                manifest_file = os.path.join(subdir, file)
                with open(manifest_file, "r") as f:
                    manifest_json = json.load(f)
                dependencies = manifest_json['dependencies']
                if package_name in dependencies:
                    dependencies[package_name] = new_version
                    with open(manifest_file, "w") as json_file:
                        json.dump(manifest_json, json_file, indent=4)


if __name__ == '__main__':
    args = parse_args(sys.argv[1:])
    update_manifest(args.package_name, args.new_version, args.target_folder)
