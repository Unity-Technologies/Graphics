# Tools

## Git-hooks

Git hooks are a way to ensure that certain rules are being followed within a repo. They provide a way to run local checks before pushing code to the remote, allowing developers to catch issues sooner and iterate faster.

For example, compliance with Unity's code convention is mandatory in order to merge a PR into master. While there are server side scripts that will check the code, you can save some time by installing these hooks and ensure to push formatted code to the remote.

### Installation

Follow these steps to install the git hooks before working on the Graphics repository:

1. Requirement A: Install [Python >= 3.5](https://www.python.org/downloads/) and make sure it is accessible in your PATH.
2. Requirement B: Install [pip3](https://pip.pypa.io/en/stable/installing/).
3. Requirement C: Make sure [unity-meta](https://internaldocs.hq.unity3d.com/unity-meta/setup/) is installed and its requirements are fulfilled. It will be used by the format code hook to ensure your code complies with the convention. _Sidenote: it is the same tool used to format C++/trunk code._
4. Requirement D: Make sure you have access to the cds.github.com repositories. Usually this means following [these steps](https://docs.github.com/en/enterprise-server@2.21/github/authenticating-to-github/connecting-to-github-with-ssh) to create and upload an ssh key to [cds.github.com](https://github.cds.internal.unity3d.com/settings/keys).
5. From the root of the repository, run `cd Tools` and `python3 ./hooks_setup.py`.

Note: If you already installed the git hooks (before November 2020), you need to follow the steps above to re-install them. This is required in order to move towards a more scalable and flexible system. _Sidenote: NodeJS and the node_modules folder are no longer required._

### Available hooks

A description of the hooks we currently have is available in the [hooks' library repository](https://github.cds.internal.unity3d.com/theo-penavaire/gfx-automation-tools#available-git-hooks).

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




**Python not found, even if python is installed, "/usr/bin/env: ‘python’: Permission denied"**

Make sure Python (>=3.5) is in your PATH. Commands that can help:
- On windows: `where python3`
- On Unix: `which python3`
- [How to add to the path on Windows10?](https://www.architectryan.com/2018/03/17/add-to-the-path-on-windows-10/)

When running the Python installer on Windows, make sure to check "Add Python to Path"!

If python can't find the `pre-commit` package, make sure the Scripts folder outputted in the error is in the PATH too.

A clean reinstall of Python solves most issues. Make sure to rerun the `hooks_setup.py` script after you reinstall Python.



**Python was not found; run without arguments to install from the Microsoft Store, or disable this shortcut from Settings > Manage App Execution Aliases.**

Run `python` instead of `python3`.
