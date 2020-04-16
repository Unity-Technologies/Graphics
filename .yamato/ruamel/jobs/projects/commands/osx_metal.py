
from ruamel.yaml.scalarstring import PreservedScalarString as pss
from ...utils.constants import REPOSITORY_NAME, TEST_PROJECTS_DIR

def _cmd_base(project, components):
    return [ 
        f'git clone git@github.cds.internal.unity3d.com:unity/utr.git {TEST_PROJECTS_DIR}/{project["folder"]}/utr',
        f'ssh -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" bokken@$BOKKEN_DEVICE_IP "bash -lc \'pip3 install --user unity-downloader-cli --extra-index-url https://artifactory.internal.unity3d.com/api/pypi/common-python/simple --upgrade\'"',
        f'scp -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" -r $YAMATO_SOURCE_DIR bokken@$BOKKEN_DEVICE_IP:~/{REPOSITORY_NAME}',
        f'scp -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" ~/.ssh/id_rsa_macmini bokken@$BOKKEN_DEVICE_IP:~/.ssh/id_rsa_macmini',
        f'ssh -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" bokken@$BOKKEN_DEVICE_IP \'/Users/bokken/Library/Python/3.7/bin/unity-downloader-cli --source-file ~/{REPOSITORY_NAME}/unity_revision.txt {"".join([f"-c {c} " for c in components])} --wait --published-only\''
    ]


def cmd_not_standalone(project, platform, api, test_platform_args):
    base = _cmd_base(project, platform["components"])
    base.extend([ 
        pss(f'''
        ssh -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" bokken@$BOKKEN_DEVICE_IP 'cd ~/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project["folder"]} && ~/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project["folder"]}/utr/utr {test_platform_args} --testproject=/Users/bokken/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project["folder"]} --editor-location=/Users/bokken/.Editor --artifacts_path=/Users/bokken/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project["folder"]}/test-results\'
        UTR_RESULT=$? 
        mkdir -p {TEST_PROJECTS_DIR}/{project["folder"]}/test-results/
        scp -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" -r bokken@$BOKKEN_DEVICE_IP:/Users/bokken/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project["folder"]}/test-results/ {TEST_PROJECTS_DIR}/{project["folder"]}/test-results/
        exit $UTR_RESULT''')
     ])
    return base

def cmd_standalone(project, platform, api, test_platform_args):
    base = _cmd_base(project, platform["components"])
    base.extend([ 
        pss(f'''
        ssh -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" bokken@$BOKKEN_DEVICE_IP \'cd ~/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project["folder"]} && ~/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project["folder"]}/utr/utr {test_platform_args}OSX  --testproject=/Users/bokken/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project["folder"]} --editor-location=/Users/bokken/.Editor --artifacts_path=/Users/bokken/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project["folder"]}/test-results --timeout=1400\'
        UTR_RESULT=$? 
        mkdir -p {TEST_PROJECTS_DIR}/{project["folder"]}/test-results/
        scp -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" -r bokken@$BOKKEN_DEVICE_IP:/Users/bokken/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project["folder"]}/test-results/ {TEST_PROJECTS_DIR}/{project["folder"]}/test-results/
        exit $UTR_RESULT''')
     ])
    return base

def cmd_standalone_build(project, platform, api, test_platform_args):
    raise Exception('osx_metal: standalone_split set to true but build commands not specified')

