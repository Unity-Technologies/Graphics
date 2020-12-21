import os
import subprocess
import ruamel.yaml as yml

# This script calls the ruamel build.py script, with an argument pointing to current GIT repo
# i.e. when this is called within Graphics repo, then build.py will edit the files in Graphics repo 
#
# 1) create .config file (yml format) in the same directory with this script, with the following content
# build_py_path: "[full path to gfx-sdet-tools repo checkout]/.yamato/ruamel/build.py"
# 
# 2) call 
# python generate_ymls.py
#
# 3) new .ymls should be now present in your repo
#
# !! REMEMBER to keep the gfx-sdet-tools repo up-to-date


root_dir = os.path.dirname(os.path.dirname(os.path.abspath(os.path.dirname(__file__))))

if __name__== "__main__":
    yaml = yml.YAML()
    
    current_yamato_dir = os.path.join(root_dir, '.yamato')
    config_file = os.path.join(current_yamato_dir, 'script', '.config')
    
    with open(config_file) as f:
        config = yaml.load(f)
    build_py = config["build_py_path"]

    cmd = f'python "{build_py}" --yamato-dir "{current_yamato_dir}"'
    print(f'Calling [{cmd}]')
    
    process = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, universal_newlines=True)
    for stdout_line in iter(process.stdout.readline, ""):
        print(stdout_line.strip())
    
    process.stdout.close()
    process.wait()