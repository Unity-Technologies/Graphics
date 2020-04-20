from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..utils.namer import editor_job_id
from ..utils.constants import VAR_UPM_REGISTRY, PATH_UNITY_REVISION
from ..utils.yml_job import YMLJob

class Editor_PrimingJob():
    
    def __init__(self, platform, editor, agent):
        self.job_id = editor_job_id(editor["version"], platform["os"])
        self.yml = self.get_job_definition(platform, editor, agent).yml


    def get_job_definition(self, platform, editor, agent):
    
        platform_os = 'windows' if platform["os"] == 'android' else platform["os"]
        components = platform["components"]
        
        # construct job
        job = YMLJob()
        job.set_name(f'[{editor["version"]},{platform["os"]}] Editor priming')
        job.set_agent(agent)
        job.set_skip_checkout(True)
        job.add_var_custom('PATH', '/home/bokken/bin:/home/bokken/.local/bin:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:/usr/games:/usr/local/games:/snap/bin:/sbin:/home/bokken/.npm-global/bin')
        job.add_var_custom('DISPLAY', dss(":0"))
        job.add_var_upm_registry()
        job.add_var_custom_revision(editor["version"])
        job.add_commands([
                f'pip install unity-downloader-cli --user --upgrade --extra-index-url https://artifactory.internal.unity3d.com/api/pypi/common-python/simple --upgrade',
                f'unity-downloader-cli {editor["cmd"]} -o {platform_os} --wait --skip-download {"".join([f"-c {c} " for c in components])} > {PATH_UNITY_REVISION}'])
        job.add_artifacts_unity_revision()
        return job
    
    