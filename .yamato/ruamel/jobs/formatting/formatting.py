from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ruamel.yaml.scalarstring import PreservedScalarString as pss
from ..shared.namer import *
from ..shared.constants import PATH_UNITY_REVISION, NPM_UPMCI_INSTALL_URL, UNITY_DOWNLOADER_CLI_URL, PATH_PACKAGES_temp, get_editor_revision,get_unity_downloader_cli_cmd
from ..shared.yml_job import YMLJob

class Formatting_Job():
    def __init__(self, metafile):
        self.job_id = formatting_job_id()
        self.yml = self.get_job_definition(metafile).get_yml()


    def get_job_definition(self, metafile):
        # define commands
        commands = [
            f'echo -e "[extensions]\nlargefiles=\n" > ~/.hgrc',
            f'hg clone -u stable http://hg-mirror-slo.hq.unity3d.com/unity-extra/unity-meta ~/unity-meta',
            f'perl ~/unity-meta/Tools/Format/format.pl --hgroot $(pwd) --dry-run .'
        ]

        # construct job
        job = YMLJob()
        job.set_name(f'Formatting')
        job.set_agent(metafile['formatting_agent'])
        job.add_commands(commands)
        job.set_timeout(metafile['timeout'])
        return job
