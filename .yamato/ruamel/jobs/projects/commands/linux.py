from ...shared.constants import TEST_PROJECTS_DIR,PATH_UNITY_REVISION, PATH_TEST_RESULTS, UNITY_DOWNLOADER_CLI_URL, UTR_INSTALL_URL,get_unity_downloader_cli_cmd, get_timeout
from ...shared.utr_utils import get_repeated_utr_calls


def _cmd_base(project_folder, platform, utr_calls, editor):
    base = [ 
        f'sudo -H pip install --upgrade pip',
        f'sudo -H pip install unity-downloader-cli --index-url {UNITY_DOWNLOADER_CLI_URL} --upgrade',
        f'curl -s {UTR_INSTALL_URL} --output {TEST_PROJECTS_DIR}/{project_folder}/utr',
        f'chmod +x {TEST_PROJECTS_DIR}/{project_folder}/utr',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && sudo unity-downloader-cli { get_unity_downloader_cli_cmd(editor, platform["os"], cd=True) } {"".join([f"-c {c} " for c in platform["components"]])} --wait --published-only',
    ]
    for utr_args in utr_calls:
        base.append(f'cd {TEST_PROJECTS_DIR}/{project_folder} && DISPLAY=:0.0 ./utr {" ".join(utr_args)}')
    return base


def cmd_editmode(project_folder, platform, api, test_platform, editor, build_config, color_space):
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project_folder)
    base = _cmd_base(project_folder, platform, utr_calls, editor)

    if project_folder.lower() == "BoatAttack".lower():
        base = extra_perf_cmd(project_folder) + install_unity_config(project_folder) + base

    return base


def cmd_playmode(project_folder, platform, api, test_platform, editor, build_config, color_space):
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project_folder)
    base = _cmd_base(project_folder, platform, utr_calls, editor)

    if project_folder.lower() == "BoatAttack".lower():
        base = extra_perf_cmd(project_folder) + install_unity_config(project_folder) + base

    return base


def cmd_standalone(project_folder, platform, api, test_platform, editor, build_config, color_space):

    base = [
        f'curl -s {UTR_INSTALL_URL} --output {TEST_PROJECTS_DIR}/{project_folder}/utr',
        f'chmod +x {TEST_PROJECTS_DIR}/{project_folder}/utr'        ]
    
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project_folder)
    for utr_args in utr_calls:
        base.append(f'cd {TEST_PROJECTS_DIR}/{project_folder} && ./utr {" ".join(utr_args)}')
    return base


def cmd_standalone_build(project_folder, platform, api, test_platform, editor, build_config, color_space):
    utr_calls = get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project_folder, utr_flags_key="utr_flags_build")

    base = _cmd_base(project_folder, platform, utr_calls, editor)

    if project_folder.lower() == "BoatAttack".lower():
        base = extra_perf_cmd(project_folder) + install_unity_config(project_folder) + base

    return base


def extra_perf_cmd(project_folder):   
    perf_list = [
        f'git clone https://github.com/Unity-Technologies/BoatAttack.git -b $BOAT_ATTACK_BRANCH TestProjects/{project_folder}',
        f'cd TestProjects/{project_folder} && git checkout $BOAT_ATTACK_REVISION'
        ]
    return perf_list

def install_unity_config(project_folder):
    cmds = [
        f'curl -L https://artifactory.prd.it.unity3d.com/artifactory/api/gpg/key/public | sudo apt-key add -',
        f'sudo sh -c "echo \'deb https://artifactory.prd.it.unity3d.com/artifactory/unity-apt-local bionic main\' > /etc/apt/sources.list.d/unity.list"',
        f'sudo apt update',
        f'sudo apt install -y unity-config',


        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency "com.unity.render-pipelines.core@file:../../../com.unity.render-pipelines.core" --project-path .',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency "com.unity.render-pipelines.universal@file:../../../com.unity.render-pipelines.universal" --project-path .',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency "com.unity.shadergraph@file:../../../com.unity.shadergraph" --project-path .',


		#f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project remove dependency com.unity.render-pipelines.universal',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency com.unity.addressables@1.16.7 --project-path .',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency com.unity.scriptablebuildpipeline@1.11.2 --project-path .',
		f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency com.unity.test-framework@1.1.18 --project-path .',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency com.unity.test-framework.performance@2.4.0 --project-path .',
		f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency com.unity.test-framework.utp-reporter@1.0.2-preview --project-path .',
		f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency com.unity.test-framework.build@0.0.1-preview.12 --project-path .',
              
		f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency \"com.unity.testing.graphics-performance@ssh://git@github.cds.internal.unity3d.com/unity/com.unity.testing.graphics-performance.git\"  --project-path .',        
		f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add dependency \"unity.graphictests.performance.universal@ssh://git@github.cds.internal.unity3d.com/unity/unity.graphictests.performance.universal.git\" --project-path .',	
		
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add testable com.unity.cli-project-setup  --project-path .',		
		f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add testable com.unity.test.performance.runtimesettings  --project-path .',
		f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add testable com.unity.test.metadata-manager  --project-path .',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add testable com.unity.testing.graphics-performance --project-path .',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add testable com.unity.render-pipelines.core  --project-path .',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project add testable unity.graphictests.performance.universal  --project-path .',
        f'cd {TEST_PROJECTS_DIR}/{project_folder} && unity-config project set project-update false --project-path .'
    ]
    return cmds
