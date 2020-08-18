from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import project_job_id_test
from ..shared.constants import get_editor_revision
from .commands._cmd_mapper import get_cmd
from ._project_base import _job

class Project_NotStandaloneJob():
    
    def __init__(self, project, editor, platform, api, test_platform):
        self.project_name = project["name"]
        self.job_id = project_job_id_test(project["name"],platform["name"],api["name"],test_platform["name"],editor["track"])
        self.yml = self.get_job_definition(project, editor, platform, api, test_platform).get_yml()


    def get_job_definition(self, project, editor, platform, api, test_platform):
        if 'URPUpdate' in project["name"]:
            cmd = get_cmd(platform["name"], api, 'not_standalone', 'internal')
            job = _job(project["name"], test_platform["name"], editor, platform, api, cmd(project["folder"], platform, api, test_platform["args"], get_editor_revision(editor,platform["os"])))
            return job
        else:
            cmd = get_cmd(platform["name"], api, 'not_standalone', "")
            job = _job(project["name"], test_platform["name"], editor, platform, api, cmd(project["folder"], platform, api, test_platform["args"], get_editor_revision(editor,platform["os"])))
            return job
    
    