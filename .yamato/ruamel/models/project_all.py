from ruamel import yaml
from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ruamel.yaml.scalarstring import PlainScalarString as pss
from .helpers.namer import file_path


def _dependencies(project_name, editor, dependencies_in_all):

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

    return dependencies

def project_all(project_name, editor, dependencies_in_all):

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
        'dependencies' : _dependencies(project_name, editor, dependencies_in_all),
    }

    if editor['version'] == 'CUSTOM-REVISION':
        job['variables'] = {'CUSTOM_REVISION': 'custom_revision_not_set'}

    return job

