from ...shared.constants import TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS, PATH_PLAYERS, UTR_INSTALL_URL, UNITY_DOWNLOADER_CLI_URL, get_unity_downloader_cli_cmd, get_timeout
from ruamel.yaml.scalarstring import PreservedScalarString as pss
from ...shared.utr_utils import  get_repeated_utr_calls,switch_var_sign


def _cmd_base(project, platform, editor):
    return []

def cmd_editmode(project, platform, api, test_platform, editor, build_config, color_space):
    
    base = [
        f'brew tap --force-auto-update unity/unity git@github.cds.internal.unity3d.com:unity/homebrew-unity.git',
        f'brew install unity/unity/unity-downloader-cli',
        f'unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"]) } {"".join([f"-c {c} " for c in platform["components"]])}  --wait --published-only',
        f'curl -s {UTR_INSTALL_URL} --output utr',
        f'chmod +x ./utr'
     ]

    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project["folder"])
    for utr_args in utr_calls:
        base.append(
        pss(f'''
         export GIT_REVISIONDATE=`git rev-parse HEAD | git show -s --format=%cI`
        ./utr {" ".join(utr_args)}'''))

    base = add_project_commands(project) + base
    return base

def cmd_playmode(project, platform, api, test_platform, editor, build_config, color_space):

    base = [
        f'brew tap --force-auto-update unity/unity git@github.cds.internal.unity3d.com:unity/homebrew-unity.git',
        f'brew install unity-downloader-cli',
        f'unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"]) } {"".join([f"-c {c} " for c in platform["components"]])}  --wait --published-only',
        f'curl -s {UTR_INSTALL_URL} --output utr',
        f'chmod +x ./utr'
     ]

    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project["folder"])
    for utr_args in utr_calls:
        base.append(
        pss(f'''
         export GIT_REVISIONDATE=`git rev-parse HEAD | git show -s --format=%cI`
        ./utr {" ".join(utr_args)}'''))
    
    base = add_project_commands(project) + base
    return base

def cmd_standalone(project, platform, api, test_platform, editor, build_config, color_space):

    base = [
        f'curl -s {UTR_INSTALL_URL} --output utr',
        f'chmod +x ./utr'
     ]

    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project["folder"])
    for utr_args in utr_calls:
        base.append(
        pss(f'''
         export GIT_REVISIONDATE=`git rev-parse HEAD | git show -s --format=%cI`
        ./utr {" ".join(utr_args)}'''))
     
    return base

        
def cmd_standalone_build(project, platform, api, test_platform, editor, build_config, color_space):

    if "Performance" not in project["name"]:
        base = [
            f'brew tap --force-auto-update unity/unity git@github.cds.internal.unity3d.com:unity/homebrew-unity.git',
            f'brew install unity/unity/unity-downloader-cli',
            f'unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"]) } {"".join([f"-c {c} " for c in platform["components"]])}  --wait --published-only',
            f'curl -s {UTR_INSTALL_URL} --output utr',
            f'chmod +x ./utr'
        ]
    else:
        base = [
            f'pip install unity-downloader-cli --index-url https://artifactory.prd.it.unity3d.com/artifactory/api/pypi/pypi/simple --upgrade',
            f'unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"]) } {"".join([f"-c {c} " for c in platform["components"]])}  --wait --published-only',
            f'curl -s {UTR_INSTALL_URL} --output utr',
            f'chmod +x ./utr'
        ]
    
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project["folder"], utr_flags_key="utr_flags_build")
    for utr_args in utr_calls:
        base.append(
        pss(f'''
         export GIT_REVISIONDATE=`git rev-parse HEAD | git show -s --format=%cI`
        ./utr {" ".join(utr_args)}'''))
    

    base = add_project_commands(project) + base
    return base


def add_project_commands(project):
    cmds = []
    if project.get("url"):
        cmds.extend([
            f'git clone {project["url"]} -b {switch_var_sign(project["branch"])} {TEST_PROJECTS_DIR}/{project["folder"]}',
            f'cd {TEST_PROJECTS_DIR}/{project["folder"]} && git checkout {switch_var_sign(project["revision"])}'
        ])
    if project.get("unity_config_commands"):
        cmds.extend([
            f'brew tap --force-auto-update unity/unity git@github.cds.internal.unity3d.com:unity/homebrew-unity.git',
            f'brew install unity-config',
        ])
        for unity_config in project["unity_config_commands"]:
            cmds.append(f'cd {TEST_PROJECTS_DIR}/{project["folder"]} && {unity_config}')
    return cmds