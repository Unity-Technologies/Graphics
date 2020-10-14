# Purpose
This script generates Yamato job definition files based on configuration/metafiles, making it easier to change the Yamato jobs by (usually) only editing these metafiles.
- Pros:
   - no manual editing means less linter errors, path mismatches etc
   - consistency among all files
   - easy to track changes to .yml files with git diff, since they are in their final form 
   - reduced code duplication and possibility define constants in a single place
- Cons:
   - the higher consistency among files and the reduced code duplication makes introducing exceptions more difficult

# Structure
- *.yamato/config/* - directory containing configurations (metafiles) for the jobs to be generated, this is where most of the changes to Yamato jobs should be introduced (Input)
- *.yamato/*  - directory containing all the generated job definition files (.yml) (Output)
- *.yamato/ruamel/build.py* - main script, which creates the actual yml files
- *.yamato/ruamel/metafile_parser.py* - helper script to read the metafiles and retrieve according information and/or override keys from ___shared.metafile
- *.yamato/ruamel/jobs/* - directory containing all Python modules for the jobs to be generated, which are organized into subdirectories by domains

# Running the script
Script must be run again each time new changes are introduced in the metafiles.
- Install ruamel by `pip install ruamel.yaml` (or `pip3`)
- Run script inside *ruamel/* directory by `python build.py` (or `python3`)

# Example use cases
The majority of changes are introduced within metafiles (*.yamato/config/\*.metafile*, for details check metafile descriptions below). After introducing changes, the script must be rerun (to clean up current jobs, and fetch the updated metafiles to recreate the jobs)

### ABV related changes (_abv.metafile)
- Add a new project to ABV: add the project name (the one used inside the project’s own metafile, e.g. Universal) under abv.projects 
- Add a new job to Nightly: add the dependency under nightly.extra_dependencies (these dependencies run in addition to ABV)
- Add job to trunk verification: add the dependency under trunk_verification.dependencies

### Project related changes (project_name.metafile)
- Adding a new job to All_{project_name}: add the new job under all.dependencies (this job can also be from a different project) 
- Adding a new platform/api for the project: extend the list under platforms as indicated
- Creating a new project: create a new metafile same way as is done for existing projects. All ymls get created once the script runs
- Use different agent than what is specified in the shared metafile: override the agent as described in the metafile description under platforms section

### Package related changes (_packages.metafile)
- Adding a new package: extend packages list with new package details. The new package jobs get automatically created once the script runs (pack, publish, test, test_dependencies). The package is also automatically included in test_all and publish_all jobs.


### Changes when branching out
- When branching out (e.g. moving from *master* to *9.x.x/release* branch), the following steps must be done:
  - In *__shared.metafile* :
    - Change `editors` section to contain the correct editor versions
    - Change `target_editor` to the target editor version for this branch (this is used e.g. for dependencies of *packages#publish_*, *preview_publish#publish_*  and *preview_publish#wait_for_nightly*) (e.g. for 9.x.x this would correspond to `2020.1`)
    - Change `target_branch` to the current branch (this is used for ci triggers, such as ABV  (*all_project_ci*) jobs) (e.g. for 9.x.x this would correspond to `9.x.x/release`)
  - In *__abv.metafile* :
    - Change `abv.trigger_editors` to the editor against which to trigger the ABV (*all_project_ci*) job (typically `fast-*` editor)  (e.g. for 9.x.x this would correspond to `fast-2020.1`)
    - Change `nightly.allowed_editors` to contain the editors for which to run nightly (*all_project_ci_nightly*) jobs (e.g. for 9.x.x this would correspond to `2020.1`)

### Custom test platforms:
- There are 3 base test platforms to choose from: standalone (build), playmode, editmode. These can be extended by renaming them, and/or adding additional utr on top of existing ones. Their corresponding base UTR flags are found in `ruamel/jobs/shared/utr_utils.py`
- If name not specified, name it set to type. Name is used for creating Yamato job ids and excluding testplatforms. If setting up e.g. two playmode types with different flags, renaming must be used, otherwise (due to matching job id) one job overrides the other.
- If a specific platform requires flags different from what is marked in `utr_utils.py`, they are to be configured in the corresponding platform cmd file. Either _a)_ override flag value with the optional parameters _b)_ cancel the flag by overriding with `None` (make sure the function expects such value for such flag though), or _c)_ append additional platform specific flags to the utr_flags list 
- Exclude testplatforms for platforms by specifying the testplatform NAME (not type) in `__shared.metafile`
- Example: extending the default playmode for a specific project performance tests (this takes base playmode flags, and appends these for all platforms, unless specified otherwise in platform cmd file.) Note: when adding extra args to a standalone job, build flags can be specified separately by `extra_utr_flags_build` (scroll down to see project metafile docs)
  ```
    - type: playmode
      name: playmode_perf_build
      extra_utr_flags:
        - --scripting-backend=il2cpp
        - --timeout=1200
        - --performance-project-id=URP_Performance
        - --testfilter=Build
        - --suite=Editor
  ```
  If this platform should not be included eg for IPhone, then specify it in `__shared.metafile` like
  ```
  iPhone:
    name: iPhone
    os: ios
    apis:
      - name: Metal
        exclude_test_platforms:
        - editmode
        - ...
        - playmode_perf_build
  ```


### Other changes to metafiles
- All files follow a similar structure and changes can be done according to the metafile descriptions given below. 

### Changes within Python
- Creating a new job: 
    - Create a new job file under a domain/, same way as existing jobs are defined. 
    - Each domain subdirectory contains a file *yml_domain.py* with a function that loops over everything defined in a metafile, and stores all the created yml jobs for this domain, and then returns a dictionary with *(key,value)* pairs of *(file_path,yml_content)* respectively. Add the newly created job into this function and make sure it is included in this dictionary with its filepath as the key.
    - When the script runs, it will dump the new job along with the rest of the jobs in this dictionary into their respective files.
- Changing constants, variables, paths, ids, etc: all changes should be introduced in either shared/namer.py or shared/constants.py
- Extending the YAMLJob building block class: if new functionality is needed, e.g. a new section under any job file is needed, define it as a function under shared/yml_job.py class.
- Changing to using split test/build for Standalone: under jobs/projects/commands/_cmd_mapper.py change the reference to which set of commands to use. For instance, to switch from Linux to Linux split, change under linux section all linux.cmd_* to linux_split.cmd_*. This simply uses the different set of commands, and the project job definition will automatically create split test/build if split commandset is used, and vice versa.

#### Python structure explanation for projects
- Project jobs are defined by 3 job definition files: **standalone** (contains standalone_build job if split commandset is used), **standalone_build** (build job for standalone tests), **not_standalone** (editmode, playmode, playmode_xr)
- Because all jobs follow the same structure no matter which platform/api is used, with only the commands (and the agent) being different, then commands are obtained from files under jobs/projects/commands/{platform}.py by the job definition class. 
    - Each of these files has commands specific to its platform. If commands differ also per api, like for OSX, then {platform}_{api}.py format is used. 
    - Each of these files contains functions for 3 commandsets (for standalone, standalone_build, not_standalone), which are then used according to which job is being created. 
    - The mapping of which commands to use for which platform is done under _cmd_mapper.py. This also makes it easy to switch the set of commands for a specific platform, such as to switch to new split built/test, without completely losing the old solution.

# FAQ

- How is Nightly ABV set up (all_project_ci_nightly)? Nightly contains the normal ABV (all_project_ci), smoke tests, plus any additional jobs specified in the _abv.metafile under nightly extra dependencies.
- What are smoke tests? Blank Unity projects containing all SRP packages (and default packages) to make sure all packages work with each other
- Why does OpenGLCore not have standalone? Because the GPU is simulated and this job is too resource heavy for these machines

# Configuration files (metafiles)

### __shared.metafile: contains configurations shared across all Yamato jobs (.i.e the central configuration file).
```
# main branch for ci triggers etc
target_branch: master 

# target editor version used for this branch 
target_editor: trunk

# editors applied for all yml files (overridable) (list)
editors: 
  - version: trunk
    rerun_strategy: always
    cmd: -u trunk # used only by editor job
  - ...

# specifies platform details for each platform 
platforms:
  Win:
    name: Win
    os: windows
    apis:
      - name: DX11
        cmd: -force-d3d11
      - name: DX12
        cmd: -force-d3d12
      - name: Vulkan
        cmd: -force-vulkan
    components:
      - editor
      - il2cpp
    agents_project: # agents used by all Windows project jobs (if apis use different agents, postfix this section with api. See OSX example)
      default: # default agent is used when no specific test platform agent is specified
        type: Unity::VM::GPU
        image: sdet/gamecode_win10:stable
        flavor: b1.large
      standalone_build: 
        type: Unity::VM
        image: sdet/gamecode_win10:stable
        flavor: b1.xlarge
      editmode: 
        type: Unity::VM
        image: sdet/gamecode_win10:stable
        flavor: b1.large
      playmode:
        type: Unity::VM
        image: sdet/gamecode_win10:stable
        flavor: b1.large
      playmode_xr: 
        type: Unity::VM
        image: sdet/gamecode_win10:stable
        flavor: b1.large
    agent_package: # used for package/template related jobs
      type: Unity::VM
      image: package-ci/win10:stable
      flavor: b1.large
    copycmd: copy upm-ci~\packages\*.tgz .Editor\Data\Resources\PackageManager\Editor # used for package/template jobs
    editorpath: .\.Editor # used for package/template jobs
  OSX:
    name: OSX
    os: macos
    apis:
      - name: Metal
      - name: OpenGLCore
        exclude_test_platforms: # specify which test platforms to exclude for this api
          - Standalone
    components:
      - editor
      - il2cpp
    agents_project_Metal:  # agents used by all OSX Metal project jobs
      default:
        type: Unity::metal::macmini
        image: slough-ops/macos-10.14-xcode:stable
        flavor: m1.mac
    agents_project_OpenGLCore: # agents used by all OSX OpenGLCore project jobs
      default:
        type: Unity::VM::osx
        image: package-ci/mac:stable
        flavor: m1.mac
    agent_package: 
      type: Unity::VM::osx
      image: package-ci/mac:stable
      flavor: m1.mac
    copycmd: cp ./upm-ci~/packages/*.tgz ./.Editor/Unity.app/Contents/Resources/PackageManager/Editor
    editorpath: "$(pwd)/.Editor/Unity.app/Contents/MacOS/Unity"
  Linux:
    ...

# agents used by package, template etc jobs (dict)
non_project_agents:
  cds_ops_ubuntu_small:
    type: Unity::VM
    image: cds-ops/ubuntu-16.04-base:stable
    flavor: b1.small  
  package_ci_ubuntu_large:
    type: Unity::VM
    image: package-ci/ubuntu:stable
    flavor: b1.large
  sdet_win_large_gpu:
    type: Unity::VM::GPU
    image: sdet/gamecode_win10:stable
    flavor: b1.large
  ...
```





### _abv.metafile: contains configurations for ABV jobs
```
abv: # all_project_ci (ABV) job configuration 
  trigger_editors: # editor(s) for which to create a PR trigger
    - fast-trunk
  projects: # projects to include in ABV by calling All_{project} jobs
    - name: Universal
    - name: Universal_Stereo
    - ...

nightly: # all_project_ci_nightly job configuration
  allowed_editors: # editor(s) for which to create nightly jobs
    - trunk
  extra_dependencies: # project jobs to run in addition to ABV
    - project: Universal # use this format to run a specific job
      platform: Android
      api: OpenGLES3
      test_platforms:
        - Standalone
    - project: HDRP_Hybrid # use this format to run an All_{project} job
      all: true  
    - ...  

smoke_test: # smoke tests configuration. Agents refer back to __shared.metafile
  folder: SRP_SmokeTest
  agent: sdet_win_large # (used for editmode)
  agent_gpu: sdet_win_large_gpu 
  test_platforms: # test platforms to create smoke tests for
    - Standalone
    - playmode
    - editmode

trunk_verification: # jobs to include in trunk verification job
  dependencies:
    - project: Universal
      platform: Win
      api: DX11
      test_platforms:
        - playmode
        - editmode
    - ...

# optionally to override editors from __shared.metafile
override_editors:
  - version: trunk
    rerun_strategy: always
```

### _editor.metafile: configuration for editor priming jobs

```
# all platforms for editor priming jobs
platforms:
  # Exhaustive list of operating systems and editor components used by all jobs so the preparation jobs
  # can make sure all editors are cached on cheap vms before starting the heavy duty machines for running tests
  - name: OSX
  - name: Android
  - name: Win
  - name: Linux
  - name: iPhone
agent: cds_ops_ubuntu_small # agent for editor priming, refers to __shared.metafile

# optionally to override editors from __shared.metafile
override_editors:
  - version: trunk
    rerun_strategy: always
    cmd: -u trunk
```


### _packages.metafile: package jobs configuration
```
# packages to create pack/test/publish jobs for
packages:
  - name: Core
    id: core
    packagename: com.unity.render-pipelines.core
    dependencies:
      - core
  - name: Lightweight
    id: lwrp
    packagename: com.unity.render-pipelines.lightweight
    dependencies:
      - core
      - shadergraph
      - universal
      - lwrp
  - ...

# platforms for test jobs (agents refer to __shared.metafile)
platforms:
  - name: Win
  - name: OSX

# agents specific for pack/publish/publish_all jobs
agent_pack: package_ci_win_large 
agent_publish: package_ci_win_large
agent_publish_all: package_ci_ubuntu_large

# optionally to override editors from __shared.metafile
override_editors:
  - version: trunk

```

### _preview_publish.metafile: preview publish job configurations
```
# publishing variables
publishing: # these are currently commented out and dont work though
  auto_publish: true # if true, publish_all_preview gets daily recurrent trigger
  auto_version: true # if true, auto_version gets branch trigger

# platform dependencies for package pack and publish jobs
platforms:
  - name: Win
  - name: OSX

# package dependencies
packages:
  - name: core
    path: com.unity.render-pipelines.core
    type: package
    publish_source: true # if true, publish and promote jobs are created
    standalone: true
  - ...

# agents for specific jobs,referring to __shared.metafile
agent_promote: package_ci_win_large
agent_auto_version: package_ci_ubuntu_large

# override editors from __shared.metafile file
override_editors:
  - version: trunk
```

### _templates.metafile: template jobs configuration (highly similar for packages configuration)
```
# templates to create jobs for
templates:
  - name: HDRP Template
    id: hdrp_template
    packagename: com.unity.template-hd
    dependencies:
      - core
      - shadergraph
      - vfx
      - config
      - hdrp
    hascodependencies: 1
  - ...

# platforms to run template tests on
platforms:
  - name: Win
  - name: OSX

# agents for specific jobs
agent_pack: package_ci_win_large
agent_test:  package_ci_win_large
agent_all_ci: package_ci_win_large

# optionally to override editors from __shared.metafile
override_editors:
  - version: trunk
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
  - type: Standalone
    extra_utr_flags: # specify additional utr flags to run for this project and test platform
      - --some-extra-utr-flag
    extra_utr_flags_build:
      - --some-extra-utr-flag # additional utr flags for build (only available for standalone type)
    timeout: 3000 # overrides default timeout 1200 for all platforms which use this property in cmd files
    # timeout: # overrides default timeout per platform (for unspecified platforms, default is used)
    #  OSX_Metal: 2400 
    #  Win: 3000 
    timeout_build: 3000 # overrides default timeout 1200 for all platforms which use this property in cmd files (only for split build jobs)
    # timeout_build: # overrides default timeout per platform (for unspecified platforms, default is used)
    #  Win: 3000 
  - type: playmode
  - type: editmode
  - type: playmode # custom testplatform: specify the 'base' type, name it to what you want, and add any additional flags
    name: playmode_XR
    extra_utr_flags:
      - --extra-editor-arg="-xr-tests" 

# platforms to use (platform details obtained from __shared.metafile)
# platforms can be overridden by using the same structure from shared
platforms:
  - name: OSX 
    apis:
      - Metal
      - OpenGLCore
  - name: Linux
    apis: 
      - Vulkan
      - OpenGLCore
  - name: Android
    apis: 
      - Vulkan
      - OpenGLES3
  - name: iPhone
    apis: 
      - Metal
  - name: Win
    apis:
      - DX11
      - DX12
      - Vulkan
  - name: Win
    apis:
      - DX11
      - DX12
      - Vulkan
    ## override example for Win
    # overrides: # allows to override keys under __shared platform section (copycmd, editorpath, agent_package, agents_project)
    #  copycmd: your new copy cmd
    #  editorpath: your new editor path
    #  agents_project:
    #    default:
    #      type: Unity::VM::GPU
    #      image: graphics-foundation/win10-dxr:stable
    #      flavor: b1.xlarge
    #      model: rtx2080
    #    editmode:
    #      type: Unity::VM
    #      image: graphics-foundation/win10-dxr:stable
    #      flavor: b1.xlarge
    #    standalone:
    #      type: Unity::VM::GPU
    #      image: graphics-foundation/win10-dxr:stable
    #      flavor: b1.xlarge
    #      model: rtx2080
    #    standalone_build:
    #      type: Unity::VM
    #      image: graphics-foundation/win10-dxr:stable
    #      flavor: b1.xlarge
    #      model: rtx2080

# which jobs to run under All_{project_name} job
# this is the same structure as in abv nightly extra dependencies
all: 
  dependencies:
    - platform: Win
      api: DX11
      test_platforms:
        - Standalone
        - editmode
        - playmode
        - playmode_XR
    - platform: OSX
      api: Metal
      test_platforms:
        - Standalone
        - playmode 
    - project: HDRP_DXR # use this if there is a dependency to another project
      platform: Win
      api: DX12
      test_platforms:
        - playmode
    - project: HDRP_DXR # use this if there is a dependency to another project
      all: true
    - ...  

# optionally to override editors from __shared.metafile
override_editors:
  - version: trunk
    rerun_strategy: always

```



