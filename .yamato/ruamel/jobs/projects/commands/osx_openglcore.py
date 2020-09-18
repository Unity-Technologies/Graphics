from ...shared.constants import TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS, PATH_PLAYERS, UNITY_DOWNLOADER_CLI_URL, UTR_INSTALL_URL
from ...shared.utr_utils import utr_editmode_flags, utr_playmode_flags, utr_standalone_split_flags, utr_standalone_build_flags

def _cmd_base(project_folder, components, utr_flags):
    return [
        f'curl -s {UTR_INSTALL_URL} --output {TEST_PROJECTS_DIR}/{project_folder}/utr',
        f'chmod +x {TEST_PROJECTS_DIR}/{project_folder}/utr',
        f'pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-downloader-cli --source-file ../../{PATH_UNITY_REVISION} {"".join([f"-c {c} " for c in components])} --wait --published-only',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && ./utr {" ".join(utr_flags)}'
    ]


def cmd_editmode(project_folder, platform, api, test_platform):
    utr_args = utr_editmode_flags()
    utr_args.extend(test_platform["extra_utr_flags"])
    return  _cmd_base(project_folder, platform["components"], utr_args)


def cmd_playmode(project_folder, platform, api, test_platform):
    utr_args = utr_playmode_flags()
    utr_args.extend(test_platform["extra_utr_flags"])
    return  _cmd_base(project_folder, platform["components"], utr_args)

def cmd_standalone(project_folder, platform, api, test_platform):
    return []

def cmd_standalone_build(project_folder, platform, api, test_platform):
    return []