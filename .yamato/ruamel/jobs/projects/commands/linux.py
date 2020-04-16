from ...utils.constants import TEST_PROJECTS_DIR

def _cmd_base(project, components):
    return [ 
        f'sudo -H pip install --upgrade pip',
        f'sudo -H pip install unity-downloader-cli --extra-index-url https://artifactory.internal.unity3d.com/api/pypi/common-python/simple --upgrade',
        f'git clone git@github.cds.internal.unity3d.com:unity/utr.git {TEST_PROJECTS_DIR}/{project["folder"]}/utr',
        f'cd {TEST_PROJECTS_DIR}/{project["folder"]} && sudo unity-downloader-cli --source-file ../../unity_revision.txt {"".join([f"-c {c} " for c in components])} --wait --published-only'
    ]


def cmd_not_standalone(project, platform, api, test_platform_args):
    base = _cmd_base(project, platform["components"])
    base.extend([ 
        f'cd {TEST_PROJECTS_DIR}/{project["folder"]} && DISPLAY=:0.0 utr/utr {test_platform_args} --testproject=. --editor-location=.Editor --artifacts_path=test-results'
     ])
    base[-1] += f' --extra-editor-arg="{api["cmd"]}"' if api.get("cmd", None) != None  else ''
    return base

def cmd_standalone(project, platform, api, test_platform_args):
    base = _cmd_base(project, platform["components"])
    base.extend([
        f'cd {TEST_PROJECTS_DIR}/{project["folder"]} && DISPLAY=:0.0 utr/utr {test_platform_args}Linux64 --extra-editor-arg="-executemethod" --extra-editor-arg="CustomBuild.BuildLinux{api["name"]}Linear" --testproject=. --editor-location=.Editor --artifacts_path=test-results'
      ])
    return base

def cmd_standalone_build(project, platform, api, test_platform_args):
    raise Exception('linux: standalone_split set to true but build commands not specified')

