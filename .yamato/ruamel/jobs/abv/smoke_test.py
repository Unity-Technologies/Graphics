from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..utils.namer import *
from ..utils.constants import VAR_UPM_REGISTRY, TEST_PROJECTS_DIR
from ..utils.shared import add_custom_revision_var

default_agent = {
    'type':'Unity::VM',
    'image':'sdet/gamecode_win10:stable',
    'flavor':'b1.large'
}

default_agent_gpu = {
    'type':'Unity::VM::GPU',
    'image':'sdet/gamecode_win10:stable',
    'flavor':'b1.large'
}

def get_job_definition(editor, test_platform):  # only run for 2020.1 and trunk
    job = {
        'name': f'SRP Smoke Test - {test_platform["name"]}_{editor["version"]}',
        'agent': dict(default_agent) if test_platform["name"] == 'editmode' else dict(default_agent_gpu), # TODO if editmode then ::VM, else ::VM::GPU,
        'variables':{
            'UPM_REGISTRY': VAR_UPM_REGISTRY
        },
        'commands': [
            f'git clone git@github.cds.internal.unity3d.com:unity/utr.git {TEST_PROJECTS_DIR}/SRP_SmokeTest/utr',
            f'pip install unity-downloader-cli --extra-index-url https://artifactory.internal.unity3d.com/api/pypi/common-python/simple --upgrade',
            f'cd {TEST_PROJECTS_DIR}/SRP_SmokeTest && unity-downloader-cli --source-file ../../unity_revision.txt -c editor --wait --published-only'
        ],
        'dependencies': [
            {
                'path':f'{editor_filepath()}#{editor_job_id(editor["version"], "windows")}',
                'rerun': 'on-new-revision'
            }
        ],
        'artifacts' : {
            'logs':{
                'paths':[
                    dss('**/test-results/**')
                ]
            }
        },
    }


    if test_platform['name'].lower() == 'standalone':
        job['commands'].append(f'cd {TEST_PROJECTS_DIR}/SRP_SmokeTest && utr/utr {test_platform["args"]}Windows64 --testproject=. --editor-location=.Editor --artifacts_path=test-results --timeout=1200')
    else:
        job['commands'].append(f'cd {TEST_PROJECTS_DIR}/SRP_SmokeTest && utr/utr {test_platform["args"]} --testproject=. --editor-location=.Editor --artifacts_path=test-results')
    
    job = add_custom_revision_var(job, editor["version"])
    return job


class ABV_SmokeTestJob():
    
    def __init__(self, editor, test_platform):
        self.job_id = abv_job_id_smoke_test(editor["version"], test_platform["name"])
        self.yml = get_job_definition(editor, test_platform)