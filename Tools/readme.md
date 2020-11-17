# Tools

## Git-hooks

Git hooks are a way to ensure that certain rules are being followed within a repo. They provide a way to run local checks before pushing code to the remote, allowing developers to catch issues sooner and iterate faster.

For example, compliance with Unity's code convention is mandatory in order to merge a PR into master. While there are server side scripts that will check the code, you can save some time by installing these hooks and ensure to push formatted code to the remote.

### Installation

Follow these steps to install the git hooks before working on the Graphics repository:

1. Requirement A: Install [Python >= 3.5](https://www.python.org/downloads/) and make sure it is accessible in your PATH.
2. Requirement B: Install [pip3](https://pip.pypa.io/en/stable/installing/). 
3. Requirement C: Make sure [unity-meta](https://internaldocs.hq.unity3d.com/unity-meta/setup/) is installed and its requirements are fulfilled. It will be used by the format code hook to ensure your code complies with the convention. _Sidenote: it is the same tool used to format C++/trunk code._
4. From the root of the repository, run `cd Tools` and `python3 ./hooks_setup.py`. 

Note: If you already installed the git hooks (before November 2020), you need to follow the steps above to re-install them. This is required in order to move towards a more scalable and flexible system. _Sidenote: NodeJS and the node_modules folder are no longer required._

### Available hooks

A description of the hooks we currently have is available on the [hooks' library repository](https://github.cds.internal.unity3d.com/theo-penavaire/gfx-automation-tools#available-git-hooks).

For this repository we have enabled:
- format-code
- check-shader-includes
- file-extension-to-lowercase
- check-branch-name

### check-branch-name - Branch name convention

- All new branches enclosed in a folder (valid name: `folder/my-branch`) 
- All branches in lowercase, except for the enclosing `HDRP` folder (valid names: `HDRP/my-branch`, `something-else/my-branch`)
