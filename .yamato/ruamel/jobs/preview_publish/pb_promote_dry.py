from ..shared.namer import *
from .pb_promote import PreviewPublish_PromoteJob


class PreviewPublish_PromoteDryJob():
    
    def __init__(self, agent, package, platforms, target_editor):
        self.job_id = pb_job_id_promote_dry(package["name"])
        self.yml = self.get_job_definition(agent, package, platforms, target_editor)


    def get_job_definition(self, agent, package, platforms, target_editor):
        job = PreviewPublish_PromoteJob(agent, package, platforms, target_editor, dry_run=True)
        job.yml['commands'][-1] += ' --dry-run'
        job.yml['name'] += ' [dry run]'

        return job.yml
    
    