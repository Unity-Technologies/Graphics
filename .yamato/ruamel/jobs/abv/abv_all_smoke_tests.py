from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import *
from ..shared.yml_job import YMLJob

class ABV_AllSmokeTestsJob():
    
    def __init__(self, editor, test_platforms):
        self.job_id = abv_job_id_all_smoke_tests(editor["track"])
        self.yml = self.get_job_definition(editor, test_platforms).get_yml()


    def get_job_definition(self,editor, test_platforms):

        # define dependencies
        dependencies = []
        for test_platform in test_platforms:
            dependencies.append({
                'path': f'{abv_filepath()}#{abv_job_id_smoke_test(editor["track"],test_platform["name"])}',
                'rerun': editor["rerun_strategy"]
            })

        # construct job
        job = YMLJob()
        job.set_name(f'All Smoke Tests - {editor["track"]}')
        job.add_dependencies(dependencies)
        job.add_var_custom_revision(editor["track"])
        return job
