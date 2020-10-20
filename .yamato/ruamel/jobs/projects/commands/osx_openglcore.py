from ...shared.constants import TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS, PATH_PLAYERS, UNITY_DOWNLOADER_CLI_URL, UTR_INSTALL_URL,get_unity_downloader_cli_cmd
from ...shared.utr_utils import utr_editmode_flags, utr_playmode_flags, utr_standalone_split_flags, utr_standalone_build_flags, utr_standalone_not_split_flags

def _cmd_base(project_folder, platform, utr_flags, editor):
    return [
        f'curl -s {UTR_INSTALL_URL} --output {TEST_PROJECTS_DIR}/{project_folder}/utr',
        f'chmod +x {TEST_PROJECTS_DIR}/{project_folder}/utr',
        f'pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"],cd=True) } {"".join([f"-c {c} " for c in platform["components"]])} --wait --published-only',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && ./utr {" ".join(utr_flags)}'
    ]


def cmd_editmode(project_folder, platform, api, test_platform, editor, build_config, color_space):
    scripting_backend = build_config["scripting_backend"]
    api_level = build_config["api_level"]
    utr_args = utr_editmode_flags(scripting_backend=f'{scripting_backend}', api_level=f'{api_level}', color_space=f'{color_space}')
    utr_args.extend(test_platform["extra_utr_flags"])
    utr_args.extend(platform["extra_utr_flags"])
    return  _cmd_base(project_folder, platform, utr_args, editor)


def cmd_playmode(project_folder, platform, api, test_platform, editor, build_config, color_space):
    scripting_backend = build_config["scripting_backend"]
    api_level = build_config["api_level"]

    utr_args = utr_playmode_flags(scripting_backend=f'{scripting_backend}', api_level=f'{api_level}', color_space=f'{color_space}')
    utr_args.extend(test_platform["extra_utr_flags"])
    utr_args.extend(platform["extra_utr_flags"])
    return  _cmd_base(project_folder, platform, utr_args, editor)

def cmd_standalone(project_folder, platform, api, test_platform, editor, build_config, color_space):
    scripting_backend = build_config["scripting_backend"]
    api_level = build_config["api_level"]

    utr_args = utr_standalone_not_split_flags(platform_spec='OSX', scripting_backend=f'{scripting_backend}', api_level=f'{api_level}', color_space=f'{color_space}')
    utr_args.extend(test_platform["extra_utr_flags"])
    utr_args.extend(platform["extra_utr_flags"])
    return  _cmd_base(project_folder, platform, utr_args, editor)

def cmd_standalone_build(project_folder, platform, api, test_platform, editor, build_config, color_space):
    raise NotImplementedError('osx_metal: standalone_split set to true but build commands not specified')