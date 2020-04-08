from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..utils.namer import packages_filepath, package_job_id_publish, package_job_id_publish_all


def get_job_definition(packages):
    job = {
        'name': f'Publish all packages',
        'agent': {
            'type':'Unity::VM',
            'image':'package-ci/ubuntu:stable',
            'flavor':'b1.large'
        },
        'dependencies': [f'{packages_filepath()}#{package_job_id_publish(package["id"])}' for package in packages],
        'commands': [
            f'git tag v$(cd com.unity.render-pipelines.core && node -e "console.log(require(\'./package.json\').version)")',
            f'git push origin --tags'
        ]
    }
    return job


class Package_PublishAllJob():
    
    def __init__(self, packages):
        self.job_id = package_job_id_publish_all()
        self.yml = get_job_definition(packages)


    
    
    