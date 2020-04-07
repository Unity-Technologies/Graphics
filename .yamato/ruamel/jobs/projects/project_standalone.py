from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..utils.namer import project_filepath_specific, project_job_id_build, project_job_id_test
from .commands._cmd_mapper import get_cmd
from ._project_base import _job

def get_job_definition(project, editor, platform, api, test_platform):

    cmd = get_cmd(platform["name"], api["name"], 'standalone') 
    job = _job(project["name"], test_platform["name"], editor, platform, api, cmd(project, platform, api, test_platform["args"]))

    if platform["standalone_split"]:
        
        yml_file= project_filepath_specific(project["name"], platform["name"], api["name"])
        job_id_build = project_job_id_build(project["name"],platform["name"], api["name"], editor["version"]) 
        
        job['skip_checkout'] = True
        job['dependencies'].append(
            {
                'path' : f'{yml_file}#{job_id_build}',
                'rerun' : f'{editor["rerun_strategy"]}'
            }
        )
        
    return job


class Project_StandaloneJob():
    
    def __init__(self, project, editor, platform, api, test_platform):
        self.project_name = project["name"]
        self.job_id = project_job_id_test(project["name"],platform["name"],api["name"],test_platform["name"],editor["version"])
        self.yml = get_job_definition(project, editor, platform, api, test_platform)

    
    
    