from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss

# TODO this file is plain conversion from existing abv and doesnt run. also, android/packages dont exist yet, upm-ci prefixes are gone
default_agent = {
    'type':'Unity::VM',
    'image':'cds-ops/ubuntu-18.04-agent:stable',
    'flavor':'b1.small'
}

default_agent_gpu = {
    'type':'Unity::VM::GPU',
    'image':'cds-ops/ubuntu-18.04-agent:stable',
    'flavor':'b1.small'
}

def all_project_ci(editor, projects):

    dependencies = [{
        'path': f'.yamato/upm-ci-packages.yml#all_package_ci_{editor["version"]}',
        'rerun': 'always'
    }]

    for project in projects:
        dependencies.append({
            'path': f'.yamato/upm-ci-{project["name"].lower()}.yml#All_{project["name"]}_{editor["version"]}',
            'rerun': 'always'
        })

    job = {
        'name' : f'_ABV for SRP repository - {editor["version"]}',
        'agent' : default_agent,
        'dependencies' : dependencies,
        'commands' : ['dir'],
        'artifacts' : {
            'logs':{
                'paths':[
                    dss('**/test-results/**') 
                ]
            }
        },
    }

    if editor['version'] == 'CUSTOM-REVISION':
        job['variables'] = {'CUSTOM-REVISION':'custom_revision_not_set'}
    elif editor['version'] == 'fast-2020.1':
        job['triggers'] = {'expression': 'pull_request.target eq "master" AND NOT pull_request.draft AND NOT pull_request.push.changes.all match ["**/*.md", "doc/**/*", "**/Documentation*/**/*"]'}
    return job

def all_project_ci_nightly(editor, projects, test_platforms):  # only run for 2020.1 and trunk

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


def smoke_test(editor, test_platform):
    job = {
        'name': f'SRP Smoke Test - {test_platform["name"]}_{editor["version"]}',
        'agent': default_agent if test_platform["name"] == 'editmode' else default_agent_gpu, # TODO if editmode then ::VM, else ::VM::GPU,
        'variables':{
            'UPM_REGISTRY':'https://artifactory-slo.bf.unity3d.com/artifactory/api/npm/upm-candidates'
        },
        'commands': [
            f'git clone git@github.cds.internal.unity3d.com:unity/utr.git TestProjects/SRP_SmokeTest/utr',
            f'pip install unity-downloader-cli --extra-index-url https://artifactory.internal.unity3d.com/api/pypi/common-python/simple --upgrade',
            f'cd TestProjects/SRP_SmokeTest && unity-downloader-cli --source-file ../../unity_revision.txt -c editor --wait --published-only'
        ],
        'dependencies': [
            {
                'path':f'.yamato/z_editor.yml#editor:priming:{editor["version"]}:windows',
                'rerun': 'on-new-revision'
            }
        ],
        'artifacts' : {
            'logs':{
                'paths':[
                    dss('**/test-results/**')
                ]
            }
        },
    }


    if editor['version'] == 'CUSTOM-REVISION':
        job['variables']['CUSTOM_REVISION'] = 'custom_revision_not_set'
    
    if test_platform['name'] != 'Standalone':
        job['commands'].append(f'cd TestProjects/SRP_SmokeTest && utr/utr {test_platform["args"]}Windows64 --testproject=. --editor-location=.Editor --artifacts_path=test-results --timeout=1200')
    else:
        job['commands'].append(f'cd TestProjects/SRP_SmokeTest && utr/utr {test_platform["args"]} --testproject=. --editor-location=.Editor --artifacts_path=test-results')
    
    return job


def all_smoke_tests(editor, test_platforms):

    dependencies = []
    for test_platform in test_platforms:
        dependencies.append({
            'path': f'.yamato/upm-ci-abv.yml#smoke_test_{test_platform["name"]}_{editor["version"]}',
            'rerun': 'on-new-revision'
        })

    job = {
        'name': f'All Smoke Tests - {editor["version"]}',
        'agent': default_agent, 
        'commands': ['dir'],
        'dependencies': dependencies
    }

    if editor['version'] == 'CUSTOM-REVISION':
        job['variables'] = {'CUSTOM-REVISION':'custom_revision_not_set'}
    
    return job

def trunk_verification(editor,projects,test_platforms):

    dependencies = []
    for project in projects:
        if project["name"] in ['HDRP_Standalone', 'Universal_Stereo','ShaderGraph_Stereo']:
            continue
        for test_platform in test_platforms:
            if test_platform["name"] == 'Standalone':
                continue
            elif test_platform["name"] == 'editmode' and project["name"] == 'VFX_LWRP':
                continue
            else:
                dependencies.append({
                    'path': f'.yamato/upm-ci-{project["name"].lower()}.yml#{ project["name"] }_Win_DX11_{ test_platform["name"] }_{ editor["version"]}',
                    'rerun': 'always'
                })

    job = {
        'name': f'Trunk verification - {editor["version"]}',
        'agent': default_agent, 
        'commands': ['dir'],
        'dependencies': dependencies
    }

    if editor['version'] == 'CUSTOM-REVISION':
        job['variables'] = {'CUSTOM-REVISION':'custom_revision_not_set'}
    
    return job
