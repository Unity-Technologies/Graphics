from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import *
from ..shared.yml_job import YMLJob

class ABV_AllProjectCiNightlyJob():
    
    def __init__(self, editor, projects, nightly_config, target_branch):
        self.job_id = abv_job_id_all_project_ci_nightly(editor["name"])
        self.yml = self.get_job_definition(editor, projects, nightly_config.get("extra_dependencies",[]), target_branch).get_yml()

    
    def get_job_definition(self, editor, projects, extra_dependencies, target_branch): 
        dependencies = [] 
        for dep in extra_dependencies:
            if dep.get("pr"):
                dependencies.append({
                    'path': f'{project_filepath_all(dep["project"])}#{project_job_id_pr(dep["project"], editor["name"])}',
                    'rerun': editor["rerun_strategy"]})
            else:
                for tp in dep["test_platforms"]:
                    dependencies.append({
                        'path': f'{project_filepath_specific(dep["project"], dep["platform"], dep["api"])}#{project_job_id_test(dep["project"], dep["platform"], dep["api"], tp, editor["name"], dep["build_config"], dep["color_space"])}',
                        'rerun': editor["rerun_strategy"]})

        dependencies.append({
                'path': f'{projectcontext_filepath()}#{projectcontext_job_id_test_all(editor["name"])}',
                'rerun': editor["rerun_strategy"]
            })

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