from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..utils.namer import project_job_id_build
from .commands._cmd_mapper import get_cmd
from ._project_base import _job

def get_job_definition(project, editor, platform, api):

    cmd = get_cmd(platform["name"], api["name"], 'standalone_build')
    job = _job(project["name"], 'standalone_build', editor, platform, api, cmd(project, platform, api))
    
    job['artifacts']['players'] = {
        'paths':[
            dss('players/**')
        ]
    }
    
    return job


class Project_StandaloneBuildJob():
    
    def __init__(self, project, editor, platform, api):
        self.project_name = project["name"]
        self.job_id = project_job_id_build(project["name"],platform["name"],api["name"],editor["version"])
        self.yml = get_job_definition(project, editor, platform, api)

    
    
    