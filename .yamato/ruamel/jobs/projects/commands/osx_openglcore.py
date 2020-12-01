from ...shared.constants import TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS, PATH_PLAYERS, UNITY_DOWNLOADER_CLI_URL, UTR_INSTALL_URL,get_unity_downloader_cli_cmd
from ...shared.utr_utils import get_repeated_utr_calls

def _cmd_base(project_folder, platform, utr_calls, editor):
    base = [
        f'curl -s {UTR_INSTALL_URL} --output {TEST_PROJECTS_DIR}/{project_folder}/utr',
        f'chmod +x {TEST_PROJECTS_DIR}/{project_folder}/utr',
        f'pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"],cd=True) } {"".join([f"-c {c} " for c in platform["components"]])} --wait --published-only',
    ]

    for utr_args in utr_calls:
        base.append(f'cd {TEST_PROJECTS_DIR}/{project_folder} && ./utr {" ".join(utr_args)}')
    
    return base


def cmd_editmode(project_folder, platform, api, test_platform, editor, build_config, color_space):
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project_folder)
    return _cmd_base(project_folder, platform, utr_calls, editor)


def cmd_playmode(project_folder, platform, api, test_platform, editor, build_config, color_space):
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project_folder)
    return _cmd_base(project_folder, platform, utr_calls, editor)

def cmd_standalone(project_folder, platform, api, test_platform, editor, build_config, color_space):
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project_folder)
    return _cmd_base(project_folder, platform, utr_calls, editor)

def cmd_standalone_build(project_folder, platform, api, test_platform, editor, build_config, color_space):
    raise NotImplementedError('osx_metal: standalone_split set to true but build commands not specified')