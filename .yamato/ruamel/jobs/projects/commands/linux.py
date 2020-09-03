from ...shared.constants import TEST_PROJECTS_DIR,PATH_UNITY_REVISION, PATH_TEST_RESULTS, UNITY_DOWNLOADER_CLI_URL, UTR_INSTALL_URL

def _cmd_base(project_folder, components):
    return [ 
        f'sudo -H pip install --upgrade pip',
        f'sudo -H pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
        f'curl -s {UTR_INSTALL_URL} --output {TEST_PROJECTS_DIR}/{project_folder}/utr',
        f'chmod +x {TEST_PROJECTS_DIR}/{project_folder}/utr',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && sudo unity-downloader-cli --source-file ../../{PATH_UNITY_REVISION} {"".join([f"-c {c} " for c in components])} --wait --published-only'
    ]


def cmd_not_standalone(project_folder, platform, api, test_platform_args):
    base = _cmd_base(project_folder, platform["components"])
    base.extend([ 
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && DISPLAY=:0.0 ./utr {test_platform_args} --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS}'
     ])
    base[-1] += f' --extra-editor-arg="{api["cmd"]}"' if api["name"] != ""  else ''
    return base

def cmd_standalone(project_folder, platform, api, test_platform_args):
    base = _cmd_base(project_folder, platform["components"])
    base.extend([
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && DISPLAY=:0.0 ./utr {test_platform_args}Linux64 --extra-editor-arg="-executemethod" --extra-editor-arg="CustomBuild.BuildLinux{api["name"]}Linear" --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS}'
      ])
    return base

def cmd_standalone_build(project_folder, platform, api, test_platform_args):
    raise Exception('linux: standalone_split set to true but build commands not specified')

