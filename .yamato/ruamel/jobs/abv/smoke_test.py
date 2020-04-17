from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..utils.namer import *
from ..utils.constants import VAR_UPM_REGISTRY, TEST_PROJECTS_DIR, PATH_TEST_RESULTS_padded, PATH_TEST_RESULTS, PATH_UNITY_REVISION
from ..utils.shared import add_custom_revision_var

def get_job_definition(editor, test_platform, smoke_test):  # only run for 2020.1 and trunk
    agent = dict(smoke_test["agent_win"])
    agent_gpu = dict(smoke_test["agent_win_gpu"])
    
    job = {
        'name': f'SRP Smoke Test - {test_platform["name"]}_{editor["version"]}',
        'agent': agent if test_platform["name"] == 'editmode' else agent_gpu,
        'variables':{
            'UPM_REGISTRY': VAR_UPM_REGISTRY
        },
        'commands': [
            f'git clone git@github.cds.internal.unity3d.com:unity/utr.git {TEST_PROJECTS_DIR}/{smoke_test["folder"]}/utr',
            f'pip install unity-downloader-cli --extra-index-url https://artifactory.internal.unity3d.com/api/pypi/common-python/simple --upgrade',
            f'cd {TEST_PROJECTS_DIR}/{smoke_test["folder"]} && unity-downloader-cli --source-file ../../{PATH_UNITY_REVISION} -c editor --wait --published-only'
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
                    dss(PATH_TEST_RESULTS_padded)
                ]
            }
        },
    }


    if test_platform['name'].lower() == 'standalone':
        job['commands'].append(f'cd {TEST_PROJECTS_DIR}/{smoke_test["folder"]} && utr/utr {test_platform["args"]}Windows64 --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS} --timeout=1200')
    else:
        job['commands'].append(f'cd {TEST_PROJECTS_DIR}/{smoke_test["folder"]} && utr/utr {test_platform["args"]} --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS}')
    
    job = add_custom_revision_var(job, editor["version"])
    return job


class ABV_SmokeTestJob():
    
    def __init__(self, editor, test_platform, smoke_test):
        self.job_id = abv_job_id_smoke_test(editor["version"], test_platform["name"])
        self.yml = get_job_definition(editor, test_platform, smoke_test)