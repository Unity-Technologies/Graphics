from ...shared.constants import TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS, PATH_PLAYERS, UTR_INSTALL_URL, UNITY_DOWNLOADER_CLI_URL
from ruamel.yaml.scalarstring import PreservedScalarString as pss

def _cmd_base(project_folder, components):
    return []


def cmd_not_standalone(project_folder, platform, api, test_platform_args):
    raise Exception('iPhone: only standalone available')

def cmd_standalone(project_folder, platform, api, test_platform_args):
    return [
        f'curl -s {UTR_INSTALL_URL} --output utr',        
        f'chmod +x ./utr',
        f'./utr --suite=playmode --platform=iOS --player-load-path={PATH_PLAYERS} --artifacts_path={PATH_TEST_RESULTS}'
    ]

        
def cmd_standalone_build(project_folder, platform, api, test_platform_args):
    components = platform["components"]
    return [
        f'pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
        f'unity-downloader-cli --source-file $YAMATO_SOURCE_DIR/{PATH_UNITY_REVISION} {"".join([f"-c {c} " for c in components])}  --wait --published-only',
        f'curl -s {UTR_INSTALL_URL} --output utr',
        f'chmod +x ./utr',
        f'./utr --suite=playmode --platform=iOS --editor-location=.Editor --testproject={TEST_PROJECTS_DIR}/{project_folder} --player-save-path={PATH_PLAYERS} --artifacts_path={PATH_TEST_RESULTS} --build-only'
     ]