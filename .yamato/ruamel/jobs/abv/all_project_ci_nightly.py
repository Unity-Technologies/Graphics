from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..utils.namer import abv_job_id_all_project_ci_nightly


def get_job_definition(editor, projects, test_platforms):  # only run for 2020.1 and trunk

    dependencies = [{
            'path': f'.yamato/upm-ci-packages.yml#all_package_ci_{editor["version"]}',
            'rerun': 'always'
        },
        {
            'path': f'.yamato/upm-ci-universal.yml#Universal_Android_OpenGLES3_{editor["version"]}',
            'rerun': 'always'
        },
        {
            'path': f'.yamato/upm-ci-universal.yml#Universal_Android_Vulkan_{editor["version"]}',
            'rerun': 'always'
        }]

    for project in projects:
        dependencies.append({
            'path': f'.yamato/upm-ci-{project["name"].lower()}.yml#All_{project["name"]}_{editor["version"]}',
            'rerun': 'always'
        })
    
    for test_platform in test_platforms:
        dependencies.append({
            'path': f'.yamato/upm-ci-abv.yml#smoke_test_{test_platform["name"]}_{editor["version"]}',
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