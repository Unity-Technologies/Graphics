from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ruamel.yaml.scalarstring import PreservedScalarString as pss

from ..shared.namer import editor_job_id, editor_job_id_update, editor_pinning_filepath, editor_job_id_merge_from_target
from ..shared.constants import VAR_UPM_REGISTRY, PATH_UNITY_REVISION
from ..shared.yml_job import YMLJob

class Editor_PinningUpdateJob():
    
    def __init__(self, agent, target_branch, target_branch_editor_ci):
        self.job_id = editor_job_id_update()
        self.yml = self.get_job_definition(agent, target_branch, target_branch_editor_ci).get_yml()


    def get_job_definition(self, agent, target_branch, target_branch_editor_ci):

        commands = [
            f'sudo pip3 install pipenv --index-url https://artifactory.prd.it.unity3d.com/artifactory/api/pypi/pypi/simple',# Remove when the image has this preinstalled.
            f'python3 -m pipenv install --dev', 
            f'curl -L https://artifactory.prd.it.unity3d.com/artifactory/api/gpg/key/public | sudo apt-key add -',
            f'sudo sh -c "echo \'deb https://artifactory.prd.it.unity3d.com/artifactory/unity-apt-local bionic main\' > /etc/apt/sources.list.d/unity.list"',
            f'sudo apt-get update',
            f'sudo apt-get install yamato-parser -y',
            pss(f'''
            if [[ "$GIT_BRANCH" != "{target_branch }" ]]; then
                echo "Should run on '{ target_branch }' but is running on '$GIT_BRANCH'"
                exit 1
            fi'''),# This should never run on anything other than stable. If you try it then it will fail
            f'git config --global user.name "noreply@unity3d.com"', # TODO
            f'git config --global user.email "noreply@unity3d.com"', # TODO
            f'pipenv run python3 .yamato/ruamel/editor_pinning/update_revisions.py --target-branch { target_branch_editor_ci } --force-push'
        ]
        
        # construct job
        job = YMLJob()
        job.set_name(f'Update pinned editor versions')
        job.set_agent(agent)
        job.add_var_custom('CI', True)
        job.add_commands(commands)
        job.add_dependencies([f'{editor_pinning_filepath()}#{editor_job_id_merge_from_target()}']) #TODO toggle
        job.add_trigger_recurrent(target_branch, '0 * * ?') # TODO uncomment
        return job