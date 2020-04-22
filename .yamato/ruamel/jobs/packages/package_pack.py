from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import package_job_id_pack
from ..shared.yml_job import YMLJob
from ..shared.constants import NPM_UPMCI_INSTALL_URL

class Package_PackJob():
    
    def __init__(self, package, agent):
        self.package_id = package["id"]
        self.job_id = package_job_id_pack(package["id"])
        self.yml = self.get_job_definition(package, agent).get_yml()


    def get_job_definition(self, package, agent):

        # construct job
        job = YMLJob()
        job.set_name(f'Pack {package["name"]}')
        job.set_agent(agent)
        job.add_commands( [
                f'npm install upm-ci-utils@stable -g --registry {NPM_UPMCI_INSTALL_URL}',
                f'upm-ci package pack --package-path {package["packagename"]}'])
        job.add_artifacts_packages()
        return job
    
    