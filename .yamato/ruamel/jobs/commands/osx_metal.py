
from ruamel.yaml.scalarstring import PreservedScalarString as pss

def _cmd_base(project, components):
    return [ 
        f'git clone git@github.cds.internal.unity3d.com:unity/utr.git TestProjects/{project["folder"]}/utr',
        f'ssh -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" bokken@$BOKKEN_DEVICE_IP "bash -lc \'pip3 install --user unity-downloader-cli --extra-index-url https://artifactory.internal.unity3d.com/api/pypi/common-python/simple\'"',
        f'scp -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" -r $YAMATO_SOURCE_DIR bokken@$BOKKEN_DEVICE_IP:~/ScriptableRenderPipeline',
        f'scp -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" ~/.ssh/id_rsa_macmini bokken@$BOKKEN_DEVICE_IP:~/.ssh/id_rsa_macmini',
        f'ssh -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" bokken@$BOKKEN_DEVICE_IP \'/Users/bokken/Library/Python/3.7/bin/unity-downloader-cli --source-file ~/ScriptableRenderPipeline/unity_revision.txt {"".join([f"-c {c} " for c in components])} --wait --published-only\''
    ]

def cmd_editmode(project, platform, api):
    base = _cmd_base(project, platform["components"])
    base.extend([ 
        pss(f'''
        ssh -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" bokken@$BOKKEN_DEVICE_IP 'cd ~/ScriptableRenderPipeline/TestProjects/{project["folder"]} && ~/ScriptableRenderPipeline/TestProjects/{project["folder"]}/utr/utr --suite=editor --platform=editmode --testproject=/Users/bokken/ScriptableRenderPipeline/TestProjects/{project["folder"]} --editor-location=/Users/bokken/.Editor --artifacts_path=/Users/bokken/ScriptableRenderPipeline/TestProjects/{project["folder"]}/test-results\'
        UTR_RESULT=$? 
        mkdir -p TestProjects/{project["folder"]}/test-results/
        scp -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" -r bokken@$BOKKEN_DEVICE_IP:/Users/bokken/ScriptableRenderPipeline/TestProjects/{project["folder"]}/test-results/ TestProjects/{project["folder"]}/test-results/
        exit $UTR_RESULT''')
     ])
    return base

def cmd_playmode(project, platform, api):
    base = _cmd_base(project, platform["components"])
    base.extend([ 
        pss(f'''
        ssh -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" bokken@$BOKKEN_DEVICE_IP 'cd ~/ScriptableRenderPipeline/TestProjects/{project["folder"]} && ~/ScriptableRenderPipeline/TestProjects/{project["folder"]}/utr/utr --suite=playmode --testproject=/Users/bokken/ScriptableRenderPipeline/TestProjects/{project["folder"]} --editor-location=/Users/bokken/.Editor --artifacts_path=/Users/bokken/ScriptableRenderPipeline/TestProjects/{project["folder"]}/test-results\'
        UTR_RESULT=$? 
        mkdir -p TestProjects/{project["folder"]}/test-results/
        scp -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" -r bokken@$BOKKEN_DEVICE_IP:/Users/bokken/ScriptableRenderPipeline/TestProjects/{project["folder"]}/test-results/ TestProjects/{project["folder"]}/test-results/
        exit $UTR_RESULT''')
     ])
    return base

def cmd_playmode_xr(project, platform, api):
    base = _cmd_base(project, platform["components"])
    base.extend([ 
        pss(f'''
        ssh -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" bokken@$BOKKEN_DEVICE_IP 'cd ~/ScriptableRenderPipeline/TestProjects/{project["folder"]} && ~/ScriptableRenderPipeline/TestProjects/{project["folder"]}/utr/utr --suite=playmode --extra-editor-arg="-xr-tests" --testproject=/Users/bokken/ScriptableRenderPipeline/TestProjects/{project["folder"]} --editor-location=/Users/bokken/.Editor --artifacts_path=/Users/bokken/ScriptableRenderPipeline/TestProjects/{project["folder"]}/test-results\'
        UTR_RESULT=$? 
        mkdir -p TestProjects/{project["folder"]}/test-results/
        scp -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" -r bokken@$BOKKEN_DEVICE_IP:/Users/bokken/ScriptableRenderPipeline/TestProjects/{project["folder"]}/test-results/ TestProjects/{project["folder"]}/test-results/
        exit $UTR_RESULT''')
     ])
    return base

def cmd_standalone(project, platform, api):
    base = _cmd_base(project, platform["components"])
    base.extend([ 
        pss(f'''
        ssh -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" bokken@$BOKKEN_DEVICE_IP \'cd ~/ScriptableRenderPipeline/TestProjects/{project["folder"]} && ~/ScriptableRenderPipeline/TestProjects/{project["folder"]}/utr/utr --suite=playmode --platform=StandaloneOSX  --testproject=/Users/bokken/ScriptableRenderPipeline/TestProjects/{project["folder"]} --editor-location=/Users/bokken/.Editor --artifacts_path=/Users/bokken/ScriptableRenderPipeline/TestProjects/{project["folder"]}/test-results --timeout=1400\'
        UTR_RESULT=$? 
        mkdir -p TestProjects/{project["folder"]}/test-results/
        scp -i ~/.ssh/id_rsa_macmini -o "StrictHostKeyChecking=no" -r bokken@$BOKKEN_DEVICE_IP:/Users/bokken/ScriptableRenderPipeline/TestProjects/{project["folder"]}/test-results/ TestProjects/{project["folder"]}/test-results/
        exit $UTR_RESULT''')
     ])
    return base

def cmd_standalone_build(project, platform, api):
    raise Exception('osx_metal: standalone_split set to true but build commands not specified')

