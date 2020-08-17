#!/usr/bin/env python3

import sys, os, json, argparse


def update_dependencies(project, file_name, target_dependency, target_version):
    for subdir, dirs, files in os.walk(project):
        for file in files:
            if file == file_name:
                path = os.path.join(subdir, file)
                with open(path, "r") as f:
                    json_content = json.load(f)
                dependencies = json_content['dependencies']
                if target_dependency in dependencies:
                    print(f'{file_name} - {file}')
                    dependencies[target_dependency] = target_version
                    with open(path, "w") as json_file:
                        json.dump(json_content, json_file, indent=2)


if __name__ == "__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument('--template-name', help='Name of the template to bump dependencies of')
    parser.add_argument('--target-dependency', help='Name of the target dependency')
    parser.add_argument('--target-version', help='Name of the target version')
    args=parser.parse_args()
    if not args.template_name or not args.target_dependency or not args.target_version:
        parser.print_usage()
        exit(0)
    else:
        update_dependencies(args.template_name, "manifest.json", args.target_dependency, args.target_version)
        update_dependencies(args.template_name, "package.json", args.target_dependency, args.target_version)
