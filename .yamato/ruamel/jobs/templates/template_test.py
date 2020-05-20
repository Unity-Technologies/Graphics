from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import *
from ..shared.constants import PATH_UNITY_REVISION, NPM_UPMCI_INSTALL_URL
from ..shared.yml_job import YMLJob

class Template_TestJob():
    
    def __init__(self, template, platform, editor):
        self.job_id = template_job_id_test(template["id"],platform["name"],editor["version"])
        self.yml = self.get_job_definition(template, platform, editor).get_yml()

    
    def get_job_definition(self, template, platform, editor):

        # define dependencies
        dependencies = [f'{editor_filepath()}#{editor_job_id(editor["version"], platform["os"]) }']
        dependencies.extend([f'{packages_filepath()}#{package_job_id_pack(dep)}' for dep in template["dependencies"]])
        

        # define commands
        commands = [
                f'npm install upm-ci-utils@stable -g --registry {NPM_UPMCI_INSTALL_URL}',
                f'pip install unity-downloader-cli --extra-index-url https://artifactory.internal.unity3d.com/api/pypi/common-python/simple --upgrade',
                f'unity-downloader-cli --source-file {PATH_UNITY_REVISION} -c editor --wait --published-only']
        if template.get('hascodependencies', None) is not None:
            commands.append(platform["copycmd"])
        commands.append(f'upm-ci template test -u {platform["editorpath"]} --project-path {template["packagename"]}')


        # construct job
        job = YMLJob()
        job.set_name(f'Test { template["name"] } {platform["name"]} {editor["version"]}')
        job.set_agent(platform['agent_default'])
        job.add_dependencies(dependencies)
        job.add_commands(commands)
        job.add_artifacts_test_results()
        return job


    
    
    