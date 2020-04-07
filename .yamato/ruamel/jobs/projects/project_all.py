from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..utils.namer import project_filepath_specific, project_job_id_all


def get_job_definition(project_name, editor, dependencies_in_all):
    dependencies = []
    for d in dependencies_in_all:
        for test_platform_name in d["test_platform_names"]:
            
            file_name= project_filepath_specific(project_name, d["platform_name"], d["api_name"])
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
            'type' : 'Unity::VM', #TODO not to be hardcoded
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


class Project_AllJob():
    
    def __init__(self, project_name, editor, dependencies_in_all):
        self.project_name = project_name
        self.job_id = project_job_id_all(project_name, editor["version"])
        self.yml = get_job_definition(project_name, editor, dependencies_in_all)

    
    
    