from ...shared.constants import TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS, PATH_PLAYERS, UNITY_DOWNLOADER_CLI_URL, UTR_INSTALL_URL
from ...shared.utr_utils import utr_editmode_flags, utr_playmode_flags, utr_standalone_split_flags, utr_standalone_build_flags

def _cmd_base(project_folder, components, utr_flags):
    return [
        f'curl -s {UTR_INSTALL_URL}.bat --output {TEST_PROJECTS_DIR}/{project_folder}/utr.bat',
        f'pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-downloader-cli --source-file ../../{PATH_UNITY_REVISION} {"".join([f"-c {c} " for c in components])} --wait --published-only',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && utr {" ".join(utr_flags)}'
    ]


def cmd_editmode(project_folder, platform, api, test_platform_args):
    utr_args = utr_editmode_flags()
    utr_args.extend(test_platform_args)
    if api["name"] != "":
        utr_args.append(f'--extra-editor-arg="{api["cmd"]}"')

    return  _cmd_base(project_folder, platform["components"], utr_args)


def cmd_playmode(project_folder, platform, api, test_platform_args):
    utr_args = utr_playmode_flags()
    utr_args.extend(test_platform_args)
    if api["name"] != "":
        utr_args.append(f'--extra-editor-arg="{api["cmd"]}"')

    return  _cmd_base(project_folder, platform["components"], utr_args)

def cmd_standalone(project_folder, platform, api, test_platform_args):
    utr_args = utr_standalone_split_flags("Windows64")
    utr_args.extend(test_platform_args)

    base = [f'curl -s {UTR_INSTALL_URL}.bat --output {TEST_PROJECTS_DIR}/{project_folder}/utr.bat']
    if project_folder.lower() == 'UniversalGraphicsTest'.lower():
        base.append('cd Tools && powershell -command ". .\\Unity.ps1; Set-ScreenResolution -width 1920 -Height 1080"')
    base.append(f'cd {TEST_PROJECTS_DIR}/{project_folder} && utr {" ".join(utr_args)}')
    
    return base


def cmd_standalone_build(project_folder, platform, api, test_platform_args):
    utr_args = utr_standalone_build_flags("Windows64")
    utr_args.extend(test_platform_args)
    utr_args.extend(['--extra-editor-arg="-executemethod"', f'--extra-editor-arg="CustomBuild.BuildWindows{api["name"]}Linear"'])
    
    return _cmd_base(project_folder, platform["components"], utr_args)

