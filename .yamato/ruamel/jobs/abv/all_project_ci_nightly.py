from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..utils.namer import *


def get_job_definition(editor, projects, test_platforms):  # only run for 2020.1 and trunk

    dependencies = [{
            'path': f'{packages_filepath()}#{package_job_id_test_all(editor["version"])}',
            'rerun': 'always'
        },
        { #TODO add these under project loop
            'path': f'{project_filepath_specific("Universal", "Android", "OpenGLES3")}#{project_job_id_test("Universal", "Android", "OpenGLES3", "", editor["version"])}', # TODO 
            'rerun': 'always'
        },
        {
            'path': f'{project_filepath_specific("Universal", "Android", "Vulkan")}#{project_job_id_test("Universal", "Android", "Vulkan", "", editor["version"])}',
            'rerun': 'always'
        }]

    for project in projects:
        dependencies.append({
            'path': f'{project_filepath_all(project["name"])}#{project_job_id_all(project["name"], editor["version"])}',
            'rerun': 'always'
        })
    
    for test_platform in test_platforms:
        dependencies.append({
            'path': f'{abv_filepath()}#{abv_job_id_smoke_test(editor["version"],test_platform["name"])}',
            'rerun': 'always'
        })



    job = {
        'name' :  f'_Nightly ABV against { editor["version"] }',
        'dependencies': dependencies,
        'triggers':{
            'recurring': [
                {
                    'branch' : 'master',
                    'frequency' : '0 * * ?'
                }
            ]
        }
    }

    return job


class ABV_AllProjectCiNightlyJob():
    
    def __init__(self, editor, projects, test_platforms):
        self.job_id = abv_job_id_all_project_ci_nightly(editor["version"])
        self.yml = get_job_definition(editor, projects, test_platforms)