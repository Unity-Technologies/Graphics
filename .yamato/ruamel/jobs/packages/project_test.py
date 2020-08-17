from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import *
from ..shared.constants import PATH_UNITY_REVISION
from ..shared.yml_job import YMLJob
from ..shared.constants import NPM_UPMCI_INSTALL_URL

class Project_TestJob():
    
    def __init__(self, platform, editor):
        self.job_id = projectcontext_job_id_test(platform["os"],editor["track"])
        self.yml = self.get_job_definition(platform, editor).get_yml()

    
    def get_job_definition(self, platform, editor):

        # define dependencies
        dependencies = []#[f'{editor_filepath()}#{editor_job_id(editor["track"], platform["os"]) }']
        dependencies.extend([f'{projectcontext_filepath()}#{projectcontext_job_id_pack()}'])
        
        revision = editor.get('default_revision', None)
        if not revision:
            revision = editor["revisions"][f"{editor['track']}_latest_internal"]["windows"]["revision"]
        # define commands
        commands = [
                f'npm install upm-ci-utils@stable -g --registry {NPM_UPMCI_INSTALL_URL}',
                f'pip install unity-downloader-cli --index-url https://artifactory.prd.it.unity3d.com/artifactory/api/pypi/pypi/simple --upgrade',
                f'unity-downloader-cli -u {revision} -c editor --wait --published-only']
        commands.append(f'upm-ci project test -u {platform["editorpath"]} --project-path TestProjects/SRP_SmokeTest --type vetting-tests')


        # construct job
        job = YMLJob()
        job.set_name(f'Test all packages [project context] {platform["name"]} {editor["track"]}')
        job.set_agent(platform['agent_package'])
        job.add_dependencies(dependencies)
        job.add_commands(commands)
        job.add_artifacts_test_results()
        return job


    
    
    