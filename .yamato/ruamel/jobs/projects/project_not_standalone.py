from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..utils.namer import project_job_id_test
from .commands._cmd_mapper import get_cmd
from ._project_base import _job

def get_job_definition(project, editor, platform, api, test_platform):

    cmd = get_cmd(platform["name"], api["name"], 'not_standalone')
    job = _job(project["name"], test_platform["name"], editor, platform, api, cmd(project, platform, api, test_platform["args"]))
    return job


class Project_NotStandaloneJob():
    
    def __init__(self, project, editor, platform, api, test_platform):
        self.project_name = project["name"]
        self.job_id = project_job_id_test(project["name"],platform["name"],api["name"],test_platform["name"],editor["version"])
        self.yml = get_job_definition(project, editor, platform, api, test_platform)

    
    
    