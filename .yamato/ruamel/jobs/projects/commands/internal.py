from ...shared.constants import TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS, PATH_PLAYERS, GITHUB_CDS_URL, UNITY_DOWNLOADER_CLI_URL, UTR_INSTALL_URL

def _cmd_base(project_folder, components, editor_revision):
    return [
        f'git clone {GITHUB_CDS_URL}/sophia/URP-Update-testing.git TestProjects/URP-Update-testing',
        f'curl -s {UTR_INSTALL_URL}.bat --output {TEST_PROJECTS_DIR}/URP-Update-testing/{project_folder}/utr.bat',
        f'pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
        f'Xcopy /E /I \"com.unity.render-pipelines.core\" \"{TEST_PROJECTS_DIR}/URP-Update-testing/{project_folder}/Packages/com.unity.render-pipelines.core\" /Y',
        f'Xcopy /E /I \"com.unity.render-pipelines.universal\" \"{TEST_PROJECTS_DIR}/URP-Update-testing/{project_folder}/Packages/com.unity.render-pipelines.universal\" /Y',
        f'Xcopy /E /I \"com.unity.shadergraph\" \"{TEST_PROJECTS_DIR}/URP-Update-testing/{project_folder}/Packages/com.unity.shadergraph\" /Y',
        f'cd {TEST_PROJECTS_DIR}/URP-Update-testing/{project_folder} && unity-downloader-cli -u { editor_revision } {"".join([f"-c {c} " for c in components])} --wait --published-only'
    ]


def cmd_not_standalone(project_folder, platform, api, test_platform_args, editor_revision):
    base = _cmd_base(project_folder, platform["components"], editor_revision)
    base.extend([
        f'cd {TEST_PROJECTS_DIR}/URP-Update-testing/{project_folder} && utr {test_platform_args} --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS}'
    ])
    base[-1] += f' --extra-editor-arg="{api["cmd"]}"' if api["name"] != ""  else ''
    return base

def cmd_standalone(project_folder, platform, api, test_platform_args, editor_revision):
    base = _cmd_base(project_folder, platform["components"], editor_revision)

    if project_folder.lower() == 'URP-Update-Testing'.lower():
        base.append('git clone {GITHUB_CDS_URL}/sophia/URP-Update-testing.git TestProjects')

    base.extend([
        f'cd {TEST_PROJECTS_DIR}/URP-Update-testing/{project_folder} && utr {test_platform_args}Windows64 --artifacts_path={PATH_TEST_RESULTS} --timeout=1200 --player-load-path=../../{PATH_PLAYERS} --player-connection-ip=auto'
    ])
    return base


def cmd_standalone_build(project_folder, platform, api, test_platform_args, editor_revision):
    base = _cmd_base(project_folder, platform["components"], editor_revision)
    base.extend([
        f'cd {TEST_PROJECTS_DIR}/URP-Update-testing/{project_folder} && utr {test_platform_args}Windows64 --extra-editor-arg="-executemethod" --extra-editor-arg="CustomBuild.BuildWindows{api}Linear" --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS} --timeout=1200 --player-save-path=../../{PATH_PLAYERS} --build-only'
    ])
    return base