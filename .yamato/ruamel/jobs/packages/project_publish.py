from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import projectcontext_filepath, projectcontext_job_id_pack, projectcontext_job_id_test, projectcontext_job_id_publish
from ..shared.yml_job import YMLJob
from ..shared.constants import NPM_UPMCI_INSTALL_URL

class Project_PublishJob():
    
    def __init__(self, package, agent, platforms, editor_tracks):
        self.package_id = package["id"]
        self.job_id = projectcontext_job_id_publish(package["id"])
        self.yml = self.get_job_definition(package, agent, platforms, editor_tracks).get_yml()

    
    def get_job_definition(self, package, agent, platforms, editor_tracks):
        
        # define dependencies
        dependencies = [f'{projectcontext_filepath()}#{projectcontext_job_id_pack()}']
        for editor_track in editor_tracks:
            dependencies.extend([f'{projectcontext_filepath()}#{projectcontext_job_id_test(platform["os"], editor_track)}' for platform in platforms])
        
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
    