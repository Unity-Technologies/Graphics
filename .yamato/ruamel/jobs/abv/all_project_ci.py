from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..utils.namer import *
from ..utils.yml_job import YMLJob


def get_job_definition(editor, projects, abv_trigger_editor): 
    
    # define dependencies
    dependencies = [{
        'path': f'{packages_filepath()}#{package_job_id_test_all(editor["version"])}',
        'rerun': 'always'}]

    for project in projects:
        dependencies.append({
            'path': f'{project_filepath_all(project["name"])}#{project_job_id_all(project["name"], editor["version"])}',
            'rerun': 'always'})

    # construct job
    job = YMLJob()
    job.set_name(f'_ABV for SRP repository - {editor["version"]}')
    job.add_dependencies(dependencies)
    job.add_var_custom_revision(editor["version"])
    if editor['version'] == abv_trigger_editor:
        job.set_trigger_on_expression('pull_request.target eq "master" AND NOT pull_request.draft AND NOT pull_request.push.changes.all match ["**/*.md", "doc/**/*", "**/Documentation*/**/*"]')
    return job


class ABV_AllProjectCiJob():
    
    def __init__(self, editor, projects, abv_trigger_editor):
        self.job_id = abv_job_id_all_project_ci(editor["version"])
        self.yml = get_job_definition(editor, projects, abv_trigger_editor).yml