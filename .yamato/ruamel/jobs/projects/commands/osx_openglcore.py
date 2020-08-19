from ...shared.constants import TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS, PATH_PLAYERS, UNITY_DOWNLOADER_CLI_URL, UTR_INSTALL_URL, get_unity_downloader_cli_cmd

def _cmd_base(project_folder, platform, editor):
    return [
        f'curl -s {UTR_INSTALL_URL} --output {TEST_PROJECTS_DIR}/{project_folder}/utr',
        f'chmod +x {TEST_PROJECTS_DIR}/{project_folder}/utr',
        f'pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"], cd=True) } {"".join([f"-c {c} " for c in platform["components"]])} --wait --published-only'
    ]


def cmd_not_standalone(project_folder, platform, api, test_platform_args, editor):
    base = _cmd_base(project_folder, platform, editor)
    base.extend([ 
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && ./utr {test_platform_args} --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS}{_get_extra_utr_arg(project_folder)}'
    ])
    return base

def cmd_standalone(project_folder, platform, api, test_platform_args, editor):
    # base = _cmd_base(project, platform)
    # base.extend([
    #     f'cd {TEST_PROJECTS_DIR}/{project_folder} && utr/utr {test_platform_args}OSX --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS} --timeout=1200 --player-load-path={PATH_PLAYERS} --player-connection-ip=auto'
    # ])
    # return base
    raise Exception("OSX_OpenGlCore standalone should not be called")

def cmd_standalone_build(project_folder, platform, api, test_platform_args, editor):
    # base = _cmd_base(project, platform)
    # base.extend([
    #     f'cd {TEST_PROJECTS_DIR}/{project_folder} && utr/utr {test_platform_args}OSX --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS} --timeout=1200 --player-save-path={PATH_PLAYERS} --build-only'
    # ])
    # return base
    raise Exception("OSX_OpenGlCore standalone should not be called")


def _get_extra_utr_arg(project_folder):
    return ' --compilation-errors-as-warnings' if project_folder.lower() in ['universalhybridtest', 'hdrp_hybridtests'] else ''
