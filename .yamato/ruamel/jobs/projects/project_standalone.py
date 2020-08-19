from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import project_filepath_specific, project_job_id_build, project_job_id_test
from ..shared.constants import get_editor_revision
from .commands._cmd_mapper import get_cmd
from ._project_base import _job
from .project_standalone_build import Project_StandaloneBuildJob

class Project_StandaloneJob():
    
    def __init__(self, project, editor, platform, api, test_platform):
        self.build_job = self.get_StandaloneBuildJob(project, editor, platform, api, test_platform)

        self.project_name = project["name"]
        self.job_id = project_job_id_test(project["name"],platform["name"],api["name"],test_platform["name"],editor["track"])
        self.yml = self.get_job_definition(project, editor, platform, api, test_platform, self.build_job).get_yml()

    
    def get_StandaloneBuildJob(self, project, editor, platform, api, test_platform):
        try:
            return Project_StandaloneBuildJob(project, editor, platform, api, test_platform)
        except:
            return None
    
    
    def get_job_definition(self, project, editor, platform, api, test_platform, build_job):

        project_folder = project.get("folder_standalone", project["folder"])
        cmd = get_cmd(platform["name"], api, 'standalone', "") 
        job = _job(project["name"], test_platform["name"], editor, platform, api, cmd(project_folder, platform, api, test_platform["args"], editor))

        if build_job is not None:

            job.add_dependencies([{
                    'path' : f'{project_filepath_specific(project["name"], platform["name"], api["name"])}#{build_job.job_id}',
                    'rerun' : f'{editor["rerun_strategy"]}'
                }])
            
            if not (project["name"].lower() == 'universal' and platform["name"].lower() == 'win' and test_platform["name"].lower() == 'standalone') :
                job.set_skip_checkout(True)
            
        return job
