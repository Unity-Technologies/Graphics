from ...shared.constants import TEST_PROJECTS_DIR,PATH_UNITY_REVISION, PATH_TEST_RESULTS

def _cmd_base(project_folder, components):
    return [ 
        f'sudo -H pip install --upgrade pip',
        f'sudo -H pip install unity-downloader-cli --extra-index-url https://artifactory.internal.unity3d.com/api/pypi/common-python/simple --upgrade',
        f'git clone git@github.cds.internal.unity3d.com:unity/utr.git {TEST_PROJECTS_DIR}/{project_folder}/utr',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && sudo unity-downloader-cli --source-file ../../{PATH_UNITY_REVISION} {"".join([f"-c {c} " for c in components])} --wait --published-only'
    ]


def cmd_not_standalone(project_folder, platform, api, test_platform_args):
    base = _cmd_base(project_folder, platform["components"])
    base.extend([ 
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && DISPLAY=:0.0 utr/utr {test_platform_args} --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS}'
     ])
    base[-1] += f' --extra-editor-arg="{platform["apis"][api]}"' if (api != "" and platform["apis"][api] != None)  else ''
    return base

def cmd_standalone(project_folder, platform, api, test_platform_args):
    base = _cmd_base(project_folder, platform["components"])
    base.extend([
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && DISPLAY=:0.0 utr/utr {test_platform_args}Linux64 --extra-editor-arg="-executemethod" --extra-editor-arg="CustomBuild.BuildLinux{api}Linear" --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS}'
      ])
    return base

def cmd_standalone_build(project_folder, platform, api, test_platform_args):
    raise Exception('linux: standalone_split set to true but build commands not specified')

