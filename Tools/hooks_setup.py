#!/usr/bin/env python3

# Use this script (python hooks_setup.py) to install git hooks from https://github.cds.internal.unity3d.com/theo-penavaire/gfx-automation-tools.

# run_cmd function taken from https://github.com/Unity-Technologies/dots/blob/master/Tools/CI/util/subprocess_helpers.py

import subprocess, logging, os, json, sys
from pathlib import Path

def run_cmd(cmd, cwd=None):
    """Runs a command and returns its output as an UTF-8 string.
    NOTICE: The output normally ends with a newline that might need to be stripped.
    NOTICE: This does not return any stderr if the command succeeds, only on
        non-zero exit code when the error is raised.
    Args:
        cmd: Command line as a string or a list (if any item contains spaces).
        cwd: Working directory to run the command in. If omitted the
            interpreter's working dir will be used.
    Raises: subprocess.CalledProcessError similar to subprocess.check_output.
    """
    if isinstance(cmd, str):
        cmd = cmd.split()
    assert isinstance(cmd, list), 'cmd must be of list type, but was "{}"'.format(type(cmd))
    logging.info("  Running: {0}".format(' '.join(cmd)))
    return subprocess.check_output(cmd, cwd=cwd, universal_newlines=True).rstrip()


def install_git_lfs():
    run_cmd('git lfs install --force')


def replace_shebangs():
    repo_root = run_cmd('git rev-parse --show-toplevel')
    hooks_folder = os.path.join(repo_root, '.git/hooks')
    current_shebang = "#!/bin/sh"
    replacement = "#!/usr/bin/env sh"
    for dname, dirs, files in os.walk(hooks_folder):
        for fname in files:
            fpath = os.path.join(dname, fname)
            with open(fpath, 'r') as f:
                s = f.read()
            s = s.replace(current_shebang, replacement)
            with open(fpath, "w") as f:
                f.write(s)


def install_precommit():
    try:
        run_cmd('pip3 install pre-commit')
    except:
        print('Pip3 is required in order to run the formatting tools. Please install pip3 and retry installing the hooks.', file=sys.stderr)


def install_hooks():
    # Allow missing config in case the user checkouts a branch without the config file.
    run_cmd('pre-commit install --allow-missing-config')
    run_cmd('pre-commit install --hook-type pre-push --allow-missing-config')


# Check perl installation
# (used for code formatting)
def config_perl(config):
    try:
        if sys.platform == "win32" or sys.platform == "cygwin":
            perl_path = run_cmd('where perl')
        else:
            perl_path = run_cmd('which perl')
        config['perl'] = perl_path
        return config
    except subprocess.CalledProcessError as e:
        print(e.output, file=sys.stderr)
        print('Perl is required in order to run the formatting tools. Please install perl and retry installing the hooks.', file=sys.stderr)
        exit(1)


# Fetch unity-meta
# (used for code formatting)
def config_unity_meta(config):
    home = str(Path.home())
    default_unity_meta_path = os.path.join(home, 'unity-meta/')
    if os.path.exists(default_unity_meta_path):
        config['unity-meta'] = default_unity_meta_path
        return config
    else:
        print('unity-meta is required in order to run the formatting tools. Please install it (https://internaldocs.hq.unity3d.com/unity-meta/setup/) and retry installing the hooks. If it is not in your $HOME folder, manually add the path to it in .git/hooks/hooks-config.json', file=sys.stderr)
        exit(1)


def config_hooks():
    repo_root = run_cmd('git rev-parse --show-toplevel')
    config_path = os.path.join(repo_root, ".git/hooks/hooks-config.json")
    config = {}
    if os.path.exists(config_path):
        with open(config_path, 'r') as config_file_r:
            config = json.load(config_file_r)

    ## Add static configuration methods here
    config = config_unity_meta(config)
    config = config_perl(config)

    with open(config_path, 'w') as config_file_w:
        json.dump(config, config_file_w, indent=4, sort_keys=True)


def check_ssh_access():
    try:
      result = run_cmd('git ls-remote git@github.cds.internal.unity3d.com:theo-penavaire/gfx-automation-tools.git HEAD')
    except:
        print('You must register an SSH key with github.cds. Please visit https://github.cds.internal.unity3d.com/settings/keys and reinstall the hooks after uploading the key.', file=sys.stderr)
        exit(1)


def main():
    logging.basicConfig(level=logging.INFO, format='[%(levelname)s] %(message)s')
    install_git_lfs()
    replace_shebangs()
    install_precommit()
    install_hooks()
    config_hooks()
    check_ssh_access()
    print('Successfully installed the git hooks. Thank you!')
    return 0


if __name__ == "__main__":
    exit(main())
