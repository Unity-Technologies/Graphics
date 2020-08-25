from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import *
from ..shared.yml_job import YMLJob

class ABV_TrunkVerificationJob():
    
    def __init__(self, editor, extra_dependencies):
        self.job_id = abv_job_id_trunk_verification(editor["version"])
        self.yml = self.get_job_definition(editor, extra_dependencies).get_yml()

    
    def get_job_definition(self, editor, extra_dependencies): 
        
        # define dependencies
        dependencies = []
        for dep in extra_dependencies:
            if dep.get("all"):
                dependencies.append({
                    'path': f'{project_filepath_all(dep["project"])}#{project_job_id_all(dep["project"], editor["version"])}',
                    'rerun': editor["rerun_strategy"]})
            else:
                for tp in dep["test_platforms"]:
                    dependencies.append({
                        'path': f'{project_filepath_specific(dep["project"], dep["platform"], dep["api"])}#{project_job_id_test(dep["project"], dep["platform"], dep["api"], tp, editor["version"])}',
                        'rerun': editor["rerun_strategy"]})

        # construct job
        job = YMLJob()
        job.set_name(f'Trunk verification - {editor["version"]}')
        job.add_dependencies(dependencies)
        job.add_var_custom_revision(editor["version"])
        return job