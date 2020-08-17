import sys
import os
import json

# place this file in Graphics/TestProjects/ before running

# enter package to update as first arg
package = sys.argv[1]
# enter new version as second arg
new_version = sys.argv[2]

for subdir, dirs, files in os.walk('.'):
    for file in files:
        if file == "manifest.json":
                manifest_file = os.path.join(subdir, file)
                with open(manifest_file, "r") as f:
                    manifest_json = json.load(f)
                dependencies = manifest_json['dependencies']
                if package in dependencies:
                    dependencies[package] = new_version
                    with open(manifest_file, "w") as json_file:
                        json.dump(manifest_json, json_file, indent=4)