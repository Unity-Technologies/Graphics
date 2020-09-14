from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ruamel.yaml.scalarstring import PreservedScalarString as pss
from ..shared.namer import *
from ..shared.constants import VAR_UPM_REGISTRY, PATH_UNITY_REVISION
from ..shared.yml_job import YMLJob

class Editor_PinningMergeAllJob():
    
    def __init__(self, editors, agent, target_branch, target_branch_editor_ci):
        self.job_id = editor_job_id_merge_all()
        self.yml_job = self.get_job_definition(editors, agent, target_branch, target_branch_editor_ci)
        self.yml = self.yml_job.get_yml()


    def get_job_definition(self, editors, agent, target_branch, target_branch_editor_ci):
    

        dependencies = []
        for editor in editors:
            if str(editor['track']).lower()=='custom-revision':
                continue
            dependencies.append(f'{editor_pinning_filepath()}#{editor_job_id_merge_revisions_ABV(editor["track"])}')
        
        commands = [
            f'sudo pip3 install pipenv --index-url https://artifactory.prd.it.unity3d.com/artifactory/api/pypi/pypi/simple',# Remove when the image has this preinstalled.
            f'python3 -m pipenv install --dev', 
            f'curl -L https://artifactory.prd.it.unity3d.com/artifactory/api/gpg/key/public | sudo apt-key add -',
            f'sudo sh -c "echo \'deb https://artifactory.prd.it.unity3d.com/artifactory/unity-apt-local bionic main\' > /etc/apt/sources.list.d/unity.list"',
            f'sudo apt-get update',
            pss(f'''
            if [[ "$GIT_BRANCH" != "{target_branch_editor_ci }" ]]; then
                echo "Should run on '{ target_branch_editor_ci }' but is running on '$GIT_BRANCH'"
                exit 1
            fi'''),# This should never run on anything other than stable. If you try it then it will fail
            f'git config --global user.name "noreply@unity3d.com"', # TODO
            f'git config --global user.email "noreply@unity3d.com"', # TODO
            f'git checkout {target_branch}',
            f'git pull'
            f'pipenv run python3 .yamato/ruamel/build.py',
            f'git add .yamato/*.yml',
            f'git commit -m "[CI] Updated .ymls to new revision"',
            f'git push'
        ]

        # construct job
        job = YMLJob()
        job.set_name(f'Merge all [ABV] [CI]')
        job.set_agent(agent)
        job.add_var_custom('CI', True)
        job.allow_failure()
        job.add_dependencies(dependencies)
        job.add_commands(commands)
        job.set_trigger_on_expression(f'push.branch eq "{target_branch_editor_ci}" AND push.changes.any match "**/_latest_editor_versions*.metafile"')
        return job