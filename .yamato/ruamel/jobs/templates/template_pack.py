from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import template_job_id_pack
from ..shared.yml_job import YMLJob

class Template_PackJob():
    
    def __init__(self, template, agent):
        self.job_id = template_job_id_pack(template["id"])
        self.yml = self.get_job_definition(template, agent).yml


    def get_job_definition(self, template, agent):

        # construct job
        job = YMLJob()
        job.set_name(f'Pack {template["id"]}')
        job.set_agent(agent)
        job.add_commands( [
                f'npm install upm-ci-utils@stable -g --registry {NPM_UPMCI_INSTALL_URL}',
                f'upm-ci template pack --project-path {template["packagename"]}'])
        job.add_artifacts_packages() # TODO add templates path
        return job
    
    