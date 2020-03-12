def _cmd_base(project, components):
    return [ 
        f'sudo -H pip install --upgrade pip',
        f'sudo -H pip install unity-downloader-cli --extra-index-url https://artifactory.internal.unity3d.com/api/pypi/common-python/simple',
        f'sudo npm install upm-ci-utils -g --registry https://api.bintray.com/npm/unity/unity-npm',
        f'git clone git@github.cds.internal.unity3d.com:unity/utr.git TestProjects/{project["folder"]}/utr',
        f'cd TestProjects/{project["folder"]} && sudo unity-downloader-cli --source-file ../../unity_revision.txt {"".join([f"-c {c} " for c in components])} --wait --published-only'
    ]

def cmd_test(project, platform, test_platform, api):
    base = _cmd_base(project, platform["components"])
    base.extend([ 
        f'cd TestProjects/{project["folder"]} && DISPLAY=:0.0 utr/utr --extra-editor-arg="{api["cmd"]}"  {test_platform["args"]} --testproject=. --editor-location=.Editor --artifacts_path=test-results'
     ])
    return base

def cmd_test_standalone(project, platform, test_platform, api):
    base = _cmd_base(project, platform["components"])
    base.extend([
        f'cd TestProjects/{project["folder"]} && DISPLAY=:0.0 utr/utr {test_platform["args"]}Linux64 --extra-editor-arg="-executemethod" --extra-editor-arg="CustomBuild.BuildLinux{api["name"]}Linear" --testproject=. --editor-location=.Editor --artifacts_path=test-results'
      ])
    return base

def cmd_build(project, platform, test_platform, api):
    raise Exception('linux: standalone_split set to true but build commands not specified')

