from ...shared.constants import TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS, PATH_PLAYERS, UTR_INSTALL_URL, UNITY_DOWNLOADER_CLI_URL
from ruamel.yaml.scalarstring import PreservedScalarString as pss

def _cmd_base(project_folder, components):
    return [
        f'curl -s {UTR_INSTALL_URL}.bat --output utr.bat',
        f'pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
        f'unity-downloader-cli --source-file %YAMATO_SOURCE_DIR%/{PATH_UNITY_REVISION} -p WindowsEditor {"".join([f"-c {c} " for c in components])} --wait --published-only'
    ]


def cmd_not_standalone(project_folder, platform, api, test_platform_args):
    raise Exception('android: only standalone available')

def cmd_standalone(project_folder, platform, api, test_platform_args):
    base = [
        f'curl -s {UTR_INSTALL_URL}.bat --output utr.bat'
    ]
    base.extend([ 
        f'%ANDROID_SDK_ROOT%\platform-tools\\adb.exe connect %BOKKEN_DEVICE_IP%',
        f'powershell %ANDROID_SDK_ROOT%\platform-tools\\adb.exe devices',
        f'NetSh Advfirewall set allprofiles state off',
        pss(f'''
        set ANDROID_DEVICE_CONNECTION=%BOKKEN_DEVICE_IP%
        utr --suite=playmode --platform=Android --editor-location=WindowsEditor --artifacts_path={PATH_TEST_RESULTS} --player-load-path={PATH_PLAYERS} --scripting-backend=il2cpp --timeout=1200'''),
        f'start %ANDROID_SDK_ROOT%\platform-tools\\adb.exe kill-server'
        ])
    return base

        
def cmd_standalone_build(project_folder, platform, api, test_platform_args):
    base = _cmd_base(project_folder, platform["components"])
    base.extend([  
        f'mklink /d WindowsEditor\Data\PlaybackEngines\AndroidPlayer\OpenJDK %JAVA_HOME% || exit 0',
        f'mklink /d WindowsEditor\Data\PlaybackEngines\AndroidPlayer\SDK %ANDROID_SDK_ROOT% || exit 0',
        f'mklink /d WindowsEditor\Data\PlaybackEngines\AndroidPlayer\\NDK %ANDROID_NDK_ROOT% || exit 0'
        ])
    
    if api["name"].lower() =='vulkan':
        base.append(f'utr --suite=playmode --platform=Android --testproject={TEST_PROJECTS_DIR}\{project_folder} --extra-editor-arg="-executemethod" --extra-editor-arg="SetupProject.ApplySettings" --extra-editor-arg="vulkan" --editor-location=WindowsEditor --artifacts_path={PATH_TEST_RESULTS} --player-save-path={PATH_PLAYERS} --scripting-backend=il2cpp --timeout=1200 --build-only')
    else:
        base.append(f'utr --suite=playmode --platform=Android --testproject={TEST_PROJECTS_DIR}\{project_folder} --editor-location=WindowsEditor --artifacts_path={PATH_TEST_RESULTS} --player-save-path={PATH_PLAYERS} --scripting-backend=il2cpp --timeout=1200 --build-only')
    return base