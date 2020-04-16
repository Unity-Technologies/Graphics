from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..utils.namer import *
from ..utils.shared import add_custom_revision_var


def get_job_definition(editor, projects, abv_trigger_editor): 
    dependencies = [{
        'path': f'{packages_filepath()}#{package_job_id_test_all(editor["version"])}',
        'rerun': 'always'
    }]

    for project in projects:
        dependencies.append({
            'path': f'{project_filepath_all(project["name"])}#{project_job_id_all(project["name"], editor["version"])}',
            'rerun': 'always'
        })

    job = {
        'name' : f'_ABV for SRP repository - {editor["version"]}',
        'dependencies' : dependencies
    }


    if editor['version'] == abv_trigger_editor:
        job['triggers'] = {'expression': 'pull_request.target eq "master" AND NOT pull_request.draft AND NOT pull_request.push.changes.all match ["**/*.md", "doc/**/*", "**/Documentation*/**/*"]'}
    
    job = add_custom_revision_var(job, editor["version"])
    return job


class ABV_AllProjectCiJob():
    
    def __init__(self, editor, projects, abv_trigger_editor):
        self.job_id = abv_job_id_all_project_ci(editor["version"])
        self.yml = get_job_definition(editor, projects, abv_trigger_editor)