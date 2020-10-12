from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import project_job_id_test
from ..shared.constants import get_editor_revision
from .commands._cmd_mapper import get_cmd
from ._project_base import _job

class Project_NotStandaloneJob():
    
    def __init__(self, project, editor, platform, api, test_platform, build_config, color_space):
        self.project_name = project["name"]
        self.job_id = project_job_id_test(project["name"],platform["name"],api["name"],test_platform["name"],editor["name"], build_config["name"], color_space)
        self.yml = self.get_job_definition(project, editor, platform, api, test_platform, build_config, color_space).get_yml()


    def get_job_definition(self, project, editor, platform, api, test_platform, build_config, color_space):
        if 'URPUpdate' in project["name"]:
            cmd = get_cmd(platform["name"], api, test_platform['type'], 'internal')
            job = _job(project, test_platform["name"], editor, platform, api, cmd(project["folder"], platform, api, test_platform, editor, build_config, color_space), build_config, color_space)
            return job
        else:
            cmd = get_cmd(platform["name"], api, test_platform['type'], "")
            job = _job(project, test_platform["name"], editor, platform, api, cmd(project["folder"], platform, api, test_platform, editor, build_config, color_space), build_config, color_space)
            return job
    
    