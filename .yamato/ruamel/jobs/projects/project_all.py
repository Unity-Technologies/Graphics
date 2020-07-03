from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import *
from ..shared.yml_job import YMLJob

class Project_AllJob():
    
    def __init__(self, project, editor, dependencies_in_all):
        self.project = project
        self.job_id = project_job_id_all(project, editor["version"])
        self.yml = self.get_job_definition(project, editor, dependencies_in_all).get_yml()

    
    def get_job_definition(self, project, editor, dependencies_in_all):
    
        # define dependencies
        dependencies = []
        for dep in dependencies_in_all:
            project_dep = dep.get('project', project)
            
            if dep.get("all"):
                dependencies.append({
                    'path': f'{project_filepath_all(project_dep)}#{project_job_id_all(project_dep, editor["version"])}',
                    'rerun': editor["rerun_strategy"]})
            else:
                for test_platform in dep["test_platforms"]:
                        
                    file = project_filepath_specific(project_dep, dep["platform"], dep["api"])
                    job_id = project_job_id_test(project_dep,dep["platform"],dep["api"],test_platform,editor["version"])

                    dependencies.append({
                            'path' : f'{file}#{job_id}',
                            'rerun' : editor["rerun_strategy"]})

        # construct job
        job = YMLJob()
        job.set_name(f'All {project} CI - {editor["version"]}')
        job.add_dependencies(dependencies)
        job.add_var_custom_revision(editor["version"])
        return job
    
    