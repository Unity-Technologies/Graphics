from ruamel.yaml.scalarstring import PreservedScalarString as pss
from ...shared.constants import TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS, PATH_PLAYERS, UNITY_DOWNLOADER_CLI_URL, UTR_INSTALL_URL,get_unity_downloader_cli_cmd, get_timeout
from ...shared.utr_utils import utr_editmode_flags, utr_playmode_flags, utr_standalone_split_flags, utr_standalone_build_flags

def _cmd_base(project_folder, platform, utr_flags, editor):
    return [
        f'curl -s {UTR_INSTALL_URL}.bat --output {TEST_PROJECTS_DIR}/{project_folder}/utr.bat',
        f'pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"], cd=True) } {"".join([f"-c {c} " for c in platform["components"]])} --wait --published-only',
        pss(f'''
         git rev-parse HEAD | git show -s --format=%%cI > revdate.tmp
         set /p GIT_REVISIONDATE=<revdate.tmp
         echo %GIT_REVISIONDATE%
         del revdate.tmp
         cd {TEST_PROJECTS_DIR}/{project_folder} && utr {" ".join(utr_flags)}''')
    ]


def cmd_editmode(project_folder, platform, api, test_platform, editor, build_config, color_space):
    scripting_backend = build_config["scripting_backend"]
    api_level = build_config["api_level"]

    utr_args = utr_editmode_flags(scripting_backend=f'{scripting_backend}', api_level=f'{api_level}', color_space=f'{color_space}')

    utr_args.extend(test_platform["extra_utr_flags"])
    utr_args.extend(platform["extra_utr_flags"])
    if api["name"] != "":
        utr_args.append(f'--extra-editor-arg="{api["cmd"]}"')

    base = _cmd_base(project_folder, platform, utr_args, editor)

    extra_cmds = extra_perf_cmd(project_folder)
    unity_config = install_unity_config(project_folder)
    extra_cmds = extra_cmds + unity_config
    if project_folder.lower() == "BoatAttack".lower():
        x=0
        for y in extra_cmds:
            base.insert(x, y)
            x += 1
        #base.extend(unity_config)

    return  base


def cmd_playmode(project_folder, platform, api, test_platform, editor, build_config, color_space):
    scripting_backend = build_config["scripting_backend"]
    api_level = build_config["api_level"]
    utr_args = utr_playmode_flags(scripting_backend=f'{scripting_backend}', api_level=f'{api_level}', color_space=f'{color_space}')
    utr_args.extend(test_platform["extra_utr_flags"])
    utr_args.extend(platform["extra_utr_flags"])
    if api["name"] != "":
        utr_args.append(f'--extra-editor-arg="{api["cmd"]}"')

    base = _cmd_base(project_folder, platform, utr_args, editor)

    extra_cmds = extra_perf_cmd(project_folder)
    unity_config = install_unity_config(project_folder)
    extra_cmds = extra_cmds + unity_config
    if project_folder.lower() == "BoatAttack".lower():
        x=0
        for y in extra_cmds:
            base.insert(x, y)
            x += 1
        #base.extend(unity_config)

    return  base

def cmd_standalone(project_folder, platform, api, test_platform, editor, build_config, color_space):
    scripting_backend = build_config["scripting_backend"]
    api_level = build_config["api_level"]
    utr_args = utr_standalone_split_flags("Windows64", scripting_backend=f'{scripting_backend}', api_level=f'{api_level}', color_space=f'{color_space}')
    utr_args.extend(test_platform["extra_utr_flags"])
    utr_args.extend(platform["extra_utr_flags"])
    utr_args.append(f'--timeout={get_timeout(test_platform, "Win")}')

    base = [f'curl -s {UTR_INSTALL_URL}.bat --output {TEST_PROJECTS_DIR}/{project_folder}/utr.bat']
    if project_folder.lower() == 'UniversalGraphicsTest'.lower():
        base.append('cd Tools && powershell -command ". .\\Unity.ps1; Set-ScreenResolution -width 1920 -Height 1080"')
    
    base.append(f'cd {TEST_PROJECTS_DIR}/{project_folder} && utr {" ".join(utr_args)}')
    
    # if not test_platform['is_performance']:
    #     base.append(f'cd {TEST_PROJECTS_DIR}/{project_folder} && utr {" ".join(utr_args)}')
    # else:
    #     base.append(f'{TEST_PROJECTS_DIR}/{project_folder}/utr {" ".join(utr_args)}')

    
    return base


def cmd_standalone_build(project_folder, platform, api, test_platform, editor, build_config, color_space):
    scripting_backend = build_config["scripting_backend"]
    api_level = build_config["api_level"]
    utr_args = utr_standalone_build_flags("Windows64", graphics_api=api["name"], scripting_backend=f'{scripting_backend}', api_level=f'{api_level}', color_space=f'{color_space}')
    utr_args.extend(test_platform["extra_utr_flags_build"])
    utr_args.extend(platform["extra_utr_flags_build"])
    utr_args.append(f'--timeout={get_timeout(test_platform, "Win", build=True)}')

    if not test_platform['is_performance']:
        utr_args.extend(['--extra-editor-arg="-executemethod"'])
        utr_args.extend([f'--extra-editor-arg="CustomBuild.BuildWindows{api["name"]}Linear"'])
    
    base = _cmd_base(project_folder, platform, utr_args, editor)
    
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
        f'git clone https://github.com/Unity-Technologies/BoatAttack.git -b feature/benchmark TestProjects/{project_folder}',
        f'NetSh Advfirewall set allprofiles state off'
        ]
    return perf_list

def install_unity_config(project_folder):
    cmds = [
        f'choco source add -n Unity -s https://artifactory.prd.it.unity3d.com/artifactory/api/nuget/unity-choco-local',
        f'choco install unity-config',


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