from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import *
from ..shared.yml_job import YMLJob

class ABV_TrunkVerificationJob():
    
    def __init__(self, editor, projects, test_platforms):
        self.job_id = abv_job_id_trunk_verification(editor["version"])
        self.yml = self.get_job_definition(editor, projects, test_platforms).get_yml()

    
    def get_job_definition(self, editor, projects, test_platforms): 
        
        # define dependencies
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
                        'rerun': editor["rerun_strategy"]
                    })
        

        # construct job
        job = YMLJob()
        job.set_name(f'Trunk verification - {editor["version"]}')
        job.add_dependencies(dependencies)
        job.add_var_custom_revision(editor["version"])
        return job