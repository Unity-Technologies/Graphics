from ruamel import yaml
from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ruamel.yaml.scalarstring import PlainScalarString as pss
from .commands._cmd_mapper import get_cmd
from ..shared.namer import *
from ..shared.yml_job import YMLJob

def _job(project_name, test_platform_name, editor, platform, api, cmd):

    # define name
    if test_platform_name.lower() == 'standalone_build':
        job_name = f'Build {project_name} on {platform["name"]}_{api}_Player on version {editor["version"]}'
    else:
        job_name = f'{project_name} on {platform["name"]}_{api}_{test_platform_name} on version {editor["version"]}'

    # define agent
    agent = platform.get(f'agent_{test_platform_name.lower()}', platform['agent_default']) # replace(" ","_") called for playmode_XR
    
    # define dependencies
    dependencies = [{
                'path' : f'{editor_filepath()}#{editor_job_id(editor["version"], platform["os"])}',
                'rerun' : editor["rerun_strategy"]}]

    # construct job
    job = YMLJob()
    job.set_name(job_name)
    job.set_agent(agent)
    job.add_var_upm_registry()
    job.add_var_custom_revision(editor["version"])
    job.add_dependencies(dependencies)
    job.add_commands(cmd)
    job.add_artifacts_test_results()
    return job
