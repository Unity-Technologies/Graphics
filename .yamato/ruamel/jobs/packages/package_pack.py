from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..utils.namer import package_job_id_pack
from ..utils.constants import PATH_PACKAGES


def get_job_definition(package):
    job = {
        'name': f'Pack {package["name"]}',
        'agent': {
            'type':'Unity::VM',
            'image':'package-ci/win10:stable', #TODO no hardcoding
            'flavor':'b1.large'
        },
        'commands': [
            f'npm install upm-ci-utils@stable -g --registry https://api.bintray.com/npm/unity/unity-npm',
            f'upm-ci package pack --package-path {package["packagename"]}'
        ],
        'artifacts':{
            'packages':{
                'paths': [
                    dss(PATH_PACKAGES)
                ]
            }
        }
    }
    return job


class Package_PackJob():
    
    def __init__(self, package):
        self.package_id = package["id"]
        self.job_id = package_job_id_pack(package["id"])
        self.yml = get_job_definition(package)

    
    
    