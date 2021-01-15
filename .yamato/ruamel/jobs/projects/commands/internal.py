from ...shared.constants import TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS, PATH_PLAYERS, GITHUB_CDS_URL, UNITY_DOWNLOADER_CLI_URL, UTR_INSTALL_URL,get_unity_downloader_cli_cmd
from ...shared.utr_utils import  get_repeated_utr_calls


def _cmd_base(project, platform, utr_calls, editor):
    base = [
        f'curl -s {UTR_INSTALL_URL}.bat --output {TEST_PROJECTS_DIR}/URP-Update-testing/{project["folder"]}/utr.bat',
        f'choco install unity-downloader-cli -y -s https://artifactory.prd.it.unity3d.com/artifactory/api/nuget/unity-choco-local'
    ]

    if str(editor['track']).lower()=='custom-revision':
        base.append(f'copy /Y \"{PATH_UNITY_REVISION}" \"{TEST_PROJECTS_DIR}/URP-Update-testing/{project["folder"]}/{PATH_UNITY_REVISION}"')
    base.append(f'cd {TEST_PROJECTS_DIR}/URP-Update-testing/{project["folder"]} && unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"]) } {"".join([f"-c {c} " for c in platform["components"]])} --wait --published-only')

    for utr_args in utr_calls:
        base.append(f'cd {TEST_PROJECTS_DIR}/URP-Update-testing/{project["folder"]} && utr {" ".join(utr_args)}')
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
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project["folder"])
    base = [f'curl -s {UTR_INSTALL_URL}.bat --output {TEST_PROJECTS_DIR}/{project["folder"]}/utr.bat']
    for utr_args in utr_calls:
        base.append(f'cd {TEST_PROJECTS_DIR}/{project["folder"]} && utr {" ".join(utr_args)}')

    base = add_project_commands(project) + base
    return base


def cmd_standalone_build(project, platform, api, test_platform, editor, build_config, color_space):
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project["folder"], utr_flags_key="utr_flags_build")
    return _cmd_base(project, platform, utr_calls, editor)

def add_project_commands(project):
    cmds = []
    if project.get("url"):
        cmds.extend([
            f'git clone {project["url"]} -b {project["branch"]} {TEST_PROJECTS_DIR}/{project["repo"]}',
            f'cd {TEST_PROJECTS_DIR}/{project["repo"]} && git checkout {project["revision"]}',
            f'NetSh Advfirewall set allprofiles state off'
        ])
    if project.get("unity_config_commands"):
        cmds.extend([
            f'choco source add -n Unity -s https://artifactory.prd.it.unity3d.com/artifactory/api/nuget/unity-choco-local',
            f'choco install unity-config'
        ])
        for unity_config in project["unity_config_commands"]:
            cmds.append(f'cd {TEST_PROJECTS_DIR}/{project["repo"]} && {unity_config}')
    return cmds