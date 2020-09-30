from ...shared.constants import TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS, PATH_PLAYERS, UTR_INSTALL_URL, UNITY_DOWNLOADER_CLI_URL, get_unity_downloader_cli_cmd, get_timeout
from ruamel.yaml.scalarstring import PreservedScalarString as pss
from ...shared.utr_utils import utr_editmode_flags, utr_playmode_flags, utr_standalone_split_flags,utr_standalone_not_split_flags, utr_standalone_build_flags


def _cmd_base(project_folder, components):
    return [    ]


def cmd_editmode(project_folder, platform, api, test_platform, editor):    
    utr_args = utr_editmode_flags( testproject=f'{TEST_PROJECTS_DIR}\{project_folder}',editor_location='WindowsEditor')
    utr_args.extend(test_platform["extra_utr_flags"])
    if test_platform["name"].lower()=='playmode_perf_build':
        utr_args.append('--platform=Android')

    base = [ 
        f'curl -s {UTR_INSTALL_URL}.bat --output utr.bat',
        f'pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
        f'unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"]) } -p WindowsEditor {"".join([f"-c {c} " for c in platform["components"]])} --wait --published-only',
        f'%ANDROID_SDK_ROOT%\platform-tools\\adb.exe connect %BOKKEN_DEVICE_IP%',
        f'powershell %ANDROID_SDK_ROOT%\platform-tools\\adb.exe devices',
        f'NetSh Advfirewall set allprofiles state off',
        pss(f'''
        set ANDROID_DEVICE_CONNECTION=%BOKKEN_DEVICE_IP%
        utr {" ".join(utr_args)}'''),
        f'start %ANDROID_SDK_ROOT%\platform-tools\\adb.exe kill-server'
        ]
    return base


def cmd_playmode(project_folder, platform, api, test_platform, editor):
    utr_args = utr_playmode_flags(testproject=f'{TEST_PROJECTS_DIR}\{project_folder}',editor_location='WindowsEditor')
    utr_args.extend(test_platform["extra_utr_flags"])
    if test_platform["name"].lower()=='playmode_perf_build':
        utr_args.append('--platform=Android')

    base = [ 
        f'curl -s {UTR_INSTALL_URL}.bat --output utr.bat',
        f'pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
        f'unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"]) } -p WindowsEditor {"".join([f"-c {c} " for c in platform["components"]])} --wait --published-only',
        f'%ANDROID_SDK_ROOT%\platform-tools\\adb.exe connect %BOKKEN_DEVICE_IP%',
        f'powershell %ANDROID_SDK_ROOT%\platform-tools\\adb.exe devices',
        f'NetSh Advfirewall set allprofiles state off',
        pss(f'''
        set ANDROID_DEVICE_CONNECTION=%BOKKEN_DEVICE_IP%
        utr {" ".join(utr_args)}'''),
        f'start %ANDROID_SDK_ROOT%\platform-tools\\adb.exe kill-server'
        ]
    return base

def cmd_standalone(project_folder, platform, api, test_platform, editor):
    
    utr_args = utr_standalone_split_flags(platform_spec='', platform='Android', testproject=f'{TEST_PROJECTS_DIR}\{project_folder}', player_load_path=PATH_PLAYERS, player_conn_ip=None)
    utr_args.extend(test_platform["extra_utr_flags"])
    utr_args.extend(['--scripting-backend=il2cpp', f'--editor-location=WindowsEditor'])
    utr_args.append(f'--timeout={get_timeout(test_platform, "Android")}')


    return [ 
        f'curl -s {UTR_INSTALL_URL}.bat --output utr.bat',
        f'%ANDROID_SDK_ROOT%\platform-tools\\adb.exe connect %BOKKEN_DEVICE_IP%',
        f'powershell %ANDROID_SDK_ROOT%\platform-tools\\adb.exe devices',
        f'NetSh Advfirewall set allprofiles state off',
        pss(f'''
        set ANDROID_DEVICE_CONNECTION=%BOKKEN_DEVICE_IP%
        utr {" ".join(utr_args)}'''),
        f'start %ANDROID_SDK_ROOT%\platform-tools\\adb.exe kill-server'
        ]

        
def cmd_standalone_build(project_folder, platform, api, test_platform, editor):

    utr_args = utr_standalone_build_flags(platform_spec='', platform='Android', testproject=f'{TEST_PROJECTS_DIR}\\{project_folder}', player_save_path=PATH_PLAYERS, editor_location='WindowsEditor')
    utr_args.extend(test_platform["extra_utr_flags_build"])
    utr_args.extend(['--scripting-backend=il2cpp'])
    utr_args.append(f'--timeout={get_timeout(test_platform, "Android", build=True)}')


    if api["name"].lower() =='vulkan':
        utr_args.extend(['--extra-editor-arg="-executemethod"', f'--extra-editor-arg="SetupProject.ApplySettings"','--extra-editor-arg="vulkan"'])

    return [  
        f'curl -s {UTR_INSTALL_URL}.bat --output utr.bat',
        f'pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
        f'unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"]) } -p WindowsEditor {"".join([f"-c {c} " for c in platform["components"]])} --wait --published-only',
        f'mklink /d WindowsEditor\Data\PlaybackEngines\AndroidPlayer\OpenJDK %JAVA_HOME% || exit 0',
        f'mklink /d WindowsEditor\Data\PlaybackEngines\AndroidPlayer\SDK %ANDROID_SDK_ROOT% || exit 0',
        f'mklink /d WindowsEditor\Data\PlaybackEngines\AndroidPlayer\\NDK %ANDROID_NDK_ROOT% || exit 0',
        f'utr {" ".join(utr_args)}'
        ]

