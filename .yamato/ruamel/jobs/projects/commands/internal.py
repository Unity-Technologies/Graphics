from ...shared.constants import TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS, PATH_PLAYERS, GITHUB_CDS_URL, UNITY_DOWNLOADER_CLI_URL, UTR_INSTALL_URL,get_unity_downloader_cli_cmd
from ...shared.utr_utils import  get_repeated_utr_calls


def _cmd_base(project, platform, utr_calls, editor):
    base = [
        f'git clone {GITHUB_CDS_URL}/sophia/URP-Update-testing.git TestProjects/URP-Update-testing',
        f'curl -s {UTR_INSTALL_URL}.bat --output {TEST_PROJECTS_DIR}/URP-Update-testing/{project["folder"]}/utr.bat',
        f'choco install unity-downloader-cli -y -s https://artifactory.prd.it.unity3d.com/artifactory/api/nuget/unity-choco-local',
        f'Xcopy /E /I \"com.unity.render-pipelines.core\" \"{TEST_PROJECTS_DIR}/URP-Update-testing/{project["folder"]}/Packages/com.unity.render-pipelines.core\" /Y',
        f'Xcopy /E /I \"com.unity.render-pipelines.universal\" \"{TEST_PROJECTS_DIR}/URP-Update-testing/{project["folder"]}/Packages/com.unity.render-pipelines.universal\" /Y',
        f'Xcopy /E /I \"com.unity.shadergraph\" \"{TEST_PROJECTS_DIR}/URP-Update-testing/{project["folder"]}/Packages/com.unity.shadergraph\" /Y',
    ]

    if str(editor['track']).lower()=='custom-revision':
        base.append(f'copy /Y \"{PATH_UNITY_REVISION}" \"{TEST_PROJECTS_DIR}/URP-Update-testing/{project["folder"]}/{PATH_UNITY_REVISION}"')
    base.append(f'cd {TEST_PROJECTS_DIR}/URP-Update-testing/{project["folder"]} && unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"]) } {"".join([f"-c {c} " for c in platform["components"]])} --wait --published-only')

    for utr_args in utr_calls:
        base.append(f'cd {TEST_PROJECTS_DIR}/URP-Update-testing/{project["folder"]} && utr {" ".join(utr_args)}')
    return base

def cmd_editmode(project, platform, api, test_platform, editor, build_config, color_space):
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project["folder"])
    return _cmd_base(project, platform, utr_calls, editor)


def cmd_playmode(project, platform, api, test_platform, editor, build_config, color_space):
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project["folder"])
    return _cmd_base(project, platform, utr_calls, editor)

def cmd_standalone(project, platform, api, test_platform, editor, build_config, color_space):
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project["folder"])
    base = [f'curl -s {UTR_INSTALL_URL}.bat --output {TEST_PROJECTS_DIR}/{project["folder"]}/utr.bat']
    for utr_args in utr_calls:
        base.append(f'cd {TEST_PROJECTS_DIR}/{project["folder"]} && utr {" ".join(utr_args)}')

    return base


def cmd_standalone_build(project, platform, api, test_platform, editor, build_config, color_space):
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project["folder"], utr_flags_key="utr_flags_build")
    return _cmd_base(project, platform, utr_calls, editor)
