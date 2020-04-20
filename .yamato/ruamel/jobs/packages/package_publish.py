from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..utils.namer import package_job_id_publish, packages_filepath, package_job_id_pack, package_job_id_test
from ..utils.yml_job import YMLJob

def get_job_definition(package, agent, platforms):
    
    # define dependencies
    dependencies = [f'{packages_filepath()}#{package_job_id_pack(package["id"])}']
    dependencies.extend([f'{packages_filepath()}#{package_job_id_test(package["id"],  platform["name"],"trunk")}' for platform in platforms])
    
    # construct job
    job = YMLJob()
    job.set_name(f'Publish { package["name"]}')
    job.set_agent(agent)
    job.add_dependencies(dependencies)
    job.set_commands([
            f'npm install upm-ci-utils@stable -g --registry https://api.bintray.com/npm/unity/unity-npm',
            f'upm-ci package publish --package-path {package["packagename"]}'])
    job.add_artifacts_packages()
    return job


class Package_PublishJob():
    
    def __init__(self, package, agent, platforms):
        self.package_id = package["id"]
        self.job_id = package_job_id_publish(package["id"])
        self.yml = get_job_definition(package, agent, platforms).yml

    
    
    