from ruamel import yaml
from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ruamel.yaml.scalarstring import PlainScalarString as pss
from .commands import cmd_mapper as cm
from .helpers import name_builder as nb

def _base(job_name, editor, platform, agent):
    '''Adds name, agent, artifact:logs:paths:/test-results/, dependencies:z_editor#editor-priming, variables:CUSTOM_REVISION'''

    job = {
        'name' : job_name,
        'agent' : {
            'flavor' : agent["flavor"],
            'type' : agent["type"],
            'image' : agent["image"]
        },
        'artifacts' : {
            'logs':{
                'paths':[
                    dss('**/test-results/**')
                ]
            }
        },
        'dependencies' : [
            {
                'path' : f'.yamato/z_editor.yml#editor:priming:{editor["version"]}:{platform["os"]}',
                'rerun' : f'{editor["rerun_strategy"]}'
            }
        ]
    }

    if editor['version'] == 'CUSTOM-REVISION':
        job['variables'] = {'CUSTOM_REVISION': 'custom_revision_not_set'}

    return job

def project_build(project, editor, platform, test_platform, api):
    '''Creates build player job (to be used when Standalone uses split build).'''

    agent = platform.get(f'agent_build', platform['agent_default'])
    job_name = nb.get_job_name_build(project, editor, platform, test_platform, api)
    job = _base(job_name, editor, platform, agent)
    

    cmd = cm.get_cmd(platform["name"], api["name"], 'build')
    job['commands'] = cmd(project, platform, test_platform, api)
    
    job['artifacts']['players'] = {
        'paths':[
            dss('players/**')
        ]
    }
    
    return job


def project_test(project, editor, platform, test_platform, api):
    '''Creates testing job for the specified test platform. If test_platform is Standalone and standalone_split is true, then adds dependency for build player job and adjusts commands accordingly.'''

    agent = platform.get(f'agent_{test_platform["name"]}'.lower(), platform['agent_default'])
    job_name = nb.get_job_name_test(project, editor, platform, test_platform, api)
    job = _base(job_name, editor, platform, agent)
    

    if test_platform["name"].lower() == "standalone" : # TODO check for better way to do it 
        
        if platform["standalone_split"]:
            job['skip_checkout'] = True
            job['dependencies'].append(
                {
                    'path' : f'.yamato/{nb.get_yml_name(project, platform, api)}#{nb.get_job_id_build(project, editor, platform, test_platform, api)}',
                    'rerun' : f'{editor["rerun_strategy"]}'
                }
            )
        
        cmd = cm.get_cmd(platform["name"], api["name"], 'test_standalone')       

    else:
        cmd = cm.get_cmd(platform["name"], api["name"], 'test')
    
    job['commands'] = cmd(project, platform, test_platform, api)
    
    return job
