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


def get_editor_revision(editor, platform_os):
    return VAR_CUSTOM_REVISION if str(editor['track']).lower()=='custom-revision' else editor["revisions"][f"{editor['track']}_latest_internal"][platform_os]["revision"]

def get_unity_downloader_cli_cmd(editor, platform_os, cd=False):
    if editor["track"].lower() == 'custom-revision':
        if cd:
            return f'--source-file ../../{PATH_UNITY_REVISION}'
        else:
            return PATH_UNITY_REVISION
    else:
        return f'-u {get_editor_revision(editor, platform_os)}'