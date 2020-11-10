VAR_UPM_REGISTRY = 'https://artifactory-slo.bf.unity3d.com/artifactory/api/npm/upm-candidates'
TEST_PROJECTS_DIR = 'TestProjects'
REPOSITORY_NAME = 'Graphics'
PATH_TEST_RESULTS = 'test-results'
PATH_TEST_RESULTS_padded = '**/test-results/**'
PATH_PACKAGES = 'upm-ci~/packages/**/*'
PATH_PACKAGES_temp = 'packages_temp' # used in combination with packages_temp\\[packageid] followed by PATH_PACKAGES to have unique artifact paths
PATH_TEMPLATES = 'upm-ci~/templates/**/*'
PATH_UNITY_REVISION = 'unity_revision.txt'
PATH_PLAYERS_padded = 'players/**'
PATH_PLAYERS = 'players'
NPM_UPMCI_INSTALL_URL = 'https://artifactory.prd.cds.internal.unity3d.com/artifactory/api/npm/upm-npm'
UTR_INSTALL_URL = 'https://artifactory.internal.unity3d.com/core-automation/tools/utr-standalone/utr'
UNITY_DOWNLOADER_CLI_URL = 'https://artifactory.prd.it.unity3d.com/artifactory/api/pypi/pypi/simple'
VAR_CUSTOM_REVISION = '$CUSTOM_REVISION'
GITHUB_CDS_URL = 'https://github.cds.internal.unity3d.com'
DEFAULT_TIMEOUT = 1200



def get_editor_revision(editor, platform_os):
    if str(editor['track']).lower()=='custom-revision':
        return VAR_CUSTOM_REVISION
    elif str(editor['track']).lower()=='trunk':
        return editor["revisions"][f"{editor['track']}_latest_internal"][platform_os]["revision"]
    else:
        return editor["revisions"][f"{editor['track']}_staging"][platform_os]["revision"]

def get_unity_downloader_cli_cmd(editor, platform_os, cd=False, git_root=False):
    '''Returns the revision used by unity-downloader-cli. 
    For custom revision, refers to --source-file flag. If cd, then revision file path is prepended by ../../; if git_root, then its prepended by ~/Graphics/.
    For normal tracks (not custom revision), retrieves the editor revision from latest_editor_versions file'''
    if not editor["editor_pinning"]:
        if cd:
            return f'--source-file ../../{PATH_UNITY_REVISION}'
        elif git_root:
            return f'--source-file ~/Graphics/{PATH_UNITY_REVISION}'
        else:
            return f'--source-file {PATH_UNITY_REVISION}'
    else:
        return f'-u {get_editor_revision(editor, platform_os)}'

def get_timeout(test_platform, os_name, build=False):
    '''Returns default timeout if testplatform does not specify otherwise.
    If testplatform has timeout specified as single integer, then returns this for all possible os.
    If testplatform has timeout specified per os, then returns timeout value specified for this os 
    OR default timeout if os-specific not present.'''
    key = 'timeout' if not build else 'timeout_build'

    if not test_platform.get(key):
        return DEFAULT_TIMEOUT
    else:
        if type(test_platform[key]) == int:
            return test_platform[key]
        else:
            timeout_attr = dict(test_platform[key])
            return DEFAULT_TIMEOUT if not timeout_attr.get(os_name) else timeout_attr.get(os_name)
