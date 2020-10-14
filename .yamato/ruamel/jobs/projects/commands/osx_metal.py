
from ruamel.yaml.scalarstring import PreservedScalarString as pss
from ...shared.constants import REPOSITORY_NAME, TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS, VAR_UPM_REGISTRY, UNITY_DOWNLOADER_CLI_URL, UTR_INSTALL_URL,get_unity_downloader_cli_cmd, get_timeout
from ...shared.utr_utils import utr_editmode_flags, utr_playmode_flags, utr_standalone_not_split_flags

def _cmd_base(project_folder, platform, utr_flags, editor):
    return [ 
        f'curl -s {UTR_INSTALL_URL} --output {TEST_PROJECTS_DIR}/{project_folder}/utr',
        f'chmod +x {TEST_PROJECTS_DIR}/{project_folder}/utr',
        f'ssh -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" bokken@$BOKKEN_DEVICE_IP "bash -lc \'pip3 install --user unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade\'"',
        f'scp -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" -r $YAMATO_SOURCE_DIR bokken@$BOKKEN_DEVICE_IP:~/{REPOSITORY_NAME}',
        f'scp -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" ~/.ssh/id_rsa_macmini bokken@$BOKKEN_DEVICE_IP:~/.ssh/id_rsa_macmini',
        f'ssh -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" bokken@$BOKKEN_DEVICE_IP \'$(/usr/local/bin/python3 -m site --user-base)/bin/unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"], git_root=True) } {"".join([f"-c {c} " for c in platform["components"]])} --wait --published-only\'',
        pss(f'''
        ssh -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" bokken@$BOKKEN_DEVICE_IP "export UPM_REGISTRY={VAR_UPM_REGISTRY}; echo \$UPM_REGISTRY; cd ~/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project_folder} && ~/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project_folder}/utr {" ".join(utr_flags)}"
        UTR_RESULT=$? 
        mkdir -p {TEST_PROJECTS_DIR}/{project_folder}/{PATH_TEST_RESULTS}/
        scp -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" -r bokken@$BOKKEN_DEVICE_IP:/Users/bokken/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project_folder}/{PATH_TEST_RESULTS}/ {TEST_PROJECTS_DIR}/{project_folder}/{PATH_TEST_RESULTS}/
        exit $UTR_RESULT''')
    ]


def cmd_editmode(project_folder, platform, api, test_platform, editor):

    utr_args = utr_editmode_flags(
        testproject=f'/Users/bokken/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project_folder}',
        editor_location=f'/Users/bokken/.Editor',
        artifacts_path=f'/Users/bokken/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project_folder}/{PATH_TEST_RESULTS}'
    )
    utr_args.extend(test_platform["extra_utr_flags"])
    return  _cmd_base(project_folder, platform, utr_args, editor)

def cmd_playmode(project_folder, platform, api, test_platform, editor):

    utr_args = utr_playmode_flags(
        testproject=f'/Users/bokken/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project_folder}',
        editor_location=f'/Users/bokken/.Editor',
        artifacts_path=f'/Users/bokken/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project_folder}/{PATH_TEST_RESULTS}'
    )
    utr_args.extend(test_platform["extra_utr_flags"])
    return  _cmd_base(project_folder, platform, utr_args, editor)


def cmd_standalone(project_folder, platform, api, test_platform, editor):

    utr_args = utr_standalone_not_split_flags(
        platform='Standalone',
        platform_spec='OSX',
        testproject=f'/Users/bokken/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project_folder}',
        editor_location=f'/Users/bokken/.Editor',
        artifacts_path=f'/Users/bokken/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project_folder}/{PATH_TEST_RESULTS}'
    )
    utr_args.extend(test_platform["extra_utr_flags"])
    utr_args.append(f'--timeout={get_timeout(test_platform, "OSX_Metal")}')
    return  _cmd_base(project_folder, platform, utr_args, editor)


def cmd_standalone_build(project_folder, platform, api, test_platform, editor):
    raise NotImplementedError('osx_metal: standalone_split set to true but build commands not specified')
