from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ruamel.yaml.scalarstring import PreservedScalarString as pss

from ..shared.namer import editor_job_id, editor_job_id_update_ABV, editor_pinning_filepath
from ..shared.constants import VAR_UPM_REGISTRY, PATH_UNITY_REVISION
from ..shared.yml_job import YMLJob
from .editor_pinning_update import Editor_PinningUpdateJob

class Editor_PinningUpdateABVJob():
    
    def __init__(self, agent, target_branch, target_branch_editor_ci):
        self.job_id = editor_job_id_update_ABV()
        self.yml = self.get_job_definition(agent, target_branch, target_branch_editor_ci).get_yml()


    def get_job_definition(self, agent, target_branch, target_branch_editor_ci):

        job = Editor_PinningUpdateJob(agent, target_branch, target_branch_editor_ci).yml_job
        job.set_name(f'Update pinned editor versions [ABV]')
        job.add_trigger_recurrent(target_branch, '0 * * ?') 
        return job