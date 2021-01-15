from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ruamel.yaml.scalarstring import PreservedScalarString as pss
from ..shared.namer import *
from ..shared.constants import VAR_UPM_REGISTRY, PATH_UNITY_REVISION
from ..shared.yml_job import YMLJob

class Editor_PinningStoreGreenRevisionsJob():
    
    def __init__(self, editors, agent, target_branch):
        self.job_id = editor_job_id_store_green()
        self.yml_job = self.get_job_definition(editors, agent, target_branch)
        self.yml = self.yml_job.get_yml()


    def get_job_definition(self, editors, agent, target_branch):
        
        commands = [
            f'sudo pip3 install pipenv --index-url https://artifactory.prd.it.unity3d.com/artifactory/api/pypi/pypi/simple',# Remove when the image has this preinstalled.
            f'python3 -m pipenv install --dev', 
            f'git config --global user.name "noreply@unity3d.com"',
            f'git config --global user.email "noreply@unity3d.com"', 
        ]
        for editor in editors:
            if not editor['editor_pinning']:
                continue
            commands.append(f'pipenv run python3 .yamato/ruamel/editor_pinning/store_green_revisions.py --target-branch $GIT_BRANCH --track {editor["track"]} --apikey $YAMATO_KEY')


        # construct job
        job = YMLJob()
        job.set_name(f'Store green job revisions')
        
        job.set_agent(agent)
        job.add_commands(commands)
        job.add_trigger_recurrent(target_branch, '7 * * ?')
        return job