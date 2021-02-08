#!/usr/bin/python

# From a set of packages (root of the repository), retrieve all package.json files.
# Collect all unityRelease fields and verify that all package ship with the same one (ie the minimum compatible editor version)
# Store the version in unity_revision.txt

import sys, glob, os, subprocess, json, urllib.request, logging

SUPPORTED_PACKAGES=[
    "com.unity.shadergraph", 
    "com.unity.render-pipelines.core",
    "com.unity.render-pipelines.high-definition",
    "com.unity.render-pipelines.high-definition-config",
    "com.unity.render-pipelines.lightweight",
    "com.unity.render-pipelines.universal",
    "com.unity.visualeffectgraph"
]

def retrieve_unity_release(working_dir):
    unity_release = ""
    for directory in SUPPORTED_PACKAGES:
        package_file_path = os.path.join(working_dir, directory, "package.json")
        with open(package_file_path) as package_file:
            package_data = json.load(package_file)
            tmp_unity_release = package_data["unity"] + "." + package_data["unityRelease"]
            if unity_release != "" and unity_release != tmp_unity_release:
                logging.error('%s is requiring %s as the minimum version while other packages are using %s.', directory, tmp_unity_release, unity_release)
                raise Exception('[ERROR] Not all packages are requiring the same minimum unity version (package.json > unityRelease).')
            else:
                unity_release = tmp_unity_release
    return unity_release


def store_version(tmp_revision_file_path, unity_release):
    tmp_revision_file = open(tmp_revision_file_path, "w")
    tmp_revision_file.write(unity_release)
    tmp_revision_file.close()


def main(tmp_revision_file_path):
    
    logging.info('RUNNING: git rev-parse --show-toplevel')
    working_dir = subprocess.check_output(["git", "rev-parse", "--show-toplevel"])
    working_dir = working_dir.decode('utf-8').rstrip()
    
    logging.info('Working directory: %s', working_dir)
    unity_release = retrieve_unity_release(working_dir)

    logging.info('Unity release used by all the packages: %s', unity_release)
    store_version(os.path.join(working_dir, tmp_revision_file_path), unity_release)


if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO, format='[%(levelname)s] %(message)s')

    if len(sys.argv) < 2:
        logging.error('Usage: python3 %s [tmp_unity_revision_file_path]', sys.argv[0])
        exit(1)
    sys.exit(main(sys.argv[1]))