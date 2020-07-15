# Tools

These tools are mainly to support CI and testing.

## Git hooks

The folder `./git-hooks` contains git hooks for the Graphics repository. 

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

### Packages

We use the following packages to make the hooks work:
- [husky](https://github.com/typicode/husky) - Easy access to Git hooks from Node scripts/tools.

## Contributing

New git hooks should be added to the `./git-hooks` folder. They have to be linked to husky in the `.huskyrc.js` file.