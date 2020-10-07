from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ruamel.yaml.scalarstring import PreservedScalarString as pss
from ..shared.namer import editor_job_id, abv_filepath, abv_job_id_all_project_ci, editor_job_id_merge_revisions
from ..shared.constants import VAR_UPM_REGISTRY, PATH_UNITY_REVISION
from ..shared.yml_job import YMLJob

class Editor_PinningMergeRevisionsJob():
    
    def __init__(self, editor, agent, target_branch, target_branch_editor_ci, abv):
        self.job_id = editor_job_id_merge_revisions(editor["name"], abv)
        self.yml_job = self.get_job_definition(editor, agent, target_branch, target_branch_editor_ci, abv)
        self.yml = self.yml_job.get_yml()


    def get_job_definition(self, editor, agent, target_branch, target_branch_editor_ci, abv):
    

        commands = [
            f'sudo pip3 install pipenv --index-url https://artifactory.prd.it.unity3d.com/artifactory/api/pypi/pypi/simple',# Remove when the image has this preinstalled.
            f'python3 -m pipenv install --dev', 
            f'curl -L https://artifactory.prd.it.unity3d.com/artifactory/api/gpg/key/public | sudo apt-key add -',
            f'sudo sh -c "echo \'deb https://artifactory.prd.it.unity3d.com/artifactory/unity-apt-local bionic main\' > /etc/apt/sources.list.d/unity.list"',
            f'sudo apt-get update',
            f'sudo apt-get install yamato-parser -y',
            pss(f'''
            if [[ "$GIT_BRANCH" != "{target_branch_editor_ci }" ]]; then
                echo "Should run on '{target_branch_editor_ci}' but is running on '$GIT_BRANCH'"
                exit 1
            fi'''),# This should never run on anything other than stable. If you try it then it will fail
            f'git config --global user.name "noreply@unity3d.com"', # TODO
            f'git config --global user.email "noreply@unity3d.com"', # TODO
            f'pipenv run python3 .yamato/ruamel/editor_pinning/merge_revisions.py --revision $GIT_REVISION --target-branch { target_branch } --track {editor["track"]}'
        ]
        
        # construct job
        job = YMLJob()

        if abv: 
            job.set_name(f'Merge [{editor["track"]}] revisions to {target_branch} [ABV]')
            job.allow_failure()
            job.add_dependencies([f'{abv_filepath()}#{abv_job_id_all_project_ci(editor["name"])}'])
        else:
            job.set_name(f'Merge [{editor["track"]}] revisions to {target_branch} [no ABV]')
        
        job.set_agent(agent)
        job.add_var_custom('CI', True)
        job.add_commands(commands)
        return job