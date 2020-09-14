from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import editor_job_id
from ..shared.constants import VAR_UPM_REGISTRY, PATH_UNITY_REVISION, UNITY_DOWNLOADER_CLI_URL, VAR_CUSTOM_REVISION
from ..shared.yml_job import YMLJob

class Editor_PrimingJob():
    
    def __init__(self, platform, editor, agent):
        self.job_id = editor_job_id(editor["track"], platform["os"])
        self.yml = self.get_job_definition(platform, editor, agent).get_yml()


    def get_job_definition(self, platform, editor, agent):
        
        components = platform["components"]

        if platform["os"].lower() == 'android':
            platform_os = 'windows'
        elif platform["os"].lower() == 'ios':
            platform_os = 'macos'
        else:
            platform_os = platform["os"]
        
        # construct job
        job = YMLJob()
        job.set_name(f'[{editor["track"]},{platform["os"]}] Editor priming')
        job.set_agent(agent)
        job.set_skip_checkout(True)
        job.add_var_custom('PATH', '/home/bokken/bin:/home/bokken/.local/bin:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:/usr/games:/usr/local/games:/snap/bin:/sbin:/home/bokken/.npm-global/bin')
        job.add_var_custom('DISPLAY', dss(":0"))
        job.add_var_upm_registry()
        job.add_var_custom_revision(editor["track"])
        job.add_commands([
                f'pip install unity-downloader-cli --user --upgrade --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
                f'unity-downloader-cli -u {VAR_CUSTOM_REVISION} -o {platform_os} --wait --skip-download {"".join([f"-c {c} " for c in components])} > {PATH_UNITY_REVISION}'])
        job.add_artifacts_unity_revision()
        return job
    
    