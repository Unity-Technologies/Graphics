from ...shared.constants import TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS, PATH_PLAYERS, UTR_INSTALL_URL, UNITY_DOWNLOADER_CLI_URL, get_unity_downloader_cli_cmd, get_timeout
from ruamel.yaml.scalarstring import PreservedScalarString as pss
from ...shared.utr_utils import  get_repeated_utr_calls


def _cmd_base(project_folder, project_url, project_branch, project_revision, project_dependencies, platform, editor):
    return []

def cmd_editmode(project_folder, project_url, project_branch, project_revision, project_dependencies, platform, api, test_platform, editor, build_config, color_space):
    
    base = [
        f'pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
        f'unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"]) } {"".join([f"-c {c} " for c in platform["components"]])}  --wait --published-only',
        f'curl -s {UTR_INSTALL_URL} --output utr',
        f'chmod +x ./utr'
     ]

    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project_folder)
    for utr_args in utr_calls:
        base.append(
        pss(f'''
         export GIT_REVISIONDATE=`git rev-parse HEAD | git show -s --format=%cI`
        ./utr {" ".join(utr_args)}'''))

    if project_folder.lower() == "BoatAttack".lower():
        base = extra_perf_cmd(project_folder) + install_unity_config(project_folder) + base
    return base

def cmd_playmode(project_folder, project_url, project_branch, project_revision, project_dependencies, platform, api, test_platform, editor, build_config, color_space):

    base = [
        f'pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
        f'unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"]) } {"".join([f"-c {c} " for c in platform["components"]])}  --wait --published-only',
        f'curl -s {UTR_INSTALL_URL} --output utr',
        f'chmod +x ./utr'
     ]

    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project_folder)
    for utr_args in utr_calls:
        base.append(
        pss(f'''
         export GIT_REVISIONDATE=`git rev-parse HEAD | git show -s --format=%cI`
        ./utr {" ".join(utr_args)}'''))
    
    if project_folder.lower() == "BoatAttack".lower():
        base = extra_perf_cmd(project_folder) + install_unity_config(project_folder) + base
    return base

def cmd_standalone(project_folder, project_url, project_branch, project_revision, project_dependencies, platform, api, test_platform, editor, build_config, color_space):

    base = [
        f'curl -s {UTR_INSTALL_URL} --output utr',
        f'chmod +x ./utr'
     ]

    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project_folder)
    for utr_args in utr_calls:
        base.append(
        pss(f'''
         export GIT_REVISIONDATE=`git rev-parse HEAD | git show -s --format=%cI`
        ./utr {" ".join(utr_args)}'''))
     
    return base

        
def cmd_standalone_build(project_folder, project_url, project_branch, project_revision, project_dependencies, platform, api, test_platform, editor, build_config, color_space):

    base = [
        f'pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
        f'unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"]) } {"".join([f"-c {c} " for c in platform["components"]])}  --wait --published-only',
        f'curl -s {UTR_INSTALL_URL} --output utr',
        f'chmod +x ./utr'
     ]
    
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project_folder, utr_flags_key="utr_flags_build")
    for utr_args in utr_calls:
        base.append(
        pss(f'''
         export GIT_REVISIONDATE=`git rev-parse HEAD | git show -s --format=%cI`
        ./utr {" ".join(utr_args)}'''))
    

    if project_folder.lower() == "BoatAttack".lower():
        base = extra_perf_cmd(project_folder) + install_unity_config(project_folder) + base
    return base

def extra_perf_cmd(project_folder, project_url, project_branch, project_revision):   
    perf_list = [
        f'git clone {project_url} -b {project_branch} TestProjects/{project_folder}',
        f'cd TestProjects/{project_folder} && git checkout {project_revision}'
        ]
    return perf_list

def install_unity_config(project_folder, project_dependencies):
    cmds = [
        f'brew tap --force-auto-update unity/unity git@github.cds.internal.unity3d.com:unity/homebrew-unity.git',
        f'brew install unity-config',
    ]

    for dependency in project_dependencies:
        cmds.append(dependency)

    return cmds