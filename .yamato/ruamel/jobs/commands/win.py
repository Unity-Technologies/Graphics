
def _cmd_base(project, components):
    return [
        f'git clone git@github.cds.internal.unity3d.com:unity/utr.git TestProjects/{project["folder"]}/utr',
        f'pip install unity-downloader-cli --extra-index-url https://artifactory.internal.unity3d.com/api/pypi/common-python/simple --upgrade',
        f'cd TestProjects/{project["folder"]} && unity-downloader-cli --source-file ../../unity_revision.txt {"".join([f"-c {c} " for c in components])} --wait --published-only'
    ]


def cmd_not_standalone(project, platform, api, test_platform_args):
    base = _cmd_base(project, platform["components"])
    base.extend([
        f'cd TestProjects/{project["folder"]} && utr\\utr {test_platform_args} --extra-editor-arg="{api["cmd"]}" --testproject=. --editor-location=.Editor --artifacts_path=test-results'
    ])
    return base

def cmd_standalone(project, platform, api, test_platform_args):
    base = _cmd_base(project, platform["components"])
    base.extend([
        f'cd TestProjects/{project["folder"]} && utr/utr {test_platform_args}Windows64 --artifacts_path=test-results --timeout=1200 --player-load-path=../../players --player-connection-ip=auto'
    ])
    return base


def cmd_standalone_build(project, platform, api):
    base = _cmd_base(project, platform["components"])
    base.extend([
        f'cd TestProjects/{project["folder"]} && utr/utr --suite=playmode --platform=StandaloneWindows64 --extra-editor-arg="-executemethod" --extra-editor-arg="CustomBuild.BuildWindows{api["name"]}Linear" --testproject=. --editor-location=.Editor --artifacts_path=test-results --timeout=1200 --player-save-path=../../players --build-only'
    ])
    return base
