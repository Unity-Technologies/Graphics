from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ruamel.yaml.scalarstring import PreservedScalarString as pss
from ..shared.namer import *
from ..shared.constants import VAR_UPM_REGISTRY, PATH_UNITY_REVISION
from ..shared.yml_job import YMLJob

class Editor_PinningMergeAllJob():
    
    def __init__(self, editors, agent, target_branch, target_branch_editor_ci, ci):
        self.job_id = editor_job_id_merge_all(ci)
        self.yml_job = self.get_job_definition(editors, agent, target_branch, target_branch_editor_ci, ci)
        self.yml = self.yml_job.get_yml()


    def get_job_definition(self, editors, agent, target_branch, target_branch_editor_ci, ci):
    
        
        abv_markers = []
        dependencies = []
        for editor in editors:
            if not editor['editor_pinning']:
                continue
            
            if ci: # for ci workflows use the abv dependency (true/false) marked in metafile
                abv_markers.append(f'[{editor["track"]} ABV]' if editor['editor_pinning_use_abv'] else f'[{editor["track"]} no ABV]')
                dependencies.append(f'{editor_pinning_filepath()}#{editor_job_id_merge_revisions(editor["name"], editor["editor_pinning_use_abv"])}')
            else: # for manual workflow always disable ABV dependency, since the manual job is a 'force update' 
                abv_markers.append(f'[{editor["track"]} no ABV]')
                dependencies.append(f'{editor_pinning_filepath()}#{editor_job_id_merge_revisions(editor["name"], False)}')

        job_name = 'Merge all'
        job_name += ' [CI]' if ci else ' [no CI]'
        job_name += ' '.join(abv_markers)

        # construct job
        job = YMLJob()
        job.set_name(job_name)
        if ci:
            job.set_trigger_on_expression(f'push.branch eq "{target_branch_editor_ci}" AND push.changes.any match "**/_latest_editor_versions*.metafile"')
        
        
        job.add_var_custom('CI', True)
        job.add_dependencies(dependencies)
        return job