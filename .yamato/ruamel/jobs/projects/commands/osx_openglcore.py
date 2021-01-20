from ...shared.constants import TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS, PATH_PLAYERS, UNITY_DOWNLOADER_CLI_URL, UTR_INSTALL_URL,get_unity_downloader_cli_cmd
from ...shared.utr_utils import get_repeated_utr_calls

def _cmd_base(project, platform, utr_calls, editor):
    base = [
        f'curl -s {UTR_INSTALL_URL} --output {TEST_PROJECTS_DIR}/{project["folder"]}/utr',
        f'chmod +x {TEST_PROJECTS_DIR}/{project["folder"]}/utr',
        f'brew tap --force-auto-update unity/unity git@github.cds.internal.unity3d.com:unity/homebrew-unity.git',
        f'brew install unity-downloader-cli',
        f'cd {TEST_PROJECTS_DIR}/{project["folder"]} && unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"],cd=True) } {"".join([f"-c {c} " for c in platform["components"]])} --wait --published-only',
    ]

    for utr_args in utr_calls:
        base.append(f'cd {TEST_PROJECTS_DIR}/{project["folder"]} && ./utr {" ".join(utr_args)}')
    
    return base


def cmd_editmode(project, platform, api, test_platform, editor, build_config, color_space):
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project["folder"])
    return _cmd_base(project, platform, utr_calls, editor)


def cmd_playmode(project, platform, api, test_platform, editor, build_config, color_space):
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project["folder"])
    return _cmd_base(project, platform, utr_calls, editor)

def cmd_standalone(project, platform, api, test_platform, editor, build_config, color_space):
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project["folder"])
    return _cmd_base(project, platform, utr_calls, editor)

def cmd_standalone_build(project, platform, api, test_platform, editor, build_config, color_space):
    raise NotImplementedError('osx_metal: standalone_split set to true but build commands not specified')