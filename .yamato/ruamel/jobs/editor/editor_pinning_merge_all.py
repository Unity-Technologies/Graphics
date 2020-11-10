from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ruamel.yaml.scalarstring import PreservedScalarString as pss
from ..shared.namer import *
from ..shared.constants import VAR_UPM_REGISTRY, PATH_UNITY_REVISION
from ..shared.yml_job import YMLJob

class Editor_PinningMergeAllJob():
    
    def __init__(self, editors, agent, target_branch, target_branch_editor_ci, abv):
        self.job_id = editor_job_id_merge_all(abv)
        self.yml_job = self.get_job_definition(editors, agent, target_branch, target_branch_editor_ci, abv)
        self.yml = self.yml_job.get_yml()


    def get_job_definition(self, editors, agent, target_branch, target_branch_editor_ci, abv):
    
        
        dependencies = []
        for editor in editors:
            if not editor['editor_pinning']:
                continue

            dependencies.append(f'{editor_pinning_filepath()}#{editor_job_id_merge_revisions(editor["name"], abv)}')
        
        

        # construct job
        job = YMLJob()
        
        if abv:
            job.set_name(f'Merge all [ABV] [CI]')
            job.set_trigger_on_expression(f'push.branch eq "{target_branch_editor_ci}" AND push.changes.any match "**/_latest_editor_versions*.metafile"')
        else:
            job.set_name(f'Merge all [no ABV] [no CI]')
        
        #job.set_agent(agent)
        job.add_var_custom('CI', True)
        job.add_dependencies(dependencies)
        #job.add_commands(commands)
        return job