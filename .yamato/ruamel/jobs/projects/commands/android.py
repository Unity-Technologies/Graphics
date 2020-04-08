from ...utils.constants import TEST_PROJECTS_DIR
from ruamel.yaml.scalarstring import PreservedScalarString as pss

def _cmd_base(project, components):
    return [
        f'git clone git@github.cds.internal.unity3d.com:unity/utr.git utr',
        f'pip install unity-downloader-cli --extra-index-url https://artifactory.eu-cph-1.unityops.net/api/pypi/common-python/simple --upgrade',
        f'unity-downloader-cli --source-file %YAMATO_SOURCE_DIR%/unity_revision.txt -p WindowsEditor {"".join([f"-c {c} " for c in components])} --wait --published-only'
    ]


def cmd_not_standalone(project, platform, api, test_platform_args):
    raise Exception('android: only standalone available')

def cmd_standalone(project, platform, api, test_platform_args):
    base = _cmd_base(project, platform["components"])
    base.extend([ 
        f'%ANDROID_SDK_ROOT%\platform-tools\\adb.exe connect %BOKKEN_DEVICE_IP%',
        f'powershell %ANDROID_SDK_ROOT%\platform-tools\\adb.exe devices',
        f'NetSh Advfirewall set allprofiles state off',
        pss(f'''
        set ANDROID_DEVICE_CONNECTION=%BOKKEN_DEVICE_IP%
        utr\\utr --suite=playmode --platform=Android --editor-location=WindowsEditor --artifacts_path=test-results --player-load-path=players --scripting-backend=il2cpp --timeout=1200 --player-connection-ip=%BOKKEN_DEVICE_IP%'''),
        f'start %ANDROID_SDK_ROOT%\platform-tools\\adb.exe kill-server'
        ])
    return base

        
def cmd_standalone_build(project, platform, api):
    base = _cmd_base(project, platform["components"])
    base.extend([  
        f'mklink /d WindowsEditor\Data\PlaybackEngines\AndroidPlayer\OpenJDK %JAVA_HOME%',
        f'mklink /d WindowsEditor\Data\PlaybackEngines\AndroidPlayer\SDK %ANDROID_SDK_ROOT%',
        f'mklink /d WindowsEditor\Data\PlaybackEngines\AndroidPlayer\\NDK %ANDROID_NDK_ROOT%'
        ])
    if api["name"].lower() =='Vulkan':
        base.append(f'utr\\utr --suite=playmode --platform=Android --testproject={TEST_PROJECTS_DIR}\{project["folder"]} --extra-editor-arg="-executemethod" --extra-editor-arg="SetupProject.ApplySettings" --extra-editor-arg="vulkan" --editor-location=WindowsEditor --artifacts_path=test-results --player-save-path=players --scripting-backend=il2cpp --timeout=1200 --build-only')
    else:
        base.append(f'utr\\utr --suite=playmode --platform=Android --testproject={TEST_PROJECTS_DIR}\{project["folder"]} --editor-location=WindowsEditor --artifacts_path=test-results --player-save-path=players --scripting-backend=il2cpp --timeout=1200 --build-only')
    return base