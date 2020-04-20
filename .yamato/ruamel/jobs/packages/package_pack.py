from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..utils.namer import package_job_id_pack
from ..utils.yml_job import YMLJob

class Package_PackJob():
    
    def __init__(self, package, agent):
        self.package_id = package["id"]
        self.job_id = package_job_id_pack(package["id"])
        self.yml = self.get_job_definition(package, agent).yml


    def get_job_definition(self, package, agent):

        # construct job
        job = YMLJob()
        job.set_name(f'Pack {package["name"]}')
        job.set_agent(agent)
        job.add_commands( [
                f'npm install upm-ci-utils@stable -g --registry https://api.bintray.com/npm/unity/unity-npm',
                f'upm-ci package pack --package-path {package["packagename"]}'])
        job.add_artifacts_packages()
        return job
    
    