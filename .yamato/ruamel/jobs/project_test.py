from ruamel import yaml
from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ruamel.yaml.scalarstring import PlainScalarString as pss
from .commands import cmd_mapper as cm
from .helpers.namer import file_path

def _job(project_name, test_platform_name, editor, platform, api, cmd):

    if test_platform_name.lower() == 'standalone_build':
        job_name = f'Build {project_name} on {platform["name"]}_{api["name"]}_Player on version {editor["version"]}'
    else:
        job_name = f'{project_name} on {platform["name"]}_{api["name"]}_{test_platform_name} on version {editor["version"]}'

    agent = platform.get(f'agent_{test_platform_name.lower().replace(" ","_")}', platform['agent_default'])
    
    job = {
        'name' : job_name,
        'agent' : dict(agent),
        'variables':{
            'UPM_REGISTRY': 'https://artifactory-slo.bf.unity3d.com/artifactory/api/npm/upm-candidates'
        },
        'dependencies' : [
            {
                'path' : f'.yamato/z_editor.yml#editor:priming:{editor["version"]}:{platform["os"]}',
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


def project_not_standalone(project, editor, platform, api, test_platform):
    '''Creates test job'''
    
    cmd = cm.get_cmd(platform["name"], api["name"], 'not_standalone')
    job = _job(project["name"], test_platform["name"], editor, platform, api, cmd(project, platform, api, test_platform["args"]))
    return job


def project_standalone(project, editor, platform, api, test_platform):
    '''Creates Standalone test job'''

    cmd = cm.get_cmd(platform["name"], api["name"], 'standalone') 
    job = _job(project["name"], test_platform["name"], editor, platform, api, cmd(project, platform, api, test_platform["args"]))

    if platform["standalone_split"]:
        
        yml_file= file_path(project["name"], platform["name"], api["name"])
        job_id_build = f'Build_{project["name"]}_{platform["name"]}_{api["name"]}_Player_{editor["version"]}'
        
        job['skip_checkout'] = True
        job['dependencies'].append(
            {
                'path' : f'{yml_file}#{job_id_build}',
                'rerun' : f'{editor["rerun_strategy"]}'
            }
        )
        
    return job


def project_standalone_build(project, editor, platform, api):
    '''Creates build player job (to be used when Standalone uses split build).'''

    cmd = cm.get_cmd(platform["name"], api["name"], 'standalone_build')
    job = _job(project["name"], 'standalone_build', editor, platform, api, cmd(project, platform, api))
    
    job['artifacts']['players'] = {
        'paths':[
            dss('players/**')
        ]
    }
    
    return job


def project_all(project_name, editor, dependencies_in_all):

    dependencies = []
    for d in dependencies_in_all:
        for test_platform_name in d["test_platform_names"]:
            
            file_name= file_path(project_name, d["platform_name"], d["api_name"])
            job_id = f'{project_name}_{d["platform_name"]}_{d["api_name"]}_{test_platform_name}_{editor["version"]}'

            dependencies.append(
                {
                    'path' : f'{file_name}#{job_id}',
                    'rerun' : editor["rerun_strategy"]
                }
            )

    job = {
        'name' : f'All {project_name} CI - {editor["version"]}',
        'agent' : {
            'flavor' : 'b1.small',
            'type' : 'Unity::VM',
            'image' : 'cds-ops/ubuntu-18.04-agent:stable'
        },
        'commands' : [
            'dir'
        ],
        'dependencies' : dependencies
    }

    if editor['version'] == 'CUSTOM-REVISION':
        job['variables'] = {'CUSTOM_REVISION': 'custom_revision_not_set'}

    return job