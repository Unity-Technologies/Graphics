from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..utils.namer import packages_filepath, package_job_id_test_all, package_job_id_test, package_job_id_test_dependencies

def get_job_definition(packages, platforms, editor):
    dependencies = []
    for platform in platforms:
        for package in packages:
            dependencies.append(f'{packages_filepath()}#{package_job_id_test(package["id"],platform["name"],editor["version"])}')
            dependencies.append(f'{packages_filepath()}#{package_job_id_test_dependencies(package["id"],platform["name"],editor["version"])}')
    
    job = {
        'name': f'Pack and test all packages - { editor["version"] }',
        'agent':{
            'type':'Unity::VM',
            'image':'package-ci/win10:stable',
            'flavor':'b1.large'
        },
        'dependencies': dependencies,
        'commands': [
            f'npm install upm-ci-utils@stable -g --registry https://api.bintray.com/npm/unity/unity-npm',
            f'upm-ci package izon -t',
            f'upm-ci package izon -d'
        ]
    }
    return job


class Package_AllPackageCiJob():
    
    def __init__(self, packages, platforms, editor):
        self.job_id = package_job_id_test_all(editor["version"])
        self.yml = get_job_definition(packages,platforms, editor)


    
    
    