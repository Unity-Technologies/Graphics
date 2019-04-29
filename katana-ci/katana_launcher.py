import argparse
import json
import os
import sys

import requests
import katana_lib

args = None

#original code: https://gitlab.cds.internal.unity3d.com/burst/burst/tree/ci/run_katana_builds/Tools/CI

def main():
    properties = {
        "force_chain_rebuild": "true",
        "force_rebuild": "true",
        "priority": "50" }

    project = 'proj57-Test PlayMode - Mac (Intel)'
    build_number = katana_lib.start_katana_build(project, properties)

    while not katana_lib.has_katana_finished(build_number, project):
        pass

    # get test results
    while not katana_lib.process_running_builds(build_number, project):
        pass

if __name__ == '__main__':
    main()
