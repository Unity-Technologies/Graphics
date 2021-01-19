from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import *
from ..shared.yml_job import YMLJob

class Project_NightlyJob():
    
    def __init__(self, project, editor, dependencies_in_nightly):
        self.project = project
        self.job_id = project_job_id_nightly(project["name"], editor["name"])
        self.yml = self.get_job_definition(project, editor, dependencies_in_nightly).get_yml()

    
    def get_job_definition(self, project, editor, dependencies_in_nightly):
    
        # define dependencies
        dependencies = []
        for dep in dependencies_in_nightly:
            project_dep = dep.get('project', project["name"])
            
            if dep.get("pr"):
                dependencies.append({
                    'path': f'{project_filepath_all(project_dep)}#{project_job_id_pr(project_dep, editor["name"])}',
                    'rerun': editor["rerun_strategy"]})
            elif dep.get("nightly"):
                dependencies.append({
                    'path': f'{project_filepath_all(project_dep)}#{project_job_id_nightly(project_dep, editor["name"])}',
                    'rerun': editor["rerun_strategy"]})
            else:
                for test_platform in dep["test_platforms"]:
                        
                    file = project_filepath_specific(project_dep, dep["platform"], dep["api"])
                    job_id = project_job_id_test(project_dep,dep["platform"],dep["api"],test_platform,editor["name"],dep["build_config"],dep["color_space"])

                    dependencies.append({
                            'path' : f'{file}#{job_id}',
                            'rerun' : editor["rerun_strategy"]})

        # construct job
        job = YMLJob()
        job.set_name(f'Nightly {project["name"]} - {editor["name"]}')
        job.add_dependencies(dependencies)
        # if expression_trigger != "":
        #     job.set_trigger_on_expression(expression_trigger)
        job.add_var_custom_revision(editor["track"])
        job.add_var_custom('UTR_VERSION', dss("current"))
        job.add_var_custom('TEST_FILTER', '.*')
        if project.get('variables'):
            for key,value in project.get('variables').items():
                job.add_var_custom(key,value)
        return job
    
    