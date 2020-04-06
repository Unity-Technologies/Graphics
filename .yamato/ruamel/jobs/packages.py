from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss

agent_win = {
    'type':'Unity::VM',
    'image':'package-ci/win10:stable',
    'flavor':'b1.large'
}

agent_osx = {
    'type':'Unity::VM::osx',
    'image':'buildfarm/mac:stable',
    'flavor':'m1.mac'
}

agent_ubuntu = {
    'type':'Unity::VM',
    'image':'package-ci/ubuntu:stable',
    'flavor':'b1.large'
}

def pack(package):
    job = {
        'name': f'z_(do not use) Pack {package["name"]}',
        'agent': agent_win,
        'commands': [
            f'npm install upm-ci-utils@stable -g --registry https://api.bintray.com/npm/unity/unity-npm',
            f'upm-ci package pack --package-path {package["packagename"]}'
        ],
        'artifacts':{
            'packages':{
                'paths': [
                    dss("upm-ci~/packages/**/*")
                ]
            }
        }
    }
    return job

def test(package, platform, editor):
    job = {
        'name': f'z_(do not use) Test { package["name"] } {platform["name"]} {editor["version"]}',
        'agent': {
            'type': platform["agent"]["type"],
            'image': platform["agent"]["image"],
            'flavor': platform["agent"]["flavor"]
        },
        'dependencies':[
            f'.yamato/z_editor.yml#editor:priming:{ editor["version"] }:{ platform["os"] }'
        ],
        'commands': [
            f'npm install upm-ci-utils@stable -g --registry https://api.bintray.com/npm/unity/unity-npm',
            f'pip install unity-downloader-cli --extra-index-url https://artifactory.internal.unity3d.com/api/pypi/common-python/simple --upgrade',
            f'unity-downloader-cli --source-file unity_revision.txt -c editor --wait --published-only'
        ],
        'artifacts':{
            'packages':{
                'paths': [
                    dss("**/upm-ci~/test-results/**/*")
                ]
            }
        }
    }

    [job["commands"].append(dep) for dep in package["dependencies"]]

    if package.get('hascodependencies', None) is not None:
        job["commands"].append(package["copycmd"])
    else:
        job["commands"].append(f'upm-ci package test -u {platform["editorpath"]} --package-path {package["packagename"]}')

    return job


def test_dependencies(package, platform, editor):
    job = {
        'name': f'z_(do not use) Test { package["name"] } {platform["name"]} {editor["version"]} - dependencies',
        'agent': {
            'type': platform["agent"]["type"],
            'image': platform["agent"]["image"],
            'flavor': platform["agent"]["flavor"]
        },
        'dependencies':[
            f'.yamato/z_editor.yml#editor:priming:{ editor["version"] }:{ platform["os"] }',
            f'.yamato/upm-ci-packages.yml#test_{package["id"]}_{platform["name"]}_{editor["version"]}'
        ],
        'commands': [
            f'npm install upm-ci-utils@stable -g --registry https://api.bintray.com/npm/unity/unity-npm',
            f'pip install unity-downloader-cli --extra-index-url https://artifactory.internal.unity3d.com/api/pypi/common-python/simple --upgrade',
            f'unity-downloader-cli --source-file unity_revision.txt -c editor --wait --published-only'
        ],
        'artifacts':{
            'packages':{
                'paths': [
                    dss("**/upm-ci~/test-results/**/*")
                ]
            }
        }
    }

    [job["commands"].append(dep) for dep in package["dependencies"]]

    if package.get('hascodependencies', None) is not None:
        job["commands"].append(package["copycmd"])
    else:
        job["commands"].append(f'upm-ci package test -u {platform["editorpath"]} --type updated-dependencies-tests --package-path {package["packagename"]}')

    return job

def all_package_ci(editor, platforms, packages):
    
    dependencies = []
    for platform in platforms:
        for package in packages:
            dependencies.append(f'.yamato/upm-ci-packages.yml#test_{package["id"]}_{ platform["name"]}_{editor["version"]}')
            dependencies.append(f'.yamato/upm-ci-packages.yml#test_{package["id"]}_{ platform["name"]}_{editor["version"]}_dependencies')
    
    job = {
        'name': f'Pack and test all packages - { editor["version"] }',
        'agent': agent_win,
        'dependencies': dependencies,
        'commands': [
            f'npm install upm-ci-utils@stable -g --registry https://api.bintray.com/npm/unity/unity-npm',
            f'upm-ci package izon -t',
            f'upm-ci package izon -d'
        ]
    }
    return job

def publish(package, platforms):

    dependencies = [f'.yamato/upm-ci-packages.yml#pack_{ package["id"]}']
    for platform in platforms:
        dependencies.append(f'.yamato/upm-ci-packages.yml#test_{package["id"]}_{ platform["name"]}_trunk')

    job = {
        'name': f'z_(do not use) Publish { package["name"]}',
        'agent': agent_win,
        'dependencies': dependencies,
        'commands': [
            f'npm install upm-ci-utils@stable -g --registry https://api.bintray.com/npm/unity/unity-npm',
            f'upm-ci package publish --package-path {package["packagename"]}'
        ],
        'artifacts':{
            'packages':{
                'paths':[
                    dss("upm-ci~/packages/*.tgz")
                ]
            }
        }
    }
    return job

def publish_all(packages):
    job = {
        'name': f'Publish all packages',
        'agent': agent_ubuntu,
        'dependencies': [f'.yamato/upm-ci-packages.yml#publish_{package["id"]}' for package in packages],
        'commands': [
            f'git tag v$(cd com.unity.render-pipelines.core && node -e "console.log(require(\'./package.json\').version)")',
            f'git push origin --tags'
        ]
    }
    return job