from ...shared.constants import TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS, PATH_PLAYERS

def _cmd_base(project_folder, components):
    return [
        f'curl -s https://artifactory.internal.unity3d.com/core-automation/tools/utr-standalone/utr --output {TEST_PROJECTS_DIR}/{project_folder}/utr',
        f'chmod +x {TEST_PROJECTS_DIR}/{project_folder}/utr',
        f'pip install unity-downloader-cli --extra-index-url https://artifactory.internal.unity3d.com/api/pypi/common-python/simple --upgrade',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-downloader-cli --source-file ../../{PATH_UNITY_REVISION} {"".join([f"-c {c} " for c in components])} --wait --published-only'
    ]


def cmd_not_standalone(project_folder, platform, api, test_platform_args):
    base = _cmd_base(project_folder, platform["components"])
    base.extend([ 
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && utr {test_platform_args} --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS}'
    ])
    return base

def cmd_standalone(project_folder, platform, api, test_platform_args):
    # base = _cmd_base(project, platform["components"])
    # base.extend([
    #     f'cd {TEST_PROJECTS_DIR}/{project_folder} && utr/utr {test_platform_args}OSX --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS} --timeout=1200 --player-load-path={PATH_PLAYERS} --player-connection-ip=auto'
    # ])
    # return base
    raise Exception("OSX_OpenGlCore standalone should not be called")

def cmd_standalone_build(project_folder, platform, api, test_platform_args):
    # base = _cmd_base(project, platform["components"])
    # base.extend([
    #     f'cd {TEST_PROJECTS_DIR}/{project_folder} && utr/utr {test_platform_args}OSX --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS} --timeout=1200 --player-save-path={PATH_PLAYERS} --build-only'
    # ])
    # return base
    raise Exception("OSX_OpenGlCore standalone should not be called")


