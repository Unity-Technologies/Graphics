from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..utils.namer import packages_filepath, package_job_id_test_all, package_job_id_test, package_job_id_test_dependencies
from ..utils.yml_job import YMLJob

def get_job_definition(packages, agent, platforms, editor):

    # define dependencies
    dependencies = []
    for platform in platforms:
        for package in packages:
            dependencies.append(f'{packages_filepath()}#{package_job_id_test(package["id"],platform["name"],editor["version"])}')
            #dependencies.append(f'{packages_filepath()}#{package_job_id_test_dependencies(package["id"],platform["name"],editor["version"])}')
    
    # construct job
    job = YMLJob()
    job.set_name(f'Pack and test all packages - { editor["version"] }')
    job.set_agent(agent)
    job.add_dependencies(dependencies)
    job.add_commands([
            f'npm install upm-ci-utils@stable -g --registry https://api.bintray.com/npm/unity/unity-npm',
            f'upm-ci package izon -t',
            f'upm-ci package izon -d'])
    return job


class Package_AllPackageCiJob():
    
    def __init__(self, packages, agent, platforms, editor):
        self.job_id = package_job_id_test_all(editor["version"])
        self.yml = get_job_definition(packages, agent, platforms, editor).yml


    
    
    