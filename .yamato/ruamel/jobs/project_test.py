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

    agent = platform.get(f'agent_{test_platform_name.lower()}', platform['agent_default'])
    
    job = {
        'name' : job_name,
        'agent' : {
            'flavor' : agent["flavor"],
            'type' : agent["type"],
            'image' : agent["image"]
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
        job['variables'] = {'CUSTOM_REVISION': 'custom_revision_not_set'}

    return job

def project_editmode(project, editor, platform, api):
    '''Creates editmode test job'''
    
    cmd = cm.get_cmd(platform["name"], api["name"], 'editmode')
    job = _job(project["name"], 'editmode', editor, platform, api, cmd(project, platform, api))
    return job


def project_playmode(project, editor, platform, api):
    '''Creates playmode test job'''
    
    cmd = cm.get_cmd(platform["name"], api["name"], 'playmode')
    job = _job(project["name"], 'playmode', editor, platform, api, cmd(project, platform, api))
    return job

def project_playmode_xr(project, editor, platform, api):
    '''Creates playmode test job'''
    
    cmd = cm.get_cmd(platform["name"], api["name"], 'playmode_xr')
    job = _job(project["name"], 'playmode_XR', editor, platform, api, cmd(project, platform, api))
    return job

def project_standalone(project, editor, platform, api):
    '''Creates Standalone test job'''

    cmd = cm.get_cmd(platform["name"], api["name"], 'standalone') 
    job = _job(project["name"], 'Standalone', editor, platform, api, cmd(project, platform, api))

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
