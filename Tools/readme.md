<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
**Table of Contents**

- [Tools](#tools)
  - [Git-hooks](#git-hooks)
    - [Installation](#installation)
    - [Available hooks](#available-hooks)
    - [FAQ and Troubleshooting steps](#faq-and-troubleshooting-steps)
  - [Formatting](#formatting)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

# Tools

_Questions: #devs-graphics-automation_

## Git-hooks

Git hooks are a way to ensure that certain rules are being followed within a repo. They provide a way to run local checks before pushing code to the remote, allowing developers to catch issues sooner and iterate faster.

For example, compliance with Unity's code convention is mandatory in order to merge a PR into master. While there are server side scripts that will check the code, you can save some time by installing these hooks and ensure to push formatted code to the remote.

### Installation

Follow these steps to install the git hooks before working on the Graphics repository:

1. Install [Python >= 3.6](https://www.python.org/downloads/) and make sure it is accessible in your PATH.
2. Install [pip3](https://pip.pypa.io/en/stable/installing/).
3. Make sure [unity-meta](https://internaldocs.hq.unity3d.com/unity-meta/setup/) is installed and its requirements are fulfilled. It will be used by the format code hook to ensure your code complies with the convention. 
  - _Sidenote: it is the same tool used to format C++/trunk code._ 
  - _Sidenote 2: Nowadays unity-meta can be installed using git only, no need to use the mercurial clone anymore. The git repository can be found [here](https://github.cds.internal.unity3d.com/unity/unity-meta)._
4. Make sure you have access to the cds.github.com repositories. Usually this means following [these steps](https://docs.github.com/en/enterprise-server@2.21/github/authenticating-to-github/connecting-to-github-with-ssh) to create and upload an ssh key to [cds.github.com](https://github.cds.internal.unity3d.com/settings/keys).
5. From the root of the repository, run `cd Tools` and `python3 ./hooks_setup.py`.

### Available hooks

A description of the hooks we currently have is available in the [hooks library repository](https://github.cds.internal.unity3d.com/unity/gfx-sdet-tools/blob/master/hooks/readme.md).

For this repository we have enabled:

- format-code
- check-shader-includes
- file-extension-to-lowercase
- check-branch-name

### FAQ and Troubleshooting steps

**How to make sure the hooks are correctly installed?**

This is the output of a successful installation:
```
[INFO]   Running: git lfs install --force
[INFO]   Running: git rev-parse --show-toplevel
[INFO]   Running: pip3 install pre-commit
[INFO]   Running: pre-commit install --allow-missing-config
[INFO]   Running: pre-commit install --hook-type pre-push --allow-missing-config
[INFO]   Running: git rev-parse --show-toplevel
[INFO]   Running: where perl
[INFO]   Running: git ls-remote git@github.cds.internal.unity3d.com:theo-penavaire/gfx-automation-tools.git HEAD
Successfully installed the git hooks. Thank you!
```

Additionally, you can run the following commands to make sure the hooks are triggered on git operations:
```
echo "test" > test.txt
git add test.txt
git commit -m "test"
// Some kind of output about the hooks being run
// Do a reset to undo our test: git reset --soft HEAD~1 (This "undoes" the last commit and keep the committed files in your staging area so delete test.txt after)
``` 

**Permission denied (SSH) when installing the git hooks**

Please, follow these steps: https://docs.github.com/en/enterprise-server@2.21/github/authenticating-to-github/connecting-to-github-with-ssh. Do not forget the ssh agent step.

If that still doesn’t work, try running 
```
ssh -vT git@github.cds.internal.unity3d.com
```
Look for a line starting by “Offering public key...”. It will tell you which ssh key is being used by shh. This key must be the one you uploaded to [github.cds](https://github.cds.internal.unity3d.com/settings/keys).

Last resort: [Troubleshooting SSH section in Github docs](https://docs.github.com/en/enterprise-server@2.21/github/authenticating-to-github/troubleshooting-ssh).


**Python or pre-commit not found, even if python is installed, "/usr/bin/env: ‘python’: Permission denied"**

Make sure Python (>=3.6) is in your PATH. Commands that can help:
- On windows: `where python3`
- On Unix: `which python3`
- [How to add to the path on Windows10?](https://www.architectryan.com/2018/03/17/add-to-the-path-on-windows-10/)

When running the Python installer on Windows, make sure to check "Add Python to Path"!

If python can't find the `pre-commit` package, make sure the Scripts folder outputted in the error is in the PATH too (use `where pre-commit` or `which pre-commit` to check which folder it is in).

A clean reinstall of Python solves most issues. Make sure to rerun the `hooks_setup.py` script after you reinstall Python.


**Python was not found; run without arguments to install from the Microsoft Store, or disable this shortcut from Settings > Manage App Execution Aliases.**

Run `python` instead of `python3`.


**bash: /path/to/AppData/Local/Microsoft/WindowsApps/python: Permission denied**

Follow the suggestions of [this StackOverflow answer](https://stackoverflow.com/questions/56974927/permission-denied-trying-to-run-python-on-windows-10/57168165#57168165).


**Can't locate Win32/Process.pm in @INC...**

On Windows, Active perl is not supported by the formatting tool. Use Strawberry perl. 

**ValueError: '.git' is not in list when running python .\hooks_setup.py**
Your git version is probably outdated. You can check that `git --version` returns a fairly recent version of it. You may have several versions of git on your machine, and the default one is outdated, in which case you'll need to add the path to the most recent one to your PATH variable (add it at the top or beginning of the list so that it takes precedence over any other git version installed on your system).

## Formatting

Provided you installed [unity-meta](https://internaldocs.hq.unity3d.com/unity-meta/setup/), you can manually run the formatting tool with the following command:
```
perl ~/unity-meta/Tools/Format/format.pl --dry-run <folder to format>
```
**Notes for Windows users:**
- Uou may have to manually "expand" the tilde (`~`) sign, meaning replacing it by your $HOME path. In powershell, hit `TAB` with the cursor on the tilde sign to automatically expand it to the $HOME path.
- You may have to run `perl.exe` instead of `perl`.


To actually apply the changes:
```
perl ~/unity-meta/Tools/Format/format.pl --nobackups <folder to format>
```
Use `--help` to discover more useful options (`--preview` will generate a diff file for instance)
