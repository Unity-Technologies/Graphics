from ...shared.constants import TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS, PATH_PLAYERS, UTR_INSTALL_URL, UNITY_DOWNLOADER_CLI_URL
from ruamel.yaml.scalarstring import PreservedScalarString as pss
from ...shared.utr_utils import utr_editmode_flags, utr_playmode_flags, utr_standalone_split_flags,utr_standalone_not_split_flags, utr_standalone_build_flags


def _cmd_base(project_folder, components):
    return []

def cmd_editmode(project_folder, platform, api, test_platform_args):
    raise Exception('iPhone [editmode]: only standalone available')

def cmd_playmode(project_folder, platform, api, test_platform_args):
    raise Exception('iPhone [playmode]: only standalone available')

def cmd_standalone(project_folder, platform, api, test_platform_args):

    utr_args = utr_standalone_split_flags(platform_spec='', platform='iOS', player_load_path='players',player_conn_ip=None, timeout=None)
    utr_args.extend(test_platform_args)

    return [
        f'curl -s {UTR_INSTALL_URL} --output utr',        
        f'chmod +x ./utr',
        f'./utr {" ".join(utr_args)}'
    ]

        
def cmd_standalone_build(project_folder, platform, api, test_platform_args):

    utr_args = utr_standalone_build_flags(platform_spec='', platform='iOS', testproject=f'{TEST_PROJECTS_DIR}/{project_folder}', player_save_path=PATH_PLAYERS, timeout=None)
    utr_args.extend(test_platform_args)

    components = platform["components"]
    return [
        f'pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
        f'unity-downloader-cli --source-file $YAMATO_SOURCE_DIR/{PATH_UNITY_REVISION} {"".join([f"-c {c} " for c in components])}  --wait --published-only',
        f'curl -s {UTR_INSTALL_URL} --output utr',
        f'chmod +x ./utr',
        f'./utr {" ".join(utr_args)}'
     ]
