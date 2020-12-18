# Purpose
This script generates Yamato job definition files based on configuration/metafiles, making it easier to change the Yamato jobs by (usually) only editing these metafiles.
- Pros:
   - no manual editing means less linter errors, path mismatches etc
   - consistency among all files
   - easy to track changes to .yml files with git diff, since they are in their final form 
   - reduced code duplication and possibility define constants in a single place
- Cons:
   - the higher consistency among files and the reduced code duplication makes introducing exceptions more difficult
   - learning curve

# Structure
- *.yamato/config/* - directory containing configurations (metafiles) for the jobs to be generated, this is where most of the changes to Yamato jobs should be introduced (Input)
- *.yamato/*  - directory containing all the generated job definition files (.yml) (Output)
- *.yamato/ruamel/build.py* - main script, which creates the actual yml files
- *.yamato/ruamel/jobs/* - directory containing all Python modules for the jobs to be generated, which are organized into subdirectories by domains

# Running the script
Script must be run again each time new changes are introduced in the metafiles.
- Install ruamel by `pip install ruamel.yaml` (or `pip3`)
- Run script by `python [.yamato/ruamel/]build.py` (or `python3`)

# Introducing changes
Each time a metafile or a python file is edited, `python build.py` must be run to regenerate the ymls.

### Adding new dependencies to a job
- Go to the corresponding (project) metafile, and add dependencies to the wanted section, usually specified by  `[job_specific_title].dependencies`. See examples in `_abv.metafile` or any project metafiles. There are 3 ways to specify a dependency, examples below:
```
# add dependencies for testplatforms of a specific job configuration
[project: Universal] # optional: if not specified then uses the project this metafile corresponds to
platform: Win
api: Vulkan
build_config: mono
color_space: Linear
test_platforms: # refer by testplatform name
  - playmode
  - playmode_XR
  - ...

# add dependency for the project PR job
- project: Universal
  pr: true

# add dependency for the Nightly project job
- project: Universal
  nightly: true
```

### Adding/removing testplatforms for projects
- Removing: delete the relevant lines in the metafile(s). If any other job had this deleted job as a dependency, the `build.py` will log it as error.
- Adding (see below sections for more detailed testplatform setup): 
  - add the testplatform configuration to project metafile `test_platforms` section same way as existing ones
  - make sure to add the new dependencies to relevant CI jobs: either PR job (`pr.dependencies`) or Nightly job (`nightly.dependencies`) sections in project metafile. PR job runs on every PR and should include necessary coverage. Nightly job should include all other configurations.

### Adding a new project
- For projects within the repository, add a new project metafile, and fill it in accordingly. See existing ones for reference. Running `build.py` will pick up the file and create `.ymls` for you.

### Introducing a completely new platform
- Add the platform with to `_shared.metafile#platforms` similarly to the rest
- Add any utr flags specific to this platform in `_shared.metafile#test_platforms`
- Add a new cmd file under `ruamel/jobs/projects/commands/[platformname].py` (use `platformname_api` if apis require different commandsets) and fill it in similarly to other cmd files
- Import and map this cmd file in `ruamel/jobs/projects/commands/cmd_mapper.py`
- Add the platform to `_editor.metafile` `platforms` and `unity_downloader_components` sections. Right now, also add it to `ruamel/editor_pinning/update_revisions.py` script (update list of platforms, as well the correct `-o` flag to be used by unity-downloader-cli) --- this is temporary, and it will soon be moved to use the metafile as well.

### Changes when branching out
- When branching out (e.g. moving from *master* to *9.x.x/release* branch), the following steps must be done:
  - In *__shared.metafile* change all references: `editors`, `target_editor` (e.g. trunk), `target_branch` (e.g. master), `target_branch_editor_ci` (editor pinning branch)
  - Create the `target_branch_editor_ci` manually via git. Make sure the branch can be created and pushed to the remote, and eventually adjust the *_shared.metafile* accordingly.
  - In *__editor.metafile* change all references: `editor_tracks` (used by editor pinning), `trunk_track` (used by editor pinning), `green_revision_jobs`  
  - In *_packages.metafile* change reference: `publish_all_track`
  - Rename `_green_job_revisions_[track].metafile` and `_latest_editor_versions_[track].metafile` to use the correct track
  - Additional measures: run editor pinning update job and green revisions job manually (see below how), to ensure everything works as expected and to update the files accordingly.
  - Change Yamato API link to correct branch and jobname in `store_green_revisions.py`

### Test platforms and UTR flags:
Test platforms setup:
- There are 3 base test platforms: standalone (build), playmode, editmode. Their corresponding basic UTR flags are found in `config/_shared.metafile`
- Create a custom test platform (when needed) in project metafile by specifying the base with `type`, and rename it to what you need. Then configure any additional UTR flags you need
- You can exclude this testplatform for certain APIs, by referencing its name under `platform.apis -> exclude_test_platforms`

UTR flags setup:
- The base flags for each of the 3 testplatforms are found in `config/_shared.metafile#test_platforms` under `utr_flags`
- To add additional flags for your testplatform, add an `utr_flags` section (same structure as in shared metafile) under the testplatform in your project metafile. For split _build_ jobs, use `utr_flags_build`
- UTR flags specified by `[all]` are applied for all platform/api cases, but the list `[platform_api, platform_api, ...]` can be used specific cases.
- Last flag overwrites preceding one, file priority being that project metafile overwrites shared metafile.
- It is also possible to call UTR multiple times within the same job, see below for _Repeated UTR calls_

Example:
  ```
test_platforms:
  - type: Standalone
    name: Standalone_new
    utr_flags:
      - [all]: --timeout=1200
      - [OSX_Metal]: --timeout=2400
      - [all]: --reruncount=2
      - [Win_DX11, Win_DX12, Win_Vulkan]: --platform=StandaloneWindows64
      - [Linux_OpenGlCore, Linux_Vulkan]: --platform=StandaloneLinux64
    utr_flags_build: # settings for build job
      - [all]: --timeout=4000
      - [iPhone_Metal]: --timeout=5000
platforms:
  - name: iPhone
    apis:
      - name: Metal
        exclude_test_platforms:
          - name: playmode
  ```

#### Repeated UTR runs
- You can run UTR multiple times within a single job by specifying `utr_repeat` section under a test_platform in project metafile, and specifying the UTR flags used for each run. Each block corresponding to a list item (specified by `-`) corresponds to one UTR run. Use `utr_flags_build` section only for split build jobs.
- If this `utr_repeat` section is not specified, then normal flow applies (utr is called once).
- The `apply` section corresponds to list of platforms/apis for which this repeated block applies.
```
test_platforms:
  - type: Standalone
    name: Standalone_new
    utr_flags:
      - [all]: --timeout=1000
      - [iPhone_Metal]: --timeout=2400
    utr_flags_build:
      - [iPhone_Metal]: --extra-editor-arg="-buildtarget" --extra-editor-arg="iOS"
    utr_repeat:
      - apply: [iPhone_Metal] 
        utr_flags:
        - [iPhone_Metal]: --player-load-path=playersLow
        utr_flags_build:
        - [all]: --testfilter=Low
        - [iPhone_Metal]: --player-save-path=playersLow
      - apply: [Android_Vulkan, Android_OpenGLES3, Win_DX11, Win_DX12, Win_Vulkan]
        utr_flags:
        - [Android_Vulkan, Android_OpenGLES3]: --player-load-path=playersMedium
        - [Win_DX11, Win_DX12, Win_Vulkan]: --player-load-path=../../playersMedium
        utr_flags_build:
        - [all]: --testfilter=Medium
        - [Android_Vulkan, Android_OpenGLES3]: --player-save-path=playersMedium
        - [Win_DX11, Win_DX12, Win_Vulkan]: --player-save-path=../../playersMedium
  ```

# General setup

## Variables
Some jobs benefit from Yamato variables, which can be edited in Yamato UI. 
- `CUSTOM_REVISION`: used by all custom revision jobs, specifies against which unity revision to run the job
- `TEST_FILTER`: used by project jobs (incl _All [project>] CI_), and applies for all standalone build/playmode/editmode jobs, for which the testfilter is not hardcoded in the metafile. Default value is `.*` (run all tests). To check if a testfilter is hardcoded or the variable is used, simply check if the `utr` command in the job yml references the variable or not.


## Python quickstart:
- Each platform has its own commands file under `ruamel/jobs/projects/commands/{platform}.py`, where you can edit the command block. These command files are mapped in `ruamel/jobs/projects/commands/cmd_mapper.py`
- Each file under `ruamel/jobs/[domain]/[job].py` corresponds to one type of Yamato job. For projects there are 3 types: standalone, standalone build, and not_standalone (editmode/playmode). Each domain also has its own `yml_[domain].py` which stores the looping logic over configurations and jobs for this domain (eg looping over editors/platforms etc.), and then writes the ymls to files (this file essentially contains all the for-loops as in the previous liquid yml system)
- `ruamel/jobs/shared` contains all shared scripts, constants etc.


## Editor priming vs editor pinning
- Editor priming:
    - Gets the editor in a separate job to save on the compute resources, stores the editor version in a .txt file which is then picked up by the parent job which calls unity-downloader-cli
    - Still used for custom-revision jobs, because we don't want to hold on to expensive compute resources the job itself requires, while waiting for the editor 
- Editor pinning:
    - Updates editor revisions (`_latest_editor_versions_[track].metafile`) on a nightly basis. Green ABV requirement is toggleable, and is currently turned off (i.e. updates happen no matter ABV state) 
    - There are 3 types of revisions retrieved from _unity-downloader-cli_: `staging` corresponds to `--fast`, `latest_public` corresponds to `--published-only`, and `latest_internal` corresponds to no flags. Currently trunk uses `latest_internal`, rest use `staging` 
    - Workflow in short:
        - Update job runs nightly on target-branch. It merges target-branch into ci-branch (syncs), gets new revisions for all tracks and pushes these to ci branch
        - Merge job is triggered on changes to editor version files on ci-branch. It runs a merge job per each track, which (if the ABV with updated revisions passes green) pushes the corresponding editor revisions file to target-branch. 
    - Workflow in details is on figure below. Some things have changed since that figure was created: (1) we now use a single track per branch, not double. (2) ABV dependency is turned off
    ![Editor pinning flow](editor_pinning.png)

- Running editor pinning locally:
  - Make sure you have the latest version of unity-downloader-cli
  - Update job: `python .yamato\ruamel\editor_pinning\update_revisions.py --target-branch [localbranch] --local`
    - _--local_ flag specifies that no git pull/push/commit gets executed
    - _--target-branch_ would usually correspond to CI branch, but when running locally, just set it to the one you have checked out locally
    - This job updates `_latest_editor_versions.metafile` locally, and also runs `build.py` again to regenerate all ymls with the updated revisions. You can either keep all of the latest revisions, or only the ones you want, and rerun ymls. Once ready, merge like normal PR (i.e. no need to run the merge_revisions job)
  - Merge job: `python .yamato\ruamel\editor_pinning\merge_revisions.py --target-branch [targetbranch] --local --revision [git sha] --track [editortrack]` 
    - _--local_ flag skips checkout/pull of the target branch (but still makes commit on the currently checkout branch, if there is something to commit)
    - _--target-branch_ the target branch into which the revisions get merged to from the ci branch (after jobs passed on ci branch, when CI context used). But due to the local flag, this branch won't get checked out/pulled.
    - _--revision_ the git SHA of the updated revisions commit (the one made on the ci branch by update job). The job runs `git diff HEAD..[revision] -- [path]`, i.e. diff between the current checked out branch vs that SHA (revision). (The _path_ corresponds to yml files or the latest editor versions metafile, but this is already setup within the job). Therefore the merge job only cares about these two paths, and will not merge other changes. This works, because in general, if merge job gets triggered, then CI branch is 1 commit ahead of target branch (which is the updated revisions commit).
    - _--track_ specifies which editor track the merge job runs for (i.e which editor file it aims to merge)
    - In general there is no need to run this file locally. It is only handy when wanting to test the script for syntax errors/functionality etc.

## Storing green revisions for jobs
This job runs nightly (7am) and uses the `_latest_editor_versions_[track].metafile` together with the most recent nightly. For every job in `_editor.metafile#green_revision_jobs`, if it was successful in the nightly job, it updates the corresponding revision section in `_green_job_revisions_[track].metafile`.
- Run it locally by `python .yamato/ruamel/editor_pinning/store_green_revisions.py --target-branch [any branch] --track trunk  --apikey [apikey] --local`
    - _--local_ flag specifies that no git pull/push/commit gets executed
    - _--target-branch_ would usually correspond to CI branch, but when running locally, just set it to the one you have checked out locally
    - _--track_ specifies the track to use (required to match job names, editor versions file, green revisions file, and nightly)

## Current workflow setup
- There are project spefic CI jobs:
    - `[project] PR Job` is triggered on PRs (according to which files were changes), and contains necessary coverage for this project
    - `Nightly [project]` is runs nightly and contains the PR job + all other jobs for this project
- General `_Nightly ABV` contains all project nightlies + some additional jobs 

# FAQ
- What are smoke tests? Blank Unity projects containing all SRP packages (and default packages) to make sure all packages work with each other
- Why does OpenGLCore not have standalone? Because the GPU is simulated and this job is too resource heavy for these machines
- How to UTR flags work? UTR flag order is preserved while parsing in metafiles, and shared metafile is parsed before project metafile. Thus, if shared metafile has `[all]: --timeout=1200, [Win_DX11, Win_DX12]: --timeout=3000` and project metafile has `[Win_DX11]: --timeout=5000`, this will result in DX11 having 5000, DX12 having 3000, and everything else 1200. Note that flags end up alphabetically sorted in the final ymls.


# Configuration file examples (metafiles)

### _shared.metafile: specifying editors
```
# editors applied for all yml files (overridable) (bunch of examples)
editors: 
  # run editor pinning for trunk, and set up a recurrent nightly and weekly
  - track: trunk # unity track: trunk, 2020.2, ....
    name: trunk # name used in job ids, useful if editor pinning is not used and e.g. 2019.4 and fast-2019.4 are used instead
    rerun_strategy: on-new-revision
    editor_pinning: True  # use editor pinning for this track
    editor_pinning_use_abv: False # green ABV is not required for editor pinning
    nightly: True  # add recurrent trigger for the _Nightly job
    weekly: True  # add recurrent trigger for the _Weekly job 
    abv_pr: True  # trigger ABV on PRs 
    fast: # use this if editor pinning is turned off and editor should be used with --fast flag
```


### {project_name}.metafile: project jobs configuration
If the project is just a high-level job only consisting of dependencies, then `project.folder`, `test_platforms`, and `platforms` can be left out (i.e. you only need to specify `project.name` and `all.dependencies`).
```
# project details
project:
  name: project_name # e.g. Universal
  folder: project_folder # e.g. UniversalGraphicsTest
  folder_standalone: project_folder_standalone # use this if standalone is in different folder, like for HDRP currently

# test platforms to generate jobs for
test_platforms:
  ... # see above

# platforms to use (platform details obtained from __shared.metafile)
# platforms can be overridden by using the same structure from shared
platforms:
  - name: Win
    apis:
      - name: DX11
        exclude_test_platforms: # exclude testplatforms for this specific api by referencing their name
          - name: editmode
      - name: DX12
      - name: Vulkan
    build_configs: # specify build configs for this platform by their name in shared.metafile
      - name: il2cpp_apiNet4
      - name: mono_apiNet2
    color_spaces: # specify color spaces 
      - Linear
      - Gamma
  - name: OSX
    ...
....
# see above sections on how to add dependencies
pr: # jobs to run under PR job
  dependencies:
    - ...  
nightly: # jobs to run nightly
  dependencies:
    - ...
```



