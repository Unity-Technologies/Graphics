from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ruamel.yaml.scalarstring import PreservedScalarString as pss
from ..shared.namer import editor_job_id, abv_filepath, abv_job_id_all_project_ci, editor_job_id_merge_revisions_ABV
from ..shared.constants import VAR_UPM_REGISTRY, PATH_UNITY_REVISION
from ..shared.yml_job import YMLJob
from .editor_pinning_merge_revisions import Editor_PinningMergeRevisionsJob

class Editor_PinningMergeRevisionsABVJob():
    
    def __init__(self, editors, agent, target_branch, target_branch_editor_ci):
        self.job_id = editor_job_id_merge_revisions_ABV()
        self.yml = self.get_job_definition(editors, agent, target_branch, target_branch_editor_ci).get_yml()


    def get_job_definition(self, editors, agent, target_branch, target_branch_editor_ci):
        
        dependencies = []
        for editor in editors:
            if str(editor["track"]).lower()=='custom-revision':
                continue
            dependencies.append(f'{abv_filepath()}#{abv_job_id_all_project_ci(editor["track"])}')

        job = Editor_PinningMergeRevisionsJob(editor, agent, target_branch, target_branch_editor_ci).yml_job
        job.add_dependencies(dependencies)
        job.set_trigger_on_expression(f'push.branch eq "{target_branch_editor_ci}" AND push.changes.any match "**/_latest_editor_versions.metafile"')
        job.set_name(f'Merge editor revisions to {target_branch} [ABV]')

        return job