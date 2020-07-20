# Tools

These tools are mainly to support CI and testing.

## Scripts

- `file_extension_to_lowercase`: Convert all file extensions in the specified folder from uppercase to lowercase (e.g. `file.FBX` will be converted to `file.fbx` as well as its meta file)
	- Prerequisites: Python installed and accessible from the `PATH`
	- Usage: `python ./file_extension_to_lowercase [list of folders to convert]`


## Git hooks

The folder `Tools/git-hooks` contains git hooks for the Graphics repository. 

### Installation

**Prerequisites:**
- [NodeJS](https://nodejs.org/en/) is installed and present in your PATH

**Steps:**

1. At the root of the repo, open a shell and run : 
```
cd Tools
npm install
```
This will add the hooks to your `.git/hooks` folder.

2. Verify that there weren't any error in the log outputted by husky in the terminal.

**Troubleshooting:**

After trying the solutions below, you may want to delete the `Tools/node_modules` folder and run `npm install` again.

- _Cannot read property 'toString' of null ; husky > Failed to install_:
	- `git` is probably not accessible from your `PATH` variable. You'll have to locate the `git` executable on your filesystem and add it to the `PATH` environment variable.

- _Husky requires Node 10_:
	- Your version of NodeJS is outdated (We need at least version 10). You can update it [here](https://nodejs.org/en/download/). Make sure NodeJS is updated, not only npm.

### Available git hooks
- `check-branch-name`: Ensure the current branch is following the convention:
	- All new branches enclosed in a folder (valid name: `folder/my-branch`)
	- All branches in lowercase, except for the enclosing `HDRP` folder (valid names: `HDRP/my-branch`, `something-else/my-branch`)
- `check-shader-includes`: Compare the case sensitivity of the shader includes in the code files to the actual files in the filesystem. Generate a log if it differs.
- `renormalize-files`: Ensure all files are normalized with LF line endings. CRLF line endings are not allowed on the remote.

### Contributing

New git hooks should be added to the `./git-hooks` folder. They have to be linked to husky in the `.huskyrc.js` file.

### Packages

We use the following packages to make the hooks work:
- [husky](https://github.com/typicode/husky) - Easy access to Git hooks from Node scripts/tools.
