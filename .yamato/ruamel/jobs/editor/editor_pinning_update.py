from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ruamel.yaml.scalarstring import PreservedScalarString as pss

from ..shared.namer import editor_job_id
from ..shared.constants import VAR_UPM_REGISTRY, PATH_UNITY_REVISION
from ..shared.yml_job import YMLJob

class Editor_PinningUpdateJob():
    
    def __init__(self, agent, editor_pin_target_branch, editor_pin_ci_branch):
        self.job_id = 'update-editor-pinning'
        self.yml = self.get_job_definition(agent, editor_pin_target_branch, editor_pin_ci_branch).get_yml()


    def get_job_definition(self, agent, editor_pin_target_branch, editor_pin_ci_branch):

        commands = [
            f'sudo pip3 install pipenv',
            f'pipenv install --dev', 
            f'curl -L https://artifactory.prd.it.unity3d.com/artifactory/api/gpg/key/public | sudo apt-key add -',
            f'sudo sh -c "echo \'deb https://artifactory.prd.it.unity3d.com/artifactory/unity-apt-local bionic main\' > /etc/apt/sources.list.d/unity.list"',
            f'sudo apt-get update',
            f'sudo apt-get install yamato-parser -y',
            pss(f'''
            if [[ "$GIT_BRANCH" != "{editor_pin_target_branch }" ]]; then
                echo "Should run on '{ editor_pin_target_branch }' but is running on '$GIT_BRANCH'"
                exit 1
            fi'''),# This should never run on anything other than stable. If you try it then it will fail
            f'git config --global user.name "noreply@unity3d.com"', # TODO
            f'git config --global user.email "noreply@unity3d.com"', # TODO
            f'pipenv run python3 .yamato/ruamel/editor_pinning/update_revisions.py --target-branch { editor_pin_ci_branch } --force-push'
        ]
        
        # construct job
        job = YMLJob()
        job.set_name(f'Update pinned editor versions')
        job.set_agent(agent)
        job.add_var_custom('CI', True)
        job.add_commands(commands)
        # job.add_trigger_recurrent(editor_pin_target_branch, '1 * * ?') TODO uncomment
        return job