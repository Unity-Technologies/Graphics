from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss



def editor(platform, editor):

    platform_os = 'windows' if platform["os"] == 'android' else platform["os"]
    components = platform["components"]
    
    job = {
        'name' : f'[{editor["version"]},{platform["os"]}] Editor priming',
        'agent' : {
            'flavor' : 'b1.small',
            'type' : 'Unity::VM',
            'image' : 'cds-ops/ubuntu-16.04-base:stable'
        },
        'skip_checkout' : True,
        'variables' : {
            'PATH' : '/home/bokken/bin:/home/bokken/.local/bin:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:/usr/games:/usr/local/games:/snap/bin:/sbin:/home/bokken/.npm-global/bin',
            'DISPLAY' : dss(":0"),
            'UPM_REGISTRY': 'https://artifactory-slo.bf.unity3d.com/artifactory/api/npm/upm-candidates'
        },
        'commands' : [
            f'pip install unity-downloader-cli --user --upgrade --extra-index-url https://artifactory.internal.unity3d.com/api/pypi/common-python/simple',
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

    if editor['version'] == 'CUSTOM-REVISION':
        job['variables']['CUSTOM_REVISION'] = 'custom_revision_not_set'

    return job