from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import project_job_id_build
from .commands._cmd_mapper import get_cmd
from ._project_base import _job

class Project_StandaloneBuildJob():
    
    def __init__(self, project, editor, platform, api, test_platform):
        self.project_name = project["name"]
        self.job_id = project_job_id_build(project["name"],platform["name"],api["name"],editor["version"])
        self.yml = self.get_job_definition(project, editor, platform, api, test_platform).get_yml()

    
    def get_job_definition(self, project, editor, platform, api, test_platform):

        cmd = get_cmd(platform["name"], api["name"], 'standalone_build')
        job = _job(project["name"], 'standalone_build', editor, platform, api, cmd(project, platform, api, test_platform["args"]))
        
        job.add_artifacts_players()
        return job
    