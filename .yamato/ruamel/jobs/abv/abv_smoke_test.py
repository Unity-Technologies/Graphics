from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import *
from ..shared.constants import TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_TEST_RESULTS, UTR_INSTALL_URL, UNITY_DOWNLOADER_CLI_URL
from ..shared.yml_job import YMLJob

class ABV_SmokeTestJob():
    
    def __init__(self, editor, test_platform, smoke_test):
        self.job_id = abv_job_id_smoke_test(editor["version"], test_platform["name"])
        self.yml = self.get_job_definition(editor, test_platform, smoke_test).get_yml()


    def get_job_definition(self, editor, test_platform, smoke_test): 
        agent = dict(smoke_test["agent"])
        agent_gpu = dict(smoke_test["agent_gpu"])
        
        # define dependencies
        dependencies = [{
                    'path':f'{editor_filepath()}#{editor_job_id(editor["version"], "windows")}',
                    'rerun': editor["rerun_strategy"]}]


        # define commands
        commands = [
                f'curl -s {UTR_INSTALL_URL}.bat --output {TEST_PROJECTS_DIR}/{smoke_test["folder"]}/utr.bat',
                f'pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
                f'cd {TEST_PROJECTS_DIR}/{smoke_test["folder"]} && unity-downloader-cli --source-file ../../{PATH_UNITY_REVISION} -c editor --wait --published-only' ]
        if test_platform['name'].lower() == 'standalone':
            commands.append(f'cd {TEST_PROJECTS_DIR}/{smoke_test["folder"]} && utr {test_platform["args"]}Windows64 --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS} --timeout=1200')
        else:
            commands.append(f'cd {TEST_PROJECTS_DIR}/{smoke_test["folder"]} && utr {test_platform["args"]} --testproject=. --editor-location=.Editor --artifacts_path={PATH_TEST_RESULTS}')
        
        # construct job
        job = YMLJob()
        job.set_name(f'SRP Smoke Test - {test_platform["name"]}_{editor["version"]}')
        job.set_agent(agent if test_platform["name"] == 'editmode' else agent_gpu)
        job.add_var_upm_registry()
        job.add_var_custom_revision(editor["version"])
        job.add_commands(commands)
        job.add_dependencies(dependencies)
        job.add_artifacts_test_results()
        return job