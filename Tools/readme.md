# Tools

These tools are mainly to support CI and testing.

## Standalone scripts

These are supposed to be run as one-off jobs, and can be called from the git-hooks.

-   `file_extension_to_lowercase`: Convert all file extensions in the specified folder from uppercase to lowercase (e.g. `file.FBX` will be converted to `file.fbx` as well as its meta file) 
    - Prerequisites: Python installed and accessible from the `PATH` 
    - Usage: `python ./file_extension_to_lowercase [list of folders to convert]`

## Git hooks

The folder `Tools/git-hook` contains git hooks for the Graphics repository.

### Installation

**Prerequisites:**

-   [NodeJS >= 10](https://nodejs.org/en/) is installed and present in your PATH.
-   [Python >= 3.5](https://www.python.org/downloads/) is installed and present in your PATH.

**Steps:**

1. At the root of the repo, open a shell and run :

```
cd Tools
npm install
```

This will add the hooks to your `.git/hooks` folder.

2. Verify that the installation logs look good in the terminal (no error).

**Troubleshooting:**

After trying the solutions below, you may want to run `npm install` again in the `Tools` folder.

-   _Cannot read property 'toString' of null ; husky > Failed to install_:

    -   `git` is probably not accessible from your `PATH` variable. You'll have to locate the `git` executable on your filesystem and add it to the `PATH` environment variable.

-   _Husky requires Node 10_:

    -   Your version of NodeJS is outdated (We need at least version 10). You can update it [here](https://nodejs.org/en/download/). Make sure NodeJS is updated, not only npm.

-   _Hook already exists: [hook title]_:

    -   If you attempted to install git lfs (`git lfs install`) _after_ installing the hooks, you may have this error. To resolve, run `git lfs update --force` and then re-do a `npm install` in the Tools folder.


### Available git hooks

-   `check-shader-includes` (pre-commit): Compare the case sensitivity of the shader includes in the code files to the actual files in the filesystem. Generate a log if it differs.
-   `check-file-name-extension` (pre-commit): Make sure all files pushed have a lowercase extension so that imports are not broken on Linux.
-   `check-branch-name` (pre-push): Ensure the current branch is following the convention: - All new branches enclosed in a folder (valid name: `folder/my-branch`) - All branches in lowercase, except for the enclosing `HDRP` folder (valid names: `HDRP/my-branch`, `something-else/my-branch`)

### Contributing

New git hooks should be added to the `./git-hook` folder. They have to be linked to husky in the `package.json` file.

### Packages

We use the following packages to make the hooks work:

-   [husky](https://github.com/typicode/husky) - Easy access to Git hooks from Node scripts/tools.
-   [lint-staged](https://github.com/okonet/lint-staged) - Match all staged files to further process them in the git-hooks.
