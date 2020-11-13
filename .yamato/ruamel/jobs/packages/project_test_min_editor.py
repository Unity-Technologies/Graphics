from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import *
from ..shared.constants import PATH_UNITY_REVISION, NPM_UPMCI_INSTALL_URL, get_unity_downloader_cli_cmd
from ..shared.yml_job import YMLJob

class Project_TestMinEditorJob():
    
    def __init__(self, platform):
        self.job_id = projectcontext_job_id_test_min_editor(platform["os"])
        self.yml = self.get_job_definition(platform).get_yml()

    
    def get_job_definition(self, platform):

        # define dependencies
        dependencies = [f'{projectcontext_filepath()}#{projectcontext_job_id_pack()}']
        dependencies.extend([f'{editor_priming_filepath()}#{editor_job_id_test_min_editor(platform["os"]) }'])
                
        # define commands
        commands = [
                f'npm install upm-ci-utils@stable -g --registry {NPM_UPMCI_INSTALL_URL}',
                f'pip install unity-downloader-cli --index-url https://artifactory.prd.it.unity3d.com/artifactory/api/pypi/pypi/simple --upgrade',
                f'unity-downloader-cli --source-file {PATH_UNITY_REVISION} -c editor --wait --published-only']
        commands.append(f'upm-ci project test -u {platform["editorpath"]} --project-path TestProjects/SRP_SmokeTest --type vetting-tests')

        # construct job
        job = YMLJob()
        job.set_name(f'Test minimum editor version - all packages [project context] {platform["name"]}')
        job.set_agent(platform['agent_package'])
        job.add_dependencies(dependencies)
        job.add_commands(commands)
        job.add_artifacts_test_results()
        return job


    
    
    