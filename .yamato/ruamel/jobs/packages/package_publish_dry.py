from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import package_job_id_publish_dry, packages_filepath, package_job_id_pack, package_job_id_test
from ..shared.yml_job import YMLJob
from ..shared.constants import NPM_UPMCI_INSTALL_URL
from .package_publish import Package_PublishJob


class Package_PublishDryJob():
    
    def __init__(self, package, agent, platforms, editor_tracks):
        self.package_id = package["id"]
        self.job_id = package_job_id_publish_dry(package["id"])
        self.yml = self.get_job_definition(package, agent, platforms, editor_tracks)

    
    def get_job_definition(self, package, agent, platforms, editor_tracks):
        
        job = Package_PublishJob(package, agent, platforms, editor_tracks)
        job.yml['commands'][-1] += ' --dry-run'
        job.yml['name'] += ' [dry run]'

        return job.yml
    