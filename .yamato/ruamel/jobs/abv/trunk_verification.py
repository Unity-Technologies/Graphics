from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..utils.namer import *

def get_job_definition(editor, projects, test_platforms):  # TODO only run for 2020.1 and trunk
    dependencies = []
    for project in projects:
        if project["name"] in ['HDRP_Standalone', 'Universal_Stereo','ShaderGraph_Stereo']:
            continue
        for test_platform in test_platforms:
            if test_platform["name"] == 'Standalone':
                continue
            elif test_platform["name"] == 'editmode' and project["name"] == 'VFX_LWRP':
                continue
            else:
                dependencies.append({
                    'path' : f'{project_filepath_specific(project["name"], "Win", "DX11")}#{project_job_id_test(project["name"], "Win", "DX11", test_platform["name"], editor["version"])}',
                    'rerun': 'always'
                })

    job = {
        'name': f'Trunk verification - {editor["version"]}',
        'agent': {
            'type':'Unity::VM',
            'image':'cds-ops/ubuntu-18.04-agent:stable',
            'flavor':'b1.small'
        }, 
        'commands': ['dir'],
        'dependencies': dependencies
    }

    if editor['version'] == 'CUSTOM-REVISION':
        job['variables'] = {'CUSTOM_REVISION':'custom_revision_not_set'}
    
    return job


class ABV_TrunkVerificationJob():
    
    def __init__(self, editor, projects, test_platforms):
        self.job_id = abv_job_id_trunk_verification(editor["version"])
        self.yml = get_job_definition(editor, projects, test_platforms)