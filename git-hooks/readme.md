# Graphics git hooks

## Installation

This folder contains git hooks for the Graphics repository. 
They can be installed by running `unix-install.sh` or `windows-install.ps1`, depending on your OS. Running the powershell script on windows requires administrator rights to create the symbolic links.

It will (recursively) create a symbolic link of each file present in this folder and put it in the `.git/hooks/` folder.
This manual installation step is required because the `.git/` folder can't be pushed to the remote repository and therefore automatically be configured for all developers. 

## Contributing

New types of hooks can be added by creating a script file (e.g. `pre-push`) at the root of this folder.
New hooks' logic should be put in a folder (`pre-push.d/`) and be called by the root file corresponding to the hook type so that the root files stay short and new hooks can be added easily.