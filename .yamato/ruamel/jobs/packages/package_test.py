from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import *
from ..shared.constants import PATH_UNITY_REVISION, NPM_UPMCI_INSTALL_URL, UNITY_DOWNLOADER_CLI_URL, PATH_PACKAGES_temp, get_editor_revision
from ..shared.yml_job import YMLJob


class Package_TestJob():
    
    def __init__(self, package, platform, editor):
        self.package_id = package["id"]
        self.job_id = package_job_id_test(package["id"],platform["os"],editor["track"])
        self.yml = self.get_job_definition(package,platform, editor).get_yml()

    
    def get_job_definition(self, package, platform, editor):

        # define dependencies
        dependencies = [f'{packages_filepath()}#{package_job_id_pack(dep)}' for dep in package["dependencies"]]
        if editor['track'].lower() == 'custom-revision':
            dependencies.extend([f'{editor_priming_filepath()}#{editor_job_id(editor["track"], platform["os"]) }'])
               
        # define commands
        commands = [
                f'npm install upm-ci-utils@stable -g --registry {NPM_UPMCI_INSTALL_URL}',
                f'pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
                f'unity-downloader-cli -u {get_editor_revision(editor, platform["os"])} -c editor --wait --published-only']
        if platform["os"].lower() == 'windows':
                commands.append(f'mkdir upm-ci~\\packages')
                commands.append(f'copy {PATH_PACKAGES_temp}\\{package["id"]}\\upm-ci~\\packages\\packages.json upm-ci~\\packages')
                commands.append(f'for /r {PATH_PACKAGES_temp} %%x in (*.tgz) do copy %%x upm-ci~\packages')
        elif platform["os"].lower() == 'macos':
                commands.append(f'mkdir upm-ci~ && mkdir upm-ci~/packages')
                commands.append(f'cp {PATH_PACKAGES_temp}/{package["id"]}/upm-ci~/packages/packages.json upm-ci~/packages')
                commands.append(f'cp {PATH_PACKAGES_temp}/**/upm-ci~/packages/*.tgz upm-ci~/packages')
        
        if package.get('hascodependencies', None) is not None:
            commands.append(platform["copycmd"])
        commands.append(f'upm-ci package test -u {platform["editorpath"]} --package-path {package["packagename"]} --extra-utr-arg="--compilation-errors-as-warnings"')


        # construct job
        job = YMLJob()
        job.set_name(f'Test { package["name"] } {platform["name"]} {editor["track"]}')
        job.set_agent(platform['agent_package'])
        job.add_dependencies(dependencies)
        job.add_commands(commands)
        job.add_artifacts_test_results()
        return job


    
    
    