from ...shared.constants import TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS, PATH_PLAYERS, UNITY_DOWNLOADER_CLI_URL, UTR_INSTALL_URL

def _cmd_base(project_folder, components):
    return [
        f'curl -s {UTR_INSTALL_URL}.bat --output {TEST_PROJECTS_DIR}/{project_folder}/utr.bat',
        f'pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-downloader-cli --source-file ../../{PATH_UNITY_REVISION} {"".join([f"-c {c} " for c in components])} --wait --published-only'
    ]


def cmd_not_standalone(project_folder, platform, api, test_platform_args):
    base = _cmd_base(project_folder, platform["components"])
    base.extend([
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && utr {test_platform_args} --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS}{_get_extra_utr_arg(project_folder)}'
    ])
    base[-1] += f' --extra-editor-arg="{api["cmd"]}"' if api["name"] != ""  else ''
    return base

def cmd_standalone(project_folder, platform, api, test_platform_args):
    base = [
        f'curl -s {UTR_INSTALL_URL}.bat --output {TEST_PROJECTS_DIR}/{project_folder}/utr.bat'
    ]

    if project_folder.lower() == 'UniversalGraphicsTest'.lower():
        base.append('cd Tools && powershell -command ". .\\Unity.ps1; Set-ScreenResolution -width 1920 -Height 1080"')

    base.extend([
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && utr {test_platform_args}Windows64 --artifacts_path={PATH_TEST_RESULTS} --timeout=1200 --player-load-path=../../{PATH_PLAYERS} --player-connection-ip=auto{_get_extra_utr_arg(project_folder)}'
    ])
    return base


def cmd_standalone_build(project_folder, platform, api, test_platform_args):
    base = _cmd_base(project_folder, platform["components"])
    base.extend([
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && utr {test_platform_args}Windows64 --extra-editor-arg="-executemethod" --extra-editor-arg="CustomBuild.BuildWindows{api["name"]}Linear" --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS} --timeout=1200 --player-save-path=../../{PATH_PLAYERS} --build-only{_get_extra_utr_arg(project_folder)}'
    ])
    return base

def cmd_not_standalone_performance(project_folder, platform, api, test_platform_args):
    base = _cmd_base(project_folder, platform["components"])

    extra_cmds = extra_perf_cmd(project_folder)
    unity_config = install_unity_config(project_folder)
    if project_folder.lower() == "BoatAttack".lower():
        x=0
        for y in extra_cmds:
            base.insert(x, y)
            x += 1
        base.extend(unity_config)
 
    base.extend([
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && utr {test_platform_args} --platform=StandaloneWindows64 --report-performance-data --performance-project-id=URP_Performance --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS}'
    ])
    base[-1] += f' --extra-editor-arg="{api["cmd"]}"' if api["name"] != ""  else ''
    return base

def cmd_standalone_performance(project_folder, platform, api, test_platform_args):
    base = [
        f'curl -s https://artifactory.internal.unity3d.com/core-automation/tools/utr-standalone/utr.bat --output {TEST_PROJECTS_DIR}/{project_folder}/utr.bat'
    ]

    extra_cmds = extra_perf_cmd(project_folder)
    unity_config = install_unity_config(project_folder)
    if project_folder.lower() == "BoatAttack".lower():
        x=0
        for y in extra_cmds:
            base.insert(x, y)
            x += 1
        base.extend(unity_config)

    if project_folder.lower() == 'UniversalGraphicsTest'.lower():
        base.append('cd Tools && powershell -command ". .\\Unity.ps1; Set-ScreenResolution -width 1920 -Height 1080"')

    base.extend([
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && utr {test_platform_args} --platform=StandaloneWindows64 --report-performance-data --performance-project-id=URP_Performance --artifacts_path={PATH_TEST_RESULTS} --timeout=1200 --player-load-path=../../{PATH_PLAYERS} --player-connection-ip=auto'
    ])
    return base

def cmd_standalone_build_performance(project_folder, platform, api, test_platform_args):
    base = _cmd_base(project_folder, platform["components"])
    extra_cmds = extra_perf_cmd(project_folder)
    unity_config = install_unity_config(project_folder)
    if project_folder.lower() == "BoatAttack".lower():
        x=0
        for y in extra_cmds:
            base.insert(x, y)
            x += 1
        base.extend(unity_config)

    base.extend([
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && utr --suite=playmode --platform=StandaloneWindows64 --extra-editor-arg="-executemethod" --extra-editor-arg="EditorTests.CliSetup" --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS} --timeout=1200 --player-save-path=../../{PATH_PLAYERS} --build-only'
    ])
    return base

def _get_extra_utr_arg(project_folder):
    return ' --compilation-errors-as-warnings' if project_folder.lower() in ['universalhybridtest', 'hdrp_hybridtests'] else ''

def extra_perf_cmd(project_folder):   
    perf_list = [
        f'git clone https://github.com/seanstolberg-unity/BoatAttack.git -b performance/urp TestProjects/{project_folder}',
        f'Xcopy /E /I \"com.unity.render-pipelines.core\" \"{TEST_PROJECTS_DIR}/{project_folder}/Packages/com.unity.render-pipelines.core\" /Y',
        f'Xcopy /E /I \"com.unity.render-pipelines.universal\" \"{TEST_PROJECTS_DIR}/{project_folder}/Packages/com.unity.render-pipelines.universal\" /Y',
        f'Xcopy /E /I \"com.unity.shadergraph\" \"{TEST_PROJECTS_DIR}/{project_folder}/Packages/com.unity.shadergraph\" /Y'
        ]
    return perf_list

def install_unity_config(project_folder):
    cmds = [
        f'choco source add -n Unity -s https://artifactory.prd.it.unity3d.com/artifactory/api/nuget/unity-choco-local',
        f'choco install unity-config',
		f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project set registry candidates --project-path .',
		f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project set enable-lock-file true',
		f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project remove dependency com.unity.render-pipelines.universal',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency com.unity.test-framework.performance@2.3.1-preview --project-path .',
		f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency com.unity.test-framework.utp-reporter@1.0.2-preview --project-path .',
		f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency com.unity.test-framework.build@0.0.1-preview.12 --project-path .',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency \"com.unity.test.performance.runtimesettings@ssh://git@github.cds.internal.unity3d.com/unity/com.unity.test.performance.runtimesettings.git\"',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency \"com.unity.test.metadata-manager@ssh://git@github.cds.internal.unity3d.com/unity/com.unity.test.metadata-manager.git\"',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency \"com.unity.cli-project-setup@ssh://git@github.cds.internal.unity3d.com/unity/com.unity.cli-project-setup.git\"',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency \"com.unity.testing.graphics-performance@ssh://git@github.cds.internal.unity3d.com/unity/com.unity.testing.graphics-performance.git\"',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency com.unity.shaderanalysis@\"ssh://git@github.cds.internal.unity3d.com/unity/com.unity.shaderanalysis.git\"',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency \"unity.graphictests.performance.universal@ssh://git@github.cds.internal.unity3d.com/unity/unity.graphictests.performance.universal.git\"',
		f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency \"com.unity.test-framework@1.2.1-preview.1\"',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add testable com.unity.testing.graphics-performance --project-path .',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add testable com.unity.cli-project-setup  --project-path .',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add testable com.unity.test.performance.runtimesettings  --project-path .',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add testable test.metadata-manager  --project-path .',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add testable unity.graphictests.performance.universal  --project-path .'
    ]
    return cmds