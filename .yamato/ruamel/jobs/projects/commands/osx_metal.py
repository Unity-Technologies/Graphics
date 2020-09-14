
from ruamel.yaml.scalarstring import PreservedScalarString as pss
from ...shared.constants import REPOSITORY_NAME, TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS, VAR_UPM_REGISTRY, UNITY_DOWNLOADER_CLI_URL, UTR_INSTALL_URL, get_unity_downloader_cli_cmd

def _cmd_base(project_folder, platform, editor):
    return [ 
        f'curl -s {UTR_INSTALL_URL} --output {TEST_PROJECTS_DIR}/{project_folder}/utr',
        f'chmod +x {TEST_PROJECTS_DIR}/{project_folder}/utr',
        f'ssh -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" bokken@$BOKKEN_DEVICE_IP "bash -lc \'pip3 install --user unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade\'"',
        f'scp -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" -r $YAMATO_SOURCE_DIR bokken@$BOKKEN_DEVICE_IP:~/{REPOSITORY_NAME}',
        f'scp -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" ~/.ssh/id_rsa_macmini bokken@$BOKKEN_DEVICE_IP:~/.ssh/id_rsa_macmini',
        f'ssh -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" bokken@$BOKKEN_DEVICE_IP \'/Users/bokken/Library/Python/3.7/bin/unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"], git_root=True) } {"".join([f"-c {c} " for c in platform["components"]])} --wait --published-only\''
    ]


def cmd_not_standalone(project_folder, platform, api, test_platform_args, editor):
    base = _cmd_base(project_folder, platform, editor)
    base.extend([ 
        pss(f'''
        ssh -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" bokken@$BOKKEN_DEVICE_IP "export UPM_REGISTRY={VAR_UPM_REGISTRY}; echo \$UPM_REGISTRY; cd ~/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project_folder} && ~/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project_folder}/utr {test_platform_args} --testproject=/Users/bokken/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project_folder} --editor-location=/Users/bokken/.Editor --artifacts_path=/Users/bokken/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project_folder}/{PATH_TEST_RESULTS}{_get_extra_utr_arg(project_folder)}"
        UTR_RESULT=$? 
        mkdir -p {TEST_PROJECTS_DIR}/{project_folder}/{PATH_TEST_RESULTS}/
        scp -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" -r bokken@$BOKKEN_DEVICE_IP:/Users/bokken/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project_folder}/{PATH_TEST_RESULTS}/ {TEST_PROJECTS_DIR}/{project_folder}/{PATH_TEST_RESULTS}/
        exit $UTR_RESULT''')
     ])
    return base

def cmd_standalone(project_folder, platform, api, test_platform_args, editor):
    base = _cmd_base(project_folder, platform, editor)
    base.extend([ 
        pss(f'''
        ssh -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" bokken@$BOKKEN_DEVICE_IP "export UPM_REGISTRY={VAR_UPM_REGISTRY}; echo \$UPM_REGISTRY; cd ~/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project_folder} && ~/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project_folder}/utr {test_platform_args}OSX  --testproject=/Users/bokken/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project_folder} --editor-location=/Users/bokken/.Editor --artifacts_path=/Users/bokken/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project_folder}/{PATH_TEST_RESULTS} --timeout=2400{_get_extra_utr_arg(project_folder)}"
        UTR_RESULT=$? 
        mkdir -p {TEST_PROJECTS_DIR}/{project_folder}/{PATH_TEST_RESULTS}/
        scp -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" -r bokken@$BOKKEN_DEVICE_IP:/Users/bokken/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project_folder}/{PATH_TEST_RESULTS}/ {TEST_PROJECTS_DIR}/{project_folder}/{PATH_TEST_RESULTS}/
        exit $UTR_RESULT''')
     ])
    return base

def cmd_standalone_build(project_folder, platform, api, test_platform_args, editor):
    raise NotImplementedError('osx_metal: standalone_split set to true but build commands not specified')


def _get_extra_utr_arg(project_folder):
    return ' --compilation-errors-as-warnings' if project_folder.lower() in ['universalhybridtest', 'hdrp_hybridtests'] else ''