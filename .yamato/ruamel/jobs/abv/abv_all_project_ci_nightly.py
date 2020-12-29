from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import *
from ..shared.yml_job import YMLJob

class ABV_AllProjectCiNightlyJob():
    
    def __init__(self, editor, projects, target_branch):
        self.job_id = abv_job_id_all_project_ci_nightly(editor["name"])
        self.yml = self.get_job_definition(editor, projects, target_branch).get_yml()

    
    def get_job_definition(self, editor, projects, target_branch): 
    
        # define dependencies
        dependencies = [{
                'path': f'{projectcontext_filepath()}#{projectcontext_job_id_test_all(editor["name"])}',
                'rerun': editor["rerun_strategy"]
            }]

        for project in projects:
            dependencies.append({
                'path': f'{project_filepath_all(project["name"])}#{project_job_id_nightly(project["name"], editor["name"])}',
                'rerun': editor["rerun_strategy"]})

        # construct job
        job = YMLJob()
        job.set_name(f'_Nightly ABV against { editor["name"] }')
        job.add_dependencies(dependencies)
        job.add_var_custom_revision(editor["track"])
        job.add_trigger_recurrent(target_branch,'1 * * ?')
        return job