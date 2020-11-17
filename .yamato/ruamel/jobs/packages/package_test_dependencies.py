from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import *
from ..shared.constants import PATH_UNITY_REVISION, NPM_UPMCI_INSTALL_URL, UNITY_DOWNLOADER_CLI_URL, get_unity_downloader_cli_cmd
from ..shared.yml_job import YMLJob

class Package_TestDependenciesJob():
    
    def __init__(self, package, platform, editor):
        self.package_id = package["id"]
        self.job_id = package_job_id_test_dependencies(package["id"],platform["os"],editor["name"])
        self.yml = self.get_job_definition(package,platform, editor).get_yml()


    def get_job_definition(yml, package, platform, editor):
    
        # define dependencies
        dependencies = [f'{packages_filepath()}#{package_job_id_test(package["id"],platform["os"],editor["name"])}']
        dependencies.extend([f'{packages_filepath()}#{package_job_id_pack(dep)}' for dep in package["dependencies"]])
        if not editor["editor_pinning"]:
            dependencies.extend([f'{editor_priming_filepath()}#{editor_job_id(editor["name"], platform["os"]) }'])
        
        # define commands
        commands =  [
                f'npm install upm-ci-utils@stable -g --registry {NPM_UPMCI_INSTALL_URL}',
                f'pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
                f'unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"]) } -c editor --wait --published-only']
        if package.get('hascodependencies', None) is not None:
            if platform["os"].lower() == 'windows':
                commands.append(f'mkdir upm-ci~\\packages')
                commands.append(f'copy packages_temp\\{package["id"]}\\upm-ci~\\packages\\packages.json upm-ci~\\packages')
                commands.append(f'for /r packages_temp %%x in (*.tgz) do copy %%x upm-ci~\packages')
            elif platform["os"].lower() == 'macos':
                commands.append(f'mkdir upm-ci~ && mkdir upm-ci~/packages')
                commands.append(f'cp packages_temp/{package["id"]}/upm-ci~/packages/packages.json upm-ci~/packages')
                commands.append(f'cp packages_temp/**/upm-ci~/packages/*.tgz upm-ci~/packages')
            commands.append(platform["copycmd"])
        commands.append(f'upm-ci package test -u {platform["editorpath"]} --type updated-dependencies-tests --package-path {package["packagename"]}')


        # construct job
        job = YMLJob()
        job.set_name(f'Test { package["name"] } {platform["name"]} {editor["name"]} - dependencies')
        job.set_agent(platform['agent_package'])
        job.add_dependencies(dependencies)
        job.add_commands(commands)
        job.add_artifacts_test_results()
        return job
    
    
    