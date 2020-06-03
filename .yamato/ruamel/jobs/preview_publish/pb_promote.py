from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import *
from ..shared.yml_job import YMLJob
from ..shared.constants import NPM_UPMCI_INSTALL_URL

class PreviewPublish_PromoteJob():
    
    def __init__(self, agent, package):
        self.job_id = pb_job_id_promote(package["name"])
        self.yml = self.get_job_definition(agent, package).get_yml()


    def get_job_definition(self, agent, package):

        if package["publish_source"] != True:
            raise Exception('Tried to promote package for which "publish_source" set to false.')
        
        # construct job
        job = YMLJob()
        job.set_name(f'[{package["name"]}] Production Promote')
        job.set_agent(agent)
        job.add_var_custom('UPMCI_PROMOTION', 1)
        job.add_dependencies([f'{packages_filepath()}#{package_job_id_pack(package["name"])}'])
        job.add_commands([
                f'npm install upm-ci-utils@stable -g --registry {NPM_UPMCI_INSTALL_URL}',
                f'upm-ci {package["type"]} promote --{package["type"]}-path {package["path"]}'])
        job.add_artifacts_packages()
        return job
    
    