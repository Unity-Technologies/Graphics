from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import *
from ..shared.yml_job import YMLJob

class PreviewPublish_WaitForNightlyJob():
    
    def __init__(self, packages, platforms,target_editor):
        self.job_id = pb_job_id_wait_for_nightly()
        self.yml = self.get_job_definition(packages, platforms, target_editor).get_yml()


    def get_job_definition(self, packages, platforms, target_editor):

        dependencies = [f'{abv_filepath()}#{abv_job_id_all_project_ci_nightly(target_editor)}']

        for package in packages:
            dependencies.append(f'{packages_filepath()}#{package_job_id_pack(package["name"])}')

            
            for platform in platforms:
                dependencies.append(f'{packages_filepath()}#{package_job_id_test(package["name"],  platform["os"], target_editor)}')

        # construct job
        job = YMLJob()
        job.set_name(f'Wait for nightly')
        job.add_dependencies(dependencies)
        return job
    
    