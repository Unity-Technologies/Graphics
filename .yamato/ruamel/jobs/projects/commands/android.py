from ...shared.constants import TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS, PATH_PLAYERS, UTR_INSTALL_URL, UNITY_DOWNLOADER_CLI_URL, get_unity_downloader_cli_cmd, get_timeout
from ruamel.yaml.scalarstring import PreservedScalarString as pss
from ...shared.utr_utils import extract_flags, get_repeated_utr_calls


def _cmd_base(project, components):
    return [    ]


def cmd_editmode(project, platform, api, test_platform, editor, build_config, color_space):    
    base = [ 
        f'curl -s {UTR_INSTALL_URL}.bat --output utr.bat',
        f'pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
        f'unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"]) } -p WindowsEditor {"".join([f"-c {c} " for c in platform["components"]])} --wait --published-only',
        f'NetSh Advfirewall set allprofiles state off']

    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project["folder"])
    for utr_args in utr_calls:
        base.append(
        pss(f'''
         git rev-parse HEAD | git show -s --format=%%cI > revdate.tmp
         set /p GIT_REVISIONDATE=<revdate.tmp
         echo %GIT_REVISIONDATE%
         del revdate.tmp
         utr {" ".join(utr_args)}'''))
    
    base = add_project_commands(project) + base
    return base


def cmd_playmode(project, platform, api, test_platform, editor, build_config, color_space):

    base = [ 
        f'curl -s {UTR_INSTALL_URL}.bat --output utr.bat',
        f'choco install unity-downloader-cli -y -s https://artifactory.prd.it.unity3d.com/artifactory/api/nuget/unity-choco-local',
        f'unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"]) } -p WindowsEditor {"".join([f"-c {c} " for c in platform["components"]])} --wait --published-only',
        f'%ANDROID_SDK_ROOT%\platform-tools\\adb.exe connect %BOKKEN_DEVICE_IP%',
        f'powershell %ANDROID_SDK_ROOT%\platform-tools\\adb.exe devices',
        f'NetSh Advfirewall set allprofiles state off']
    
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project["folder"])
    for utr_args in utr_calls:
        base.append(
        pss(f'''
         git rev-parse HEAD | git show -s --format=%%cI > revdate.tmp
         set /p GIT_REVISIONDATE=<revdate.tmp
         echo %GIT_REVISIONDATE%
         del revdate.tmp
         utr {" ".join(utr_args)}'''))
    base.append(f'start %ANDROID_SDK_ROOT%\platform-tools\\adb.exe kill-server')
    base = add_project_commands(project) + base
    return base

def cmd_standalone(project, platform, api, test_platform, editor, build_config, color_space):   
    base = [ 
        f'curl -s {UTR_INSTALL_URL}.bat --output utr.bat',
        f'%ANDROID_SDK_ROOT%\platform-tools\\adb.exe connect %BOKKEN_DEVICE_IP%',
        f'powershell %ANDROID_SDK_ROOT%\platform-tools\\adb.exe devices',
        f'NetSh Advfirewall set allprofiles state off']
    
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project["folder"])
    for utr_args in utr_calls:
        base.append(
        pss(f'''
        set ANDROID_DEVICE_CONNECTION=%BOKKEN_DEVICE_IP%
         git rev-parse HEAD | git show -s --format=%%cI > revdate.tmp
         set /p GIT_REVISIONDATE=<revdate.tmp
         echo %GIT_REVISIONDATE%
         del revdate.tmp
        utr {" ".join(utr_args)}'''))

    base.append(f'start %ANDROID_SDK_ROOT%\platform-tools\\adb.exe kill-server')
    return base

        
def cmd_standalone_build(project, platform, api, test_platform, editor, build_config, color_space):

    base = [ 
        f'curl -s {UTR_INSTALL_URL}.bat --output utr.bat',
        f'pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
        f'unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"]) } -p WindowsEditor {"".join([f"-c {c} " for c in platform["components"]])} --wait --published-only',
        f'NetSh Advfirewall set allprofiles state off' ]

    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project["folder"], utr_flags_key="utr_flags_build")
    for utr_args in utr_calls:
        base.append(
        pss(f'''
         git rev-parse HEAD | git show -s --format=%%cI > revdate.tmp
         set /p GIT_REVISIONDATE=<revdate.tmp
         echo %GIT_REVISIONDATE%
         del revdate.tmp
         utr {" ".join(utr_args)}'''))

    base = add_project_commands(project) + base

    return base
    

def add_project_commands(project):
    cmds = []
    if project.get("url"):
        cmds.extend([
            f'git clone {project["url"]} -b {project["branch"]} {TEST_PROJECTS_DIR}/{project["folder"]}',
            f'cd {TEST_PROJECTS_DIR}/{project["folder"]} && git checkout {project["revision"]}',
        ])
    if project.get("unity_config_commands"):
        cmds.extend([
            f'choco source add -n Unity -s https://artifactory.prd.it.unity3d.com/artifactory/api/nuget/unity-choco-local',
            f'choco install unity-config',
        ])
        for unity_config in project["unity_config_commands"]:
            cmds.append(f'cd {TEST_PROJECTS_DIR}/{project["folder"]} && {unity_config}')
    return cmds