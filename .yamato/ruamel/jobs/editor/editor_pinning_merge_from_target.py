from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ruamel.yaml.scalarstring import PreservedScalarString as pss
from ..shared.namer import editor_job_id, abv_filepath, abv_job_id_all_project_ci, editor_job_id_merge_from_target
from ..shared.constants import VAR_UPM_REGISTRY, PATH_UNITY_REVISION
from ..shared.yml_job import YMLJob

class Editor_PinningMergeFromTargetJob():
    
    def __init__(self, agent, target_branch, target_branch_editor_ci):
        self.job_id = editor_job_id_merge_from_target()
        self.yml = self.get_job_definition(agent, target_branch, target_branch_editor_ci).get_yml()


    def get_job_definition(self, agent, target_branch, target_branch_editor_ci):
    

        commands = [
            # This should never run on anything other than master or releases. If you try it then it will fail
            f'echo $GIT_BRANCH',
            pss(f'''
            if [[ "$GIT_BRANCH" != "{target_branch }" ]]; then
                echo "Should run on '{target_branch}' but is running on '$GIT_BRANCH'"
                exit 1
            fi'''),
            f'git fetch',
            f'git checkout {target_branch}',
            f'git checkout {target_branch_editor_ci}',
            f'git config --global user.name "noreply@unity3d.com"',
            f'git config --global user.email "noreply@unity3d.com"',
            f'git merge {target_branch} --ff',
            f'git push'
        ]
        
        # construct job
        job = YMLJob()
        job.set_name(f'Merge {target_branch} to {target_branch_editor_ci}')
        job.set_agent(agent)
        job.add_var_custom('CI', True)
        job.add_commands(commands)
        return job