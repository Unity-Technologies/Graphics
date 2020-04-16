from ruamel import yaml
from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ruamel.yaml.scalarstring import PlainScalarString as pss
from .commands._cmd_mapper import get_cmd
from ..utils.namer import *
from ..utils.constants import VAR_UPM_REGISTRY

def _job(project_name, test_platform_name, editor, platform, api, cmd):

    if test_platform_name.lower() == 'standalone_build':
        job_name = f'Build {project_name} on {platform["name"]}_{api["name"]}_Player on version {editor["version"]}'
    else:
        job_name = f'{project_name} on {platform["name"]}_{api["name"]}_{test_platform_name} on version {editor["version"]}'

    agent = platform.get(f'agent_{test_platform_name.lower().replace(" ","_")}', platform['agent_default']) # replace(" ","_") called for playmode XR
    
    job = {
        'name' : job_name,
        'agent' : dict(agent),
        'variables':{
            'UPM_REGISTRY': VAR_UPM_REGISTRY
        },
        'dependencies' : [
            {
                'path' : f'{editor_filepath()}#{editor_job_id(editor["version"], platform["os"])}',
                'rerun' : editor["rerun_strategy"]
            }
        ],
        'commands' : cmd,
        'artifacts' : {
            'logs':{
                'paths':[
                    dss('**/test-results/**') # TODO linux paths
                ]
            }
        },
    }

    if editor['version'] == 'CUSTOM-REVISION':
        job['variables']['CUSTOM_REVISION'] = 'custom_revision_not_set'

    return job
