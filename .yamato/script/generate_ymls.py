import os
import subprocess
import ruamel.yaml as yml

# This script calls the ruamel build.py script, with an argument pointing to current GIT repo
# i.e. when this is called within Graphics repo, then build.py will edit the files in Graphics repo 
#
# 1) create path.config file (yml format) in the same directory with this script, with the following content (this file is ignored by git)
# gfx_sdet_tools_path: "[your path to repo checkout using forward slashes]/gfx-sdet-tools"
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
    config_file = os.path.join(current_yamato_dir, 'script', 'path.config')
    gfx_sdet_tools_rev_file = os.path.join(current_yamato_dir, 'script', 'gfx_sdet_tools_revision.txt')
    
    with open(config_file) as f:
        config = yaml.load(f)
    gfx_sdet_tools_dir = config["gfx_sdet_tools_path"]
    build_py = os.path.join(gfx_sdet_tools_dir,'yml-generator','ruamel','build.py')

    cmd = f'python "{build_py}" --yamato-dir "{current_yamato_dir}"'
    print(f'Calling [{cmd}]')
    
    process = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, universal_newlines=True)
    for stdout_line in iter(process.stdout.readline, ""):
        print(stdout_line.strip())
    process.stdout.close()
    process.wait()

    gfx_sdet_tools_rev = subprocess.check_output('git rev-parse HEAD', stderr=subprocess.STDOUT, universal_newlines=True, cwd=gfx_sdet_tools_dir)
    with open(gfx_sdet_tools_rev_file, 'w') as f:
        f.write(gfx_sdet_tools_rev)
    
    print(f'Used gfx-sdet-tools at revision {gfx_sdet_tools_rev}')