from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..utils.namer import *


def get_job_definition(package, platform, editor):
    job = {
        'name': f'Test { package["name"] } {platform["name"]} {editor["version"]} - dependencies',
        'agent': dict(platform["agent"]),
        'dependencies':[
            f'{editor_filepath()}#{editor_job_id(editor["version"], platform["os"]) }',
            f'{packages_filepath()}#{package_job_id_test(package["id"],platform["name"],editor["version"])}'
        ],
        'commands': [
            f'npm install upm-ci-utils@stable -g --registry https://api.bintray.com/npm/unity/unity-npm',
            f'pip install unity-downloader-cli --extra-index-url https://artifactory.internal.unity3d.com/api/pypi/common-python/simple --upgrade',
            f'unity-downloader-cli --source-file unity_revision.txt -c editor --wait --published-only'
        ],
        'artifacts':{
            'logs':{
                'paths': [
                    dss("**/upm-ci~/test-results/**/*")
                ]
            }
        }
    }

    [job["commands"].append(f'{packages_filepath()}#{package_job_id_pack(package["id"])}') for dep in package["dependencies"]]

    if package.get('hascodependencies', None) is not None:
        job["commands"].append(platform["copycmd"])
    job["commands"].append(f'upm-ci package test -u {platform["editorpath"]} --type updated-dependencies-tests --package-path {package["packagename"]}')

    return job


class Package_TestDependenciesJob():
    
    def __init__(self, package, platform, editor):
        self.package_id = package["id"]
        self.job_id = package_job_id_test_dependencies(package["id"],platform["name"],editor["version"])
        self.yml = get_job_definition(package,platform, editor)


    
    
    