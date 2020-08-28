from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import projectcontext_job_id_pack
from ..shared.yml_job import YMLJob
from ..shared.constants import NPM_UPMCI_INSTALL_URL

class Project_PackJob():
    
    def __init__(self, agent):
        self.job_id = projectcontext_job_id_pack()
        self.yml = self.get_job_definition(agent).get_yml()


    def get_job_definition(self, agent):
        # construct job
        job = YMLJob()
        job.set_name(f'Pack all [project context]')
        job.set_agent(agent)
        job.add_commands( [
                f'npm install upm-ci-utils@stable -g --registry {NPM_UPMCI_INSTALL_URL}',
                f'upm-ci project pack --project-path TestProjects/SRP_SmokeTest'])
        job.add_artifacts_packages()
        return job
    
    