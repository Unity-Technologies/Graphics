from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import *
from ..shared.constants import VAR_UPM_REGISTRY, PATH_UNITY_REVISION, UNITY_DOWNLOADER_CLI_URL, VAR_CUSTOM_REVISION
from ..shared.yml_job import YMLJob

class Editor_PrimingMinEditorJob():
    
    def __init__(self, platform, agent):
        self.job_id = editor_job_id_test_min_editor(platform["os"])
        self.yml = self.get_job_definition(platform, agent).get_yml()


    def get_job_definition(self, platform, agent):
        
        components = platform["components"]

        if platform["os"].lower() == 'android':
            platform_os = 'windows'
        elif platform["os"].lower() == 'ios':
            platform_os = 'macos'
        else:
            platform_os = platform["os"]
        
        tmp_revision_file = "tmp_" + PATH_UNITY_REVISION

        # construct job
        job = YMLJob()
        job.set_name(f'[Min_Editor,{platform["os"]}] Editor priming')
        job.set_agent(agent)
        job.add_var_custom('PATH', '/home/bokken/bin:/home/bokken/.local/bin:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:/usr/games:/usr/local/games:/snap/bin:/sbin:/home/bokken/.npm-global/bin')
        job.add_var_custom('DISPLAY', dss(":0"))
        job.add_var_upm_registry()
        job.add_commands([
            f'pip install unity-downloader-cli --user --upgrade --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
            f'python3 .yamato/ruamel/jobs/editor/util/get_minimum_editor_version.py {tmp_revision_file}',
            f'unity-downloader-cli --source-file {tmp_revision_file} -c {platform_os} --wait --skip-download {"".join([f"-c {c} " for c in components])} > {PATH_UNITY_REVISION}',
            f'rm {tmp_revision_file}'])
        job.add_artifacts_unity_revision()
        return job
    
    