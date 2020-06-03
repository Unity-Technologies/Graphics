from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import *
from ..shared.yml_job import YMLJob

class PreviewPublish_WaitForNightlyJob():
    
    def __init__(self, packages, editors, platforms):
        self.job_id = pb_job_id_wait_for_nightly()
        self.yml = self.get_job_definition(packages, editors, platforms).get_yml()


    def get_job_definition(self, packages, editors, platforms):

        dependencies = [f'{abv_filepath()}#{abv_job_id_all_project_ci_nightly("trunk")}']

        for package in packages:
            dependencies.append(f'{packages_filepath()}#{package_job_id_pack(package["name"])}')

            for editor in editors:
                for platform in platforms:
                    dependencies.append(f'{packages_filepath()}#{package_job_id_test(package["name"],  platform["os"], editor["version"])}')

        # construct job
        job = YMLJob()
        job.set_name(f'Wait for nightly')
        job.add_dependencies(dependencies)
        return job
    
    