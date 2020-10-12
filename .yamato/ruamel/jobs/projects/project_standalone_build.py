from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import project_job_id_build
from ..shared.constants import get_editor_revision
from .commands._cmd_mapper import get_cmd
from ._project_base import _job

class Project_StandaloneBuildJob():
    
    def __init__(self, project, editor, platform, api, test_platform, build_config, color_space):
        self.project_name = project["name"]
        self.job_id = project_job_id_build(project["name"],platform["name"],api["name"],editor["name"], build_config["name"], color_space)
        self.yml = self.get_job_definition(project, editor, platform, api, test_platform, build_config, color_space).get_yml()

    
    def get_job_definition(self, project, editor, platform, api, test_platform, build_config, color_space):

        project_folder = project.get("folder_standalone", project["folder"])
        cmd = get_cmd(platform["name"], api, 'standalone_build', "")
        job = _job(project, 'standalone_build', editor, platform, api, cmd(project_folder, platform, api, test_platform, editor, build_config, color_space), build_config, color_space)
        
        job.add_artifacts_players()
        return job