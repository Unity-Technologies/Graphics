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
    if project_folder.lower() == "BoatAttack".lower():
        base.insert(0, extra_cmds)
 
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
    if project_folder.lower() == "BoatAttack".lower():
        base.insert(0, extra_cmds)

    if project_folder.lower() == 'UniversalGraphicsTest'.lower():
        base.append('cd Tools && powershell -command ". .\\Unity.ps1; Set-ScreenResolution -width 1920 -Height 1080"')

    base.extend([
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && utr {test_platform_args} --platform=StandaloneWindows64 --report-performance-data --performance-project-id=URP_Performance --artifacts_path={PATH_TEST_RESULTS} --timeout=1200 --player-load-path=../../{PATH_PLAYERS} --player-connection-ip=auto'
    ])
    return base

def cmd_standalone_build_performance(project_folder, platform, api, test_platform_args):
    base = _cmd_base(project_folder, platform["components"])
    extra_cmds = extra_perf_cmd(project_folder)
    if project_folder.lower() == "BoatAttack".lower():
        base.insert(0, extra_cmds)

    base.extend([
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && utr --suite=playmode --platform=StandaloneWindows64 --extra-editor-arg="-executemethod" --extra-editor-arg="CustomBuild.BuildWindows{api["name"]}Linear" --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS} --timeout=1200 --player-save-path=../../{PATH_PLAYERS} --build-only'
    ])
    return base

def _get_extra_utr_arg(project_folder):
    return ' --compilation-errors-as-warnings' if project_folder.lower() in ['universalhybridtest', 'hdrp_hybridtests'] else ''

def extra_perf_cmd(project_folder):
    perf_list = [
        f'git clone https://github.com/seanstolberg-unity/BoatAttack.git -b seans/add-perf-tests-2 TestProjects/{project_folder}',
        f'Xcopy /E /I \"com.unity.render-pipelines.core\" \"{TEST_PROJECTS_DIR}/{project_folder}/Packages/com.unity.render-pipelines.core\" /Y',
        f'Xcopy /E /I \"com.unity.render-pipelines.universal\" \"{TEST_PROJECTS_DIR}/{project_folder}/Packages/com.unity.render-pipelines.universal\" /Y',
        f'Xcopy /E /I \"com.unity.shadergraph\" \"{TEST_PROJECTS_DIR}/{project_folder}/Packages/com.unity.shadergraph\" /Y',
        f'Xcopy /E /I \"com.unity.shaderanalysis\" \"{TEST_PROJECTS_DIR}/{project_folder}/Packages/com.unity.shaderanalysis\" /Y'
        ]
    return perf_list
