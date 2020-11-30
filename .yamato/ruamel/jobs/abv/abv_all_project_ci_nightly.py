from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import *
from ..shared.yml_job import YMLJob

class ABV_AllProjectCiNightlyJob():
    
    def __init__(self, editor, projects, nightly_config, target_branch, build_configs, color_space):
        self.job_id = abv_job_id_all_project_ci_nightly(editor["name"])
        self.yml = self.get_job_definition(editor, projects, nightly_config.get("extra_dependencies",[]), target_branch, build_configs, color_space).get_yml()

    
    def get_job_definition(self, editor, projects, extra_dependencies, target_branch, build_configs, color_space): 

        # define dependencies
        dependencies = [
            {
                'path': f'{abv_filepath()}#{abv_job_id_all_project_ci(editor["name"])}',
                'rerun': editor["rerun_strategy"]},
            # Todo: re-add template tests to the nightly once the publishing issue with upm-ci template test is fixed:
            # "(There has never been a full release of this package. The major must be 0 or 1.)"
            # {
            #     'path': f'{templates_filepath()}#{template_job_id_test_all(editor["track"])}',
            #     'rerun': editor["rerun_strategy"]
            # }
        ]

        for dep in extra_dependencies:
            if dep.get("all"):
                dependencies.append({
                    'path': f'{project_filepath_all(dep["project"])}#{project_job_id_all(dep["project"], editor["name"])}',
                    'rerun': editor["rerun_strategy"]})
            else:
                for tp in dep["test_platforms"]:
                    dependencies.append({
                        'path': f'{project_filepath_specific(dep["project"], dep["platform"], dep["api"])}#{project_job_id_test(dep["project"], dep["platform"], dep["api"], tp, editor["name"], dep["build_config"], dep["color_space"])}',
                        'rerun': editor["rerun_strategy"]})
            
        # construct job
        job = YMLJob()
        job.set_name(f'_Nightly ABV against { editor["name"] }')
        job.add_dependencies(dependencies)
        job.add_var_custom_revision(editor["track"])
        job.add_trigger_recurrent(target_branch,'0 * * ?')
        return job