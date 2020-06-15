from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import pb_job_id_auto_version
from ..shared.yml_job import YMLJob
from ..shared.constants import NPM_UPMCI_INSTALL_URL

class PreviewPublish_AutoVersionJob():
    
    def __init__(self, agent, packages, target_branch, auto_version):
        self.job_id = pb_job_id_auto_version()
        self.yml = self.get_job_definition(agent, packages, target_branch, auto_version).get_yml()


    def get_job_definition(self, agent, packages, target_branch, auto_version):
        bump_packages_args = " ".join([f'--{package["type"]}-path {package["path"]}' for package in packages])

        # construct job
        job = YMLJob()
        job.set_name(f'Auto version')
        job.set_agent(agent)
        job.add_var_custom('PATH', '/home/bokken/bin:/home/bokken/.local/bin:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:/usr/games:/usr/local/games:/snap/bin:/sbin:/home/bokken/.npm-global/bin')
        job.add_var_custom('DISPLAY', dss(":0"))
        job.add_commands([
                f'npm install upm-ci-utils@stable -g --registry {NPM_UPMCI_INSTALL_URL}',
                f'upm-ci utils auto-version bump {bump_packages_args}',
                f'upm-ci utils auto-version commit --push'])
        job.add_trigger_integration_branch(target_branch)
        job.add_artifacts_packages()
        # if auto_version is True:
        #     job.add_trigger_integration_branch(target_branch)
        return job
    