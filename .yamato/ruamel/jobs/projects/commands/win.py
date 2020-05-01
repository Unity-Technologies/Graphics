from ...shared.constants import TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS

def _cmd_base(project, components):
    return [
        f'git clone git@github.cds.internal.unity3d.com:unity/utr.git {TEST_PROJECTS_DIR}/{project["folder"]}/utr',
        f'pip install unity-downloader-cli --extra-index-url https://artifactory.internal.unity3d.com/api/pypi/common-python/simple --upgrade',
        f'cd {TEST_PROJECTS_DIR}/{project["folder"]} && unity-downloader-cli --source-file ../../{PATH_UNITY_REVISION} {"".join([f"-c {c} " for c in components])} --wait --published-only'
    ]


def cmd_not_standalone(project, platform, api, test_platform_args):
    base = _cmd_base(project, platform["components"])
    base.extend([
        f'cd {TEST_PROJECTS_DIR}/{project["folder"]} && utr\\utr {test_platform_args} --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS}'
    ])
    base[-1] += f' --extra-editor-arg="{api["cmd"]}"' if api.get("cmd", None) != None  else ''
    return base

def cmd_standalone(project, platform, api, test_platform_args):
    base = _cmd_base(project, platform["components"])
    base.extend([
        f'cd {TEST_PROJECTS_DIR}/{project["folder"]} && utr\\utr {test_platform_args}Windows64 --artifacts_path={PATH_TEST_RESULTS} --timeout=1200 --player-load-path=../../players --player-connection-ip=auto'
    ])
    return base


def cmd_standalone_build(project, platform, api, test_platform_args):
    base = _cmd_base(project, platform["components"])
    base.extend([
        f'cd {TEST_PROJECTS_DIR}/{project["folder"]} && utr\\utr {test_platform_args}Windows64 --extra-editor-arg="-executemethod" --extra-editor-arg="CustomBuild.BuildWindows{api["name"]}Linear" --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS} --timeout=1200 --player-save-path=../../players --build-only'
    ])
    return base
