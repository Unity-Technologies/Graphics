from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import projectcontext_filepath, projectcontext_job_id_pack, projectcontext_job_id_test, projectcontext_job_id_publish
from ..shared.yml_job import YMLJob
from ..shared.constants import NPM_UPMCI_INSTALL_URL

class Project_PublishJob():
    
    def __init__(self, package, agent, platforms, target_editor):
        self.package_id = package["id"]
        self.job_id = projectcontext_job_id_publish(package["id"])
        self.yml = self.get_job_definition(package, agent, platforms, target_editor).get_yml()

    
    def get_job_definition(self, package, agent, platforms, target_editor):
        
        # define dependencies
        dependencies = [f'{projectcontext_filepath()}#{projectcontext_job_id_pack()}']
        dependencies.extend([f'{projectcontext_filepath()}#{projectcontext_job_id_test(platform["os"], target_editor)}' for platform in platforms])
        
        # construct job
        job = YMLJob()
        job.set_name(f'Publish { package["name"]} [project context]')
        job.set_agent(agent)
        job.add_dependencies(dependencies)
        job.add_commands([
                f'npm install upm-ci-utils@stable -g --registry {NPM_UPMCI_INSTALL_URL}',
                f'upm-ci package publish --package-path {package["packagename"]}'])
        job.add_artifacts_packages()
        return job
    