from ...shared.constants import TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS, PATH_PLAYERS, UTR_INSTALL_URL, UNITY_DOWNLOADER_CLI_URL
from ruamel.yaml.scalarstring import PreservedScalarString as pss

def _cmd_base(project_folder, components, editor_revision):
    return [
        f'curl -s {UTR_INSTALL_URL}.bat --output utr.bat',
        f'pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
        f'unity-downloader-cli -u { editor_revision } -p WindowsEditor {"".join([f"-c {c} " for c in components])} --wait --published-only'
    ]


def cmd_not_standalone(project_folder, platform, api, test_platform_args, editor_revision):
    raise Exception('android: only standalone available')

def cmd_standalone(project_folder, platform, api, test_platform_args, editor_revision):
    base = [
        f'curl -s {UTR_INSTALL_URL}.bat --output utr.bat'
    ]
    base.extend([ 
        f'%ANDROID_SDK_ROOT%\platform-tools\\adb.exe connect %BOKKEN_DEVICE_IP%',
        f'powershell %ANDROID_SDK_ROOT%\platform-tools\\adb.exe devices',
        f'NetSh Advfirewall set allprofiles state off',
        pss(f'''
        set ANDROID_DEVICE_CONNECTION=%BOKKEN_DEVICE_IP%
        utr --suite=playmode --platform=Android --editor-location=WindowsEditor --artifacts_path={PATH_TEST_RESULTS} --player-load-path={PATH_PLAYERS} --scripting-backend=il2cpp --timeout=1200{_get_extra_utr_arg(project_folder)}'''),
        f'start %ANDROID_SDK_ROOT%\platform-tools\\adb.exe kill-server'
        ])
    return base

        
def cmd_standalone_build(project_folder, platform, api, test_platform_args, editor_revision):
    base = _cmd_base(project_folder, platform["components"], editor_revision)
    base.extend([  
        f'mklink /d WindowsEditor\Data\PlaybackEngines\AndroidPlayer\OpenJDK %JAVA_HOME% || exit 0',
        f'mklink /d WindowsEditor\Data\PlaybackEngines\AndroidPlayer\SDK %ANDROID_SDK_ROOT% || exit 0',
        f'mklink /d WindowsEditor\Data\PlaybackEngines\AndroidPlayer\\NDK %ANDROID_NDK_ROOT% || exit 0'
        ])
    
    if api["name"].lower() =='vulkan':
        base.append(f'utr --suite=playmode --platform=Android --testproject={TEST_PROJECTS_DIR}\{project_folder} --extra-editor-arg="-executemethod" --extra-editor-arg="SetupProject.ApplySettings" --extra-editor-arg="vulkan" --editor-location=WindowsEditor --artifacts_path={PATH_TEST_RESULTS} --player-save-path={PATH_PLAYERS} --scripting-backend=il2cpp --timeout=1800 --build-only{_get_extra_utr_arg(project_folder)}')
    else:
        base.append(f'utr --suite=playmode --platform=Android --testproject={TEST_PROJECTS_DIR}\{project_folder} --editor-location=WindowsEditor --artifacts_path={PATH_TEST_RESULTS} --player-save-path={PATH_PLAYERS} --scripting-backend=il2cpp --timeout=1800 --build-only{_get_extra_utr_arg(project_folder)}')
    return base



def _get_extra_utr_arg(project_folder):
    return ' --compilation-errors-as-warnings' if project_folder.lower() in ['universalhybridtest', 'hdrp_hybridtests'] else ''
