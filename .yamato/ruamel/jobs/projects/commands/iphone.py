from ...shared.constants import TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS, PATH_PLAYERS, UTR_INSTALL_URL, UNITY_DOWNLOADER_CLI_URL, get_unity_downloader_cli_cmd, get_timeout
from ruamel.yaml.scalarstring import PreservedScalarString as pss
from ...shared.utr_utils import utr_editmode_flags, utr_playmode_flags, utr_standalone_split_flags,utr_standalone_not_split_flags, utr_standalone_build_flags


def _cmd_base(project_folder, platform, editor):
    return []

def cmd_editmode(project_folder, platform, api, test_platform, editor, build_config, color_space):
    scripting_backend = build_config["scripting_backend"]
    api_level = build_config["api_level"]
    utr_args = utr_standalone_build_flags(platform_spec='', platform='iOS', testproject=f'{TEST_PROJECTS_DIR}/{project_folder}', graphics_api=api["name"], player_save_path=PATH_PLAYERS, scripting_backend=f'{scripting_backend}', api_level=f'{api_level}', color_space=f'{color_space}')
    utr_args.extend(test_platform["extra_utr_flags"])

    base = [
        f'pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
        f'unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"]) } {"".join([f"-c {c} " for c in platform["components"]])}  --wait --published-only',
        f'curl -s {UTR_INSTALL_URL} --output utr',
        f'chmod +x ./utr',
        f'./utr {" ".join(utr_args)}'
     ]

    extra_cmds = extra_perf_cmd(project_folder)
    unity_config = install_unity_config(project_folder)
    extra_cmds = extra_cmds + unity_config
    if project_folder.lower() == "BoatAttack".lower():
        x=0
        for y in extra_cmds:
            base.insert(x, y)
            x += 1
    return base

def cmd_playmode(project_folder, platform, api, test_platform, editor, build_config, color_space):
    scripting_backend = build_config["scripting_backend"]
    api_level = build_config["api_level"]
    utr_args = utr_playmode_flags(testproject=f'{TEST_PROJECTS_DIR}/{project_folder}', scripting_backend=f'{scripting_backend}', api_level=f'{api_level}', color_space=f'{color_space}')
    utr_args.extend(test_platform["extra_utr_flags"])
    utr_args.append(f'--timeout={get_timeout(test_platform, "iOS")}')

    base = [
        f'pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
        f'unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"]) } {"".join([f"-c {c} " for c in platform["components"]])}  --wait --published-only',
        f'curl -s {UTR_INSTALL_URL} --output utr',
        f'chmod +x ./utr',
        f'./utr {" ".join(utr_args)}'
     ]
    extra_cmds = extra_perf_cmd(project_folder)
    unity_config = install_unity_config(project_folder)
    extra_cmds = extra_cmds + unity_config
    if project_folder.lower() == "BoatAttack".lower():
        x=0
        for y in extra_cmds:
            base.insert(x, y)
            x += 1
    return base

def cmd_standalone(project_folder, platform, api, test_platform, editor, build_config, color_space):
    scripting_backend = build_config["scripting_backend"]
    api_level = build_config["api_level"]
    utr_args = utr_standalone_split_flags(platform_spec='', platform='iOS', player_load_path='players',player_conn_ip=None, scripting_backend=f'{scripting_backend}', api_level=f'{api_level}', color_space=f'{color_space}')
    utr_args.extend(test_platform["extra_utr_flags"])
    utr_args.extend(platform["extra_utr_flags"])

    return [
        f'curl -s {UTR_INSTALL_URL} --output utr',        
        f'chmod +x ./utr',
        f'./utr {" ".join(utr_args)}'
    ]

        
def cmd_standalone_build(project_folder, platform, api, test_platform, editor, build_config, color_space):
    scripting_backend = build_config["scripting_backend"]
    api_level = build_config["api_level"]
    utr_args = utr_standalone_build_flags(platform_spec='', platform='iOS', testproject=f'{TEST_PROJECTS_DIR}/{project_folder}', graphics_api=api["name"], player_save_path=PATH_PLAYERS, scripting_backend=f'{scripting_backend}', api_level=f'{api_level}', color_space=f'{color_space}')
    utr_args.extend(test_platform["extra_utr_flags_build"])
    utr_args.extend(platform["extra_utr_flags_build"])
    utr_args.append(f'--timeout={get_timeout(test_platform, "iOS", build=True)}')

    base = [
        f'pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
        f'unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"]) } {"".join([f"-c {c} " for c in platform["components"]])}  --wait --published-only',
        f'curl -s {UTR_INSTALL_URL} --output utr',
        f'chmod +x ./utr',
        f'./utr {" ".join(utr_args)}'
     ]
    extra_cmds = extra_perf_cmd(project_folder)
    unity_config = install_unity_config(project_folder)
    extra_cmds = extra_cmds + unity_config
    if project_folder.lower() == "BoatAttack".lower():
        x=0
        for y in extra_cmds:
            base.insert(x, y)
            x += 1
    return base

def extra_perf_cmd(project_folder):   
    perf_list = [
        f'git clone https://github.com/Unity-Technologies/BoatAttack.git -b feature/benchmark TestProjects/{project_folder}'
        ]
    return perf_list

def install_unity_config(project_folder):
    cmds = [
        f'brew tap --force-auto-update unity/unity git@github.cds.internal.unity3d.com:unity/homebrew-unity.git',
        f'brew install unity-config',

        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency "com.unity.render-pipelines.core@file:../../../com.unity.render-pipelines.core" --project-path .',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency "com.unity.render-pipelines.universal@file:../../../com.unity.render-pipelines.universal" --project-path .',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency "com.unity.shadergraph@file:../../../com.unity.shadergraph" --project-path .',


		#f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project remove dependency com.unity.render-pipelines.universal',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency com.unity.addressables@1.14.2 --project-path .',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency com.unity.scriptablebuildpipeline@1.11.2 --project-path .',
		f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency com.unity.test-framework@1.1.18 --project-path .',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency com.unity.test-framework.performance@2.3.1-preview --project-path .',
		f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency com.unity.test-framework.utp-reporter@1.0.2-preview --project-path .',
		f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency com.unity.test-framework.build@0.0.1-preview.12 --project-path .',
        
		f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency \"com.unity.test.metadata-manager@ssh://git@github.cds.internal.unity3d.com/unity/com.unity.test.metadata-manager.git\" --project-path .',        
		f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency \"com.unity.testing.graphics-performance@ssh://git@github.cds.internal.unity3d.com/unity/com.unity.testing.graphics-performance.git\"  --project-path .',        
		f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency \"unity.graphictests.performance.universal@ssh://git@github.cds.internal.unity3d.com/unity/unity.graphictests.performance.universal.git\" --project-path .',	
		
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add testable com.unity.cli-project-setup  --project-path .',		
		f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add testable com.unity.test.performance.runtimesettings  --project-path .',
		f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add testable test.metadata-manager  --project-path .',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add testable com.unity.testing.graphics-performance --project-path .',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add testable com.unity.render-pipelines.core  --project-path .',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add testable unity.graphictests.performance.universal  --project-path .',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project set project-update false --project-path .'
    ]
    return cmds