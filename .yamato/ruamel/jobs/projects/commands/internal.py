from ...shared.constants import TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS, PATH_PLAYERS

def _cmd_base(project, components):
    return [
        f'git clone https://github.cds.internal.unity3d.com/sophia/{project["folder"]}.git TestProjects/{project["folder"]}',
        f'curl -s https://artifactory.internal.unity3d.com/core-automation/tools/utr-standalone/utr.bat --output {TEST_PROJECTS_DIR}/{project["folder"]}/{project["name"]}/utr.bat',
        f'pip install unity-downloader-cli --extra-index-url https://artifactory.internal.unity3d.com/api/pypi/common-python/simple --upgrade',
        f'cd {TEST_PROJECTS_DIR}/{project["folder"]}/{project["name"]}  && unity-downloader-cli --source-file ../../../{PATH_UNITY_REVISION} {"".join([f"-c {c} " for c in components])} --wait --published-only'
    ]


def cmd_not_standalone(project, platform, api, test_platform_args):
    base = _cmd_base(project, platform["components"])
    base.extend([
        f'cd {TEST_PROJECTS_DIR}/{project["folder"]}/{project["name"]} && utr {test_platform_args} --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS}'
    ])
    base[-1] += f' --extra-editor-arg="{platform["apis"][api]}"' if (api != "" and platform["apis"][api] != None)  else ''
    return base

def cmd_standalone(project, platform, api, test_platform_args):
    base = _cmd_base(project, platform["components"])
    base.append('git clone https://github.cds.internal.unity3d.com/sophia/{project["folder"]}.git TestProjects')

    base.extend([
        f'cd {TEST_PROJECTS_DIR}/{project["folder"]}/{project["name"]}   && utr {test_platform_args}Windows64 --artifacts_path={PATH_TEST_RESULTS} --timeout=1200 --player-load-path=../../{PATH_PLAYERS} --player-connection-ip=auto'
    ])
    return base


def cmd_standalone_build(project, platform, api, test_platform_args):
    base = _cmd_base(project, platform["components"])

    base.extend([
        f'cd {TEST_PROJECTS_DIR}/{project["folder"]}/{project["name"]}   && utr {test_platform_args}Windows64 --extra-editor-arg="-executemethod" --extra-editor-arg="CustomBuild.BuildWindows{api}Linear" --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS} --timeout=1200 --player-save-path=../../{PATH_PLAYERS} --build-only'
    ])
    return base