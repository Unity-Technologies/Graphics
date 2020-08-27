from ..shared.namer import *
from ..shared.constants import TEST_PROJECTS_DIR, PATH_UNITY_REVISION, PATH_PLAYERS, PATH_TEST_RESULTS, UTR_INSTALL_URL, UNITY_DOWNLOADER_CLI_URL
from ..shared.yml_job import YMLJob
from .abv_smoke_test_standalone_build import ABV_SmokeTestStandaloneBuildJob

class ABV_SmokeTestStandaloneJob():
    def __init__(self, editor, test_platform, smoke_test):
        self.build_job = self.get_StandaloneBuildJob(editor, test_platform, smoke_test)

        # self.project_name = project["name"]
        self.job_id = abv_job_id_smoke_test(editor["version"], test_platform["name"])
        self.yml = self.get_job_definition(editor, test_platform, smoke_test, self.build_job).get_yml()

    def get_StandaloneBuildJob(self, editor, test_platform, smoke_test):
        try:
            return ABV_SmokeTestStandaloneBuildJob(editor, test_platform, smoke_test)
        except Exception as e:
            print(e)
            return None


    def get_job_definition(self, editor, test_platform, smoke_test, build_job):
        agent_gpu = dict(smoke_test["agent_gpu"])

        commands = [
            f'curl -s {UTR_INSTALL_URL}.bat --output {TEST_PROJECTS_DIR}/{smoke_test["folder"]}/utr.bat',
            f'cd {TEST_PROJECTS_DIR}/{smoke_test["folder"]} && utr {test_platform["args"]}Windows64 --artifacts_path={PATH_TEST_RESULTS} --timeout=1200 --player-load-path=../../{PATH_PLAYERS} --player-connection-ip=auto'
        ]

        job = YMLJob()
        if build_job is not None:
            job.add_dependencies([{
                    'path' : f'{abv_filepath()}#{build_job.job_id}',
                    'rerun' : f'{editor["rerun_strategy"]}'
                }])
        job.set_name(f'SRP Smoke Test - {test_platform["name"]}_{editor["version"]}')
        job.set_agent(agent_gpu)
        job.add_var_upm_registry()
        job.add_var_custom_revision(editor["version"])
        job.add_commands(commands)
        job.add_artifacts_test_results()
        return job