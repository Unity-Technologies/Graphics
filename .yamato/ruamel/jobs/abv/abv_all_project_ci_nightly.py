from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import *
from ..shared.yml_job import YMLJob

class ABV_AllProjectCiNightlyJob():
    
    def __init__(self, editor, projects, test_platforms, nightly_config, target_branch):
        if editor["track"] not in nightly_config["allowed_editors"]:
            raise Exception(f'Tried to construct nightly with PR trigger for version {editor["track"]}')
        self.job_id = abv_job_id_all_project_ci_nightly(editor["track"])
        self.yml = self.get_job_definition(editor, projects, test_platforms, nightly_config.get("extra_dependencies",[]), target_branch).get_yml()

    
    def get_job_definition(self, editor, projects, test_platforms, extra_dependencies, target_branch): 

        # define dependencies
        dependencies = [{
                'path': f'{abv_filepath()}#{abv_job_id_all_project_ci(editor["track"])}',
                'rerun': editor["rerun_strategy"]}]

        for test_platform in test_platforms: # TODO replace with all_smoke_tests if rerun strategy can override lower level ones
            dependencies.append({
                'path': f'{abv_filepath()}#{abv_job_id_smoke_test(editor["track"],test_platform["name"])}',
                'rerun': editor["rerun_strategy"]})

        for dep in extra_dependencies:
            if dep.get("all"):
                dependencies.append({
                    'path': f'{project_filepath_all(dep["project"])}#{project_job_id_all(dep["project"], editor["track"])}',
                    'rerun': editor["rerun_strategy"]})
            else:
                for tp in dep["test_platforms"]:
                    dependencies.append({
                        'path': f'{project_filepath_specific(dep["project"], dep["platform"], dep["api"])}#{project_job_id_test(dep["project"], dep["platform"], dep["api"], tp, editor["track"])}',
                        'rerun': editor["rerun_strategy"]})
            
        # construct job
        job = YMLJob()
        job.set_name(f'_Nightly ABV against { editor["track"] }')
        job.add_dependencies(dependencies)
        job.add_var_custom_revision(editor["track"])
        job.add_trigger_recurrent(target_branch,'0 * * ?')
        return job