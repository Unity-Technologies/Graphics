from ruamel import yaml
from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ruamel.yaml.scalarstring import PlainScalarString as pss
from .commands._cmd_mapper import get_cmd
from ..shared.namer import *
from ..shared.yml_job import YMLJob

def _job(project, test_platform_name, editor, platform, api, cmd, build_config, color_space):

    # define name
    if test_platform_name.lower() == 'standalone_build':
        job_name = f'Build {project["name"]} on {platform["name"]}_{api["name"]}_{build_config["name"]}_{color_space}_Player on version {editor["name"]}'
    else:
        job_name = f'{project["name"]} on {platform["name"]}_{api["name"]}_{test_platform_name}_{build_config["name"]}_{color_space} on version {editor["name"]}'

    # define agent
    platform_agents_project = platform.get(f'agents_project_{api["name"]}', platform.get('agents_project'))
    agent = platform_agents_project.get(f'{test_platform_name.lower()}', platform_agents_project['default']) # replace(" ","_") called for playmode_XR

    # construct job
    job = YMLJob()
    job.set_name(job_name)
    job.set_agent(agent)
    job.add_var_upm_registry()
    job.add_var_custom_revision(editor["track"])
    job.add_commands(cmd)
    job.add_artifacts_test_results()

    if test_platform_name.lower()=='standalone':
        job.add_artifacts_project_logs(project.get("folder_standalone", project["folder"]))
    else:
        job.add_artifacts_project_logs(project["folder"])
        


    if not editor['editor_pinning']:
        job.add_dependencies([{
                'path' : f'{editor_priming_filepath()}#{editor_job_id(editor["name"], platform["os"])}',
                'rerun' : editor["rerun_strategy"]}])

    return job
