from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import projectcontext_filepath, projectcontext_job_id_pack, projectcontext_job_id_test, projectcontext_job_id_publish, projectcontext_job_id_publish_dry
from ..shared.yml_job import YMLJob
from ..shared.constants import NPM_UPMCI_INSTALL_URL
from .project_publish import Project_PublishJob

class Project_PublishJob_DryRun():
    
    def __init__(self, package, agent, platforms, editor_tracks):
        self.package_id = package["id"]
        self.job_id = projectcontext_job_id_publish_dry(package["id"])
        self.yml = self.get_job_definition(package, agent, platforms, editor_tracks)

    
    def get_job_definition(self, package, agent, platforms, editor_tracks):
        
        job = Project_PublishJob(package, agent, platforms, editor_tracks)
        job.yml['commands'][-1] += ' --dry-run'
        job.yml['name'] += ' [dry run]'

        return job.yml
    