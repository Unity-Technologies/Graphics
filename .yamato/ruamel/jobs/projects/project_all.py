from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..utils.namer import *
from ..utils.shared import add_custom_revision_var

def get_job_definition(project_name, editor, dependencies_in_all):
    dependencies = []
    for d in dependencies_in_all:
        for test_platform_name in d["test_platform_names"]:
            
            file_name = project_filepath_specific(project_name, d["platform_name"], d["api_name"])
            job_id = project_job_id_test(project_name,d["platform_name"],d["api_name"],test_platform_name,editor["version"])

            dependencies.append(
                {
                    'path' : f'{file_name}#{job_id}',
                    'rerun' : editor["rerun_strategy"]
                }
            )

    job = {
        'name' : f'All {project_name} CI - {editor["version"]}',
        'dependencies' : dependencies
    }

    job = add_custom_revision_var(job, editor["version"])
    return job


class Project_AllJob():
    
    def __init__(self, project_name, editor, dependencies_in_all):
        self.project_name = project_name
        self.job_id = project_job_id_all(project_name, editor["version"])
        self.yml = get_job_definition(project_name, editor, dependencies_in_all)

    
    
    