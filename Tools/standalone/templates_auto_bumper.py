#!/usr/bin/env python3

# SRP templates auto bumper. 
# It updates the Packages/manifest.json & Packages/xxx/package.json files of the xxx template.

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
                    dependencies[target_dependency] = target_version
                    with open(path, "w") as json_file:
                        json.dump(json_content, json_file, indent=2)
                        print(f'Bumped {target_dependency} of {project} - {file_name} to {target_version}.')
                else:
                    print(f'Did not find {target_dependency} for {project} - {file_name}. Could no bump this dependency.', file=sys.stderr)


def get_dependency_last_version(target_dependency):
    for subdir, dirs, files in os.walk(target_dependency):
        for file in files:
            if file == "package.json":
                path = os.path.join(subdir, file)
                with open(path, "r") as f:
                    json_content = json.load(f)
                last_version = json_content['version']
                print(f'Last version of {target_dependency} is {last_version}.')
                return last_version
    return None


if __name__ == "__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument('--template-name', help='Name of the template to bump dependencies of')
    parser.add_argument('--target-dependency', help='Name of the target dependency')
    args=parser.parse_args()
    if not args.template_name or not args.target_dependency:
        parser.print_usage()
        exit(0)
    else:
        target_version = get_dependency_last_version(args.target_dependency)
        if target_version == None:
            print(f'Could not find the last version of {args.target_dependency}. There is likely an error with the templates auto bumper script.', file=sys.stderr)
            exit(1)
        update_dependencies(args.template_name, "manifest.json", args.target_dependency, target_version)
        update_dependencies(args.template_name, "package.json", args.target_dependency, target_version)
