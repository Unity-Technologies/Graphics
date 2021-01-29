
from ruamel.yaml.scalarstring import PreservedScalarString as pss
from ...shared.constants import REPOSITORY_NAME, TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS, VAR_UPM_REGISTRY, UNITY_DOWNLOADER_CLI_URL, UTR_INSTALL_URL,get_unity_downloader_cli_cmd, get_timeout
from ...shared.utr_utils import get_repeated_utr_calls, switch_var_sign

def _cmd_base(project, platform, utr_calls, editor):
    base = [ 
        f'curl -s {UTR_INSTALL_URL} --output {TEST_PROJECTS_DIR}/{project["folder"]}/utr',
        f'chmod +x {TEST_PROJECTS_DIR}/{project["folder"]}/utr',
        f'ssh -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" bokken@$BOKKEN_DEVICE_IP "bash -lc \'pip3 install unity-downloader-cli --user --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade\'"',
        f'scp -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" -r $YAMATO_SOURCE_DIR bokken@$BOKKEN_DEVICE_IP:~/{REPOSITORY_NAME}',
        f'scp -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" ~/.ssh/id_rsa_macmini bokken@$BOKKEN_DEVICE_IP:~/.ssh/id_rsa_macmini',
        f'ssh -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" bokken@$BOKKEN_DEVICE_IP \'$(python3 -m site --user-base)/bin/unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"], git_root=True) } {"".join([f"-c {c} " for c in platform["components"]])} --wait --published-only\'',
    ]

    for utr_args in utr_calls:
        base.append(pss(f'''
        ssh -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" bokken@$BOKKEN_DEVICE_IP "export UPM_REGISTRY={VAR_UPM_REGISTRY}; echo \$UPM_REGISTRY; cd ~/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project["folder"]} && ~/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project["folder"]}/utr {" ".join(utr_args)}"
        UTR_RESULT=$? 
        mkdir -p {TEST_PROJECTS_DIR}/{project["folder"]}/{PATH_TEST_RESULTS}/
        scp -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" -r bokken@$BOKKEN_DEVICE_IP:/Users/bokken/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project["folder"]}/{PATH_TEST_RESULTS}/ {TEST_PROJECTS_DIR}/{project["folder"]}/{PATH_TEST_RESULTS}/
        exit $UTR_RESULT'''))
    
    return base


def cmd_editmode(project, platform, api, test_platform, editor, build_config, color_space):
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project["folder"])
    base = _cmd_base(project, platform, utr_calls, editor)
    base = add_project_commands(project) + base

    return base

def cmd_playmode(project, platform, api, test_platform, editor, build_config, color_space):
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project["folder"])
    base = _cmd_base(project, platform, utr_calls, editor)
    base = add_project_commands(project) + base
    
    return base

def cmd_standalone(project, platform, api, test_platform, editor, build_config, color_space):
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project["folder"])
    base = _cmd_base(project, platform, utr_calls, editor)
    base = add_project_commands(project) + base

    return base


def cmd_standalone_build(project, platform, api, test_platform, editor, build_config, color_space):
    raise NotImplementedError('osx_metal: standalone_split set to true but build commands not specified')

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
            f'brew install unity-config'
        ])
        for unity_config in project["unity_config_commands"]:
            cmds.append(f'cd {TEST_PROJECTS_DIR}/{project["folder"]} && {unity_config}')
    return cmds

