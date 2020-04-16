from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..utils.namer import editor_job_id
from ..utils.constants import VAR_UPM_REGISTRY
from ..utils.shared import add_custom_revision_var

def get_job_definition(platform, editor, agent):
    platform_os = 'windows' if platform["os"] == 'android' else platform["os"]
    components = platform["components"]
    
    job = {
        'name' : f'[{editor["version"]},{platform["os"]}] Editor priming',
        'agent' : dict(agent),
        'skip_checkout' : True,
        'variables' : {
            'PATH' : '/home/bokken/bin:/home/bokken/.local/bin:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:/usr/games:/usr/local/games:/snap/bin:/sbin:/home/bokken/.npm-global/bin',
            'DISPLAY' : dss(":0"),
            'UPM_REGISTRY': VAR_UPM_REGISTRY
        },
        'commands' : [
            f'pip install unity-downloader-cli --user --upgrade --extra-index-url https://artifactory.internal.unity3d.com/api/pypi/common-python/simple --upgrade',
            f'unity-downloader-cli {editor["cmd"]} -o {platform_os} --wait --skip-download {"".join([f"-c {c} " for c in components])} > unity_revision.txt'
        ],
        'artifacts' : {
            'unity_revision.zip':{
                'paths':[
                    dss('unity_revision.txt')
                ]
            }
        },
    }

    job = add_custom_revision_var(job, editor["version"])
    return job


class Editor_PrimingJob():
    
    def __init__(self, platform, editor, agent):
        self.job_id = editor_job_id(editor["version"], platform["os"])
        self.yml = get_job_definition(platform, editor, agent)

    
    
    