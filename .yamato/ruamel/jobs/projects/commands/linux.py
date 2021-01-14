from ...shared.constants import TEST_PROJECTS_DIR,PATH_UNITY_REVISION, PATH_TEST_RESULTS, UNITY_DOWNLOADER_CLI_URL, UTR_INSTALL_URL,get_unity_downloader_cli_cmd, get_timeout
from ...shared.utr_utils import get_repeated_utr_calls, switch_var_sign


def _cmd_base(project, platform, utr_calls, editor):
    base = [ 
        f'curl -L https://artifactory.prd.it.unity3d.com/artifactory/api/gpg/key/public | sudo apt-key add -',
        f'sudo sh -c "echo \'deb https://artifactory.prd.it.unity3d.com/artifactory/unity-apt-local bionic main\' > /etc/apt/sources.list.d/unity.list"',
        f'sudo apt update',
        f'sudo apt install unity-downloader-cli',
        f'curl -s {UTR_INSTALL_URL} --output {TEST_PROJECTS_DIR}/{project["folder"]}/utr',
        f'chmod +x {TEST_PROJECTS_DIR}/{project["folder"]}/utr',
        f'cd {TEST_PROJECTS_DIR}/{project["folder"]} && sudo unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"], cd=True) } {"".join([f"-c {c} " for c in platform["components"]])} --wait --published-only',
    ]
    for utr_args in utr_calls:
        base.append(f'cd {TEST_PROJECTS_DIR}/{project["folder"]} && DISPLAY=:0.0 ./utr {" ".join(utr_args)}')
    return base


def cmd_editmode(project, platform, api, test_platform, editor, build_config, color_space):
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project["folder"])
    base = _cmd_base(project, platform, utr_calls, editor)
    base = add_project_commands(project) + base

    return base


def cmd_playmode(project, platform, api, test_platform, editor, build_config, color_space):
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project["folder"])
    base = _cmd_base(project, platform, utr_calls, editor)
    base = add_project_commands(project) + base

    return base


def cmd_standalone(project, platform, api, test_platform, editor, build_config, color_space):

    base = [
        f'curl -s {UTR_INSTALL_URL} --output {TEST_PROJECTS_DIR}/{project["folder"]}/utr',
        f'chmod +x {TEST_PROJECTS_DIR}/{project["folder"]}/utr'        ]
    
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project["folder"])
    for utr_args in utr_calls:
        base.append(f'cd {TEST_PROJECTS_DIR}/{project["folder"]} && ./utr {" ".join(utr_args)}')
    return base


def cmd_standalone_build(project, platform, api, test_platform, editor, build_config, color_space):
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project["folder"], utr_flags_key="utr_flags_build")

    base = _cmd_base(project, platform, utr_calls, editor)
    base = add_project_commands(project) + base
    return base


def add_project_commands(project):
    cmds = []
    if project.get("url"):
        cmds.extend([
            f'git clone {project["url"]} -b {switch_var_sign(project["branch"])} {TEST_PROJECTS_DIR}/{project["folder"]}',
            f'cd {TEST_PROJECTS_DIR}/{project["folder"]} && git checkout {switch_var_sign(project["revision"])}'
        ])
    if project.get("unity_config_commands"):
        cmds.extend([
            f'curl -L https://artifactory.prd.it.unity3d.com/artifactory/api/gpg/key/public | sudo apt-key add -',
            f'sudo sh -c "echo \'deb https://artifactory.prd.it.unity3d.com/artifactory/unity-apt-local bionic main\' > /etc/apt/sources.list.d/unity.list"',
            f'sudo apt update',
            f'sudo apt install -y unity-config',
        ])
        for unity_config in project["unity_config_commands"]:
            cmds.append(f'cd {TEST_PROJECTS_DIR}/{project["folder"]} && {unity_config}')
    return cmds
