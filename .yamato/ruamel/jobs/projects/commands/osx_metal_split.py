
from ruamel.yaml.scalarstring import PreservedScalarString as pss
from ...shared.constants import REPOSITORY_NAME, TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS, PATH_PLAYERS

def _cmd_base(project, components):
    return [   ]


def cmd_not_standalone(project_folder, platform, api, test_platform_args):
    return [ 
        f'git clone git@github.cds.internal.unity3d.com:unity/utr.git {TEST_PROJECTS_DIR}/{project_folder}/utr',
        f'ssh -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" bokken@$BOKKEN_DEVICE_IP "bash -lc \'pip3 install --user unity-downloader-cli --extra-index-url https://artifactory.internal.unity3d.com/api/pypi/common-python/simple --upgrade\'"',
        f'scp -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" -r $YAMATO_SOURCE_DIR bokken@$BOKKEN_DEVICE_IP:~/{REPOSITORY_NAME}',
        f'scp -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" ~/.ssh/id_rsa_macmini bokken@$BOKKEN_DEVICE_IP:~/.ssh/id_rsa_macmini',
        f'ssh -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" bokken@$BOKKEN_DEVICE_IP \'/Users/bokken/Library/Python/3.7/bin/unity-downloader-cli --source-file ~/{REPOSITORY_NAME}/{PATH_UNITY_REVISION} {"".join([f"-c {c} " for c in platform["components"]])} --wait --published-only\'',
        pss(f'''
        set GIT_BRANCH=$GIT_BRANCH
        set GIT_REVISION=$GIT_REVISION
        set YAMATO_JOB_ID=$YAMATO_JOB_ID
        ssh -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" bokken@$BOKKEN_DEVICE_IP 'cd ~/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project_folder} && ~/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project_folder}/utr/utr { test_platform_args } --testproject=/Users/bokken/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project_folder} --editor-location=/Users/bokken/.Editor --artifacts_path=/Users/bokken/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project_folder}/{PATH_TEST_RESULTS}'
        
        UTR_RESULT=$? 
        mkdir -p {TEST_PROJECTS_DIR}/{project_folder}/{PATH_TEST_RESULTS}/
        scp -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" -r bokken@$BOKKEN_DEVICE_IP:/Users/bokken/{REPOSITORY_NAME}/{TEST_PROJECTS_DIR}/{project_folder}/{PATH_TEST_RESULTS}/ {TEST_PROJECTS_DIR}/{project_folder}/{PATH_TEST_RESULTS}/
        exit $UTR_RESULT''')
     ]

def cmd_standalone(project_folder, platform, api, test_platform_args):
    return [ 
        f'curl -s https://artifactory.internal.unity3d.com/core-automation/tools/utr-standalone/utr --output utr',
        f'chmod +x ./utr',
        f'scp -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" -r ../{REPOSITORY_NAME}/ bokken@$BOKKEN_DEVICE_IP:~/{REPOSITORY_NAME}',
        f'scp -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" ~/.ssh/id_rsa_macmini bokken@$BOKKEN_DEVICE_IP:~/.ssh/id_rsa_macmini',
        pss(f'''
        export GIT_BRANCH=$GIT_BRANCH
        export GIT_REVISION=$GIT_REVISION
        export YAMATO_JOB_ID=$YAMATO_JOB_ID
        export YAMATO_JOBDEFINITION_NAME=$YAMATO_JOBDEFINITION_NAME
        export YAMATO_PROJECT_ID=$YAMATO_PROJECT_ID
        ssh -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" bokken@$BOKKEN_DEVICE_IP -T "./utr {test_platform_args}OSX --player-load-path=~/{REPOSITORY_NAME}/{PATH_PLAYERS} --artifacts_path=~/{REPOSITORY_NAME}/build/{PATH_TEST_RESULTS} --player-connection-ip=127.0.0.1"
        
        UTR_RESULT=$?
        mkdir -p build/{PATH_TEST_RESULTS}/
        scp -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" -r bokken@$BOKKEN_DEVICE_IP:~/{REPOSITORY_NAME}/build/{PATH_TEST_RESULTS}/ build/{PATH_TEST_RESULTS}/
        exit $UTR_RESULT''')
     ]

def cmd_standalone_build(project_folder, platform, api, test_platform_args):
    return  [
        f'git clone git@github.cds.internal.unity3d.com:unity/utr.git {TEST_PROJECTS_DIR}/{project_folder}/utr',
        f'pip install unity-downloader-cli --extra-index-url https://artifactory.internal.unity3d.com/api/pypi/common-python/simple --upgrade',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-downloader-cli --source-file ../../{PATH_UNITY_REVISION} {"".join([f"-c {c} " for c in platform["components"]])} --wait --published-only',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && utr/utr {test_platform_args}OSX --extra-editor-arg="-executemethod" --extra-editor-arg="CustomBuild.BuildOSXMetal" --testproject=. --editor-location=.Editor --artifacts_path=build-results --timeout=3600 --player-save-path={PATH_PLAYERS} --build-only'
    ]

