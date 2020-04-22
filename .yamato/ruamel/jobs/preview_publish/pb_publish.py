from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import *
from ..shared.yml_job import YMLJob

class PreviewPublish_PublishJob():
    
    def __init__(self, agent, package, integration_branch, auto_publish, editors, platforms):
        self.job_id = pb_job_id_publish(package["name"])
        self.yml = self.get_job_definition(agent, package, integration_branch, auto_publish, editors, platforms).yml


    def get_job_definition(self, agent, package, integration_branch, auto_publish, editors, platforms):
        
        if package["publish_source"] != True:
            raise Exception('Tried to publish package for which "publish_source" set to false.')

        # define dependencies
        dependencies = [f'{packages_filepath()}#{package_job_id_pack(package["name"])}']
        for editor in editors:
            for platform in platforms:
                dependencies.append(f'{packages_filepath()}#{package_job_id_test(package["name"],  platform["os"], editor["version"])}')

        # construct job
        job = YMLJob()
        job.set_name(f'{package["name"]} Candidates Publish')
        job.set_agent(agent)
        job.add_dependencies(dependencies)
        job.add_commands([
                f'npm install upm-ci-utils@stable -g --registry https://artifactory.prd.cds.internal.unity3d.com/artifactory/api/npm/upm-npm',
                f'upm-ci {package["type"]} publish --{package["type"]}-path {package["path"]}'])
        job.add_artifacts_packages()
        if auto_publish is True:
            job.set_trigger_recurrent(integration_branch, 'daily')
        return job
    
    