from ...shared.constants import TEST_PROJECTS_DIR,PATH_UNITY_REVISION, PATH_TEST_RESULTS, PATH_PLAYERS, UTR_INSTALL_URL, UNITY_DOWNLOADER_CLI_URL, get_unity_downloader_cli_cmd,get_unity_downloader_cli_cmd

def _cmd_base(project_folder, platform, editor):
    return [ 
        f'sudo -H pip install --upgrade pip',
        f'sudo -H pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
        f'curl -s {UTR_INSTALL_URL} --output {TEST_PROJECTS_DIR}/{project_folder}/utr',
        f'chmod +x {TEST_PROJECTS_DIR}/{project_folder}/utr',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"], cd=True) } {"".join([f"-c {c} " for c in platform["components"]])} --wait --published-only'
    ]


def cmd_not_standalone(project_folder, platform, api, test_platform, editor):
    base = _cmd_base(project_folder, platform, editor)
    base.extend([ 
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && DISPLAY=:0.0 utr {test_platform["extra_utr_flags"]} --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS}'
     ])
    base[-1] += f' --extra-editor-arg="{platform["apis"][api]}"' if (api != "" and platform["apis"][api] != None)  else ''
    return base

def cmd_standalone(project_folder, platform, api, test_platform, editor):
    base = [
        f'curl -s {UTR_INSTALL_URL} --output {TEST_PROJECTS_DIR}/{project_folder}/utr',
        f'chmod +x {TEST_PROJECTS_DIR}/{project_folder}/utr',
    ]
    base.extend([
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && DISPLAY=:0.0 utr {test_platform["extra_utr_flags"]}Linux64 --artifacts_path={PATH_TEST_RESULTS} --timeout=1200 --player-load-path=.{PATH_PLAYERS} --player-connection-ip=auto'
      ])
    return base

def cmd_standalone_build(project_folder, platform, api, test_platform, editor):
    base = _cmd_base(project_folder, platform, editor)
    base.extend([
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && DISPLAY=:0.0 utr {test_platform["extra_utr_flags_build"]}Linux64 --extra-editor-arg="-executemethod" --extra-editor-arg="CustomBuild.BuildLinux{api["name"]}Linear" --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS} --timeout=1200 --player-save-path=.{PATH_PLAYERS} --build-only'
      ])
    return base

