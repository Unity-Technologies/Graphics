from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import *
from ..shared.yml_job import YMLJob
from ..shared.constants import NPM_UPMCI_INSTALL_URL
from .pb_promote_project import PreviewPublish_ProjectContext_PromoteJob

class PreviewPublish_ProjectContext_PromoteJob_DryRun():
    
    def __init__(self, agent, package, platforms, target_editor):
        self.job_id = pb_projectcontext_job_id_promote_dry(package["name"])
        self.yml = self.get_job_definition(agent, package, platforms, target_editor)


    def get_job_definition(self, agent, package, platforms, target_editor):
        job = PreviewPublish_ProjectContext_PromoteJob(agent, package, platforms, target_editor, dry_run=True)
        job.yml['commands'][-1] += ' --dry-run'
        job.yml['name'] += ' [dry run]'

        return job.yml