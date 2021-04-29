import argparse
import os
import subprocess
import sys
import requests
import datetime
import ruamel.yaml
import json
from util.subprocess_helpers import git_cmd, run_cmd
from util.utils import load_yml, ordereddict_to_dict

yaml = ruamel.yaml.YAML()
UPDATED_AT = str(datetime.datetime.utcnow())
DEFAULT_CONFIG_FILE = os.path.join(os.path.abspath(git_cmd('rev-parse --show-toplevel', cwd='.').strip()),'.yamato','config','_editor.metafile')
DEFAULT_SHARED_FILE = os.path.join(os.path.abspath(git_cmd('rev-parse --show-toplevel', cwd='.').strip()),'.yamato','config','__shared.metafile')

# Fetches all changesets from Ono up to the one already present in the  _latest_editor_versions_{track}.metafile, and updates _latest_editor_versions_{track}.metafile
# See args in parse_args()
#
#
# Run locally without any git commands (just updates the file locally to most recent retrieved revisions)
# python .\script\editor_scripts\update_revisions_ono.py --track {track} --ono-branch {branch name} --api-key {apikey}
#
# Run in CI (or locally) to make a commit for each retrieved revision (up to the already stored one), from older to newer
# python .\script\editor_scripts\update_revisions_ono.py --track {track} --ono-branch {branch name} --api-key {apikey} --commit-and-push


def parse_args(flags):
    parser = argparse.ArgumentParser()
    parser.add_argument('--commit-and-push', action='store_true',
                        help='If specified: commit/push the each revision separately to the current branch. If not specified: only update the file locally to the most recent revision (no git involved')
    parser.add_argument("--track", required=True, help='Which editor track the script is targeting')
    parser.add_argument("--ono-branch", required=True, help='Which Ono branch to target via API')
    parser.add_argument("--api-key", required=True, help='Ono API key')
    args = parser.parse_args(flags)
    return args


def get_last_revisions_from_ono(api_key, last_retrieved_revision, ono_branch):
    """Calls Ono Api to get n latest revisions."""
    try:
        headers = {
            "Authorization":f"Bearer {api_key}",
            "Content-Type": "application/json"
            }
        ono_endpoint = 'https://ono.unity3d.com/_admin/graphql'
        ono_query = '''
        {
            repository(name: "unity/unity") {
                changelog(branch: "ONO_BRANCH", limit: 100) {
                        nodes{
                            id
                            date
                        }
                    }
            }
        }'''
        ono_query = ono_query.replace('ONO_BRANCH', ono_branch)
        response = requests.get(url=f'{ono_endpoint}?query={ono_query}', headers=headers)
        if response.status_code != 200:
            raise Exception(f'!! ERROR: Got {response.status_code}')

        ono_revisions = []
        for ono_revision_node in response.json()["data"]["repository"]["changelog"]["nodes"]:
            # stop when reached the last handled revision
            if last_retrieved_revision != None and ono_revision_node["id"] == last_retrieved_revision:
                break
            ono_revisions.append(ono_revision_node)
        ono_revisions.reverse() # reverse it to go from older to newer
        print('Got revisions (from older to newer): \n', json.dumps(ono_revisions, indent=2))
        return ono_revisions
    except:
        print(f"!! ERROR: Failed to call Ono. Got {response.json()}")
        return None

def update_revision_file(editor_versions_file_path, revision_node, track_key, ono_branch):
    editor_versions_file = load_yml(editor_versions_file_path)
    if not editor_versions_file.get(track_key):
        editor_versions_file[track_key] = {}
    editor_versions_file[track_key]["updated_at_UTC"] = UPDATED_AT
    editor_versions_file[track_key]["changeset"] = ordereddict_to_dict(revision_node)
    with open(editor_versions_file_path, 'w') as f:
            yaml.dump(editor_versions_file, f)




def main(argv):
    # initialize files etc.
    args = parse_args(argv)
    root = os.path.abspath(git_cmd('rev-parse --show-toplevel', cwd='.').strip())
    config = load_yml(DEFAULT_CONFIG_FILE)
    shared = load_yml(DEFAULT_SHARED_FILE)
    editor_versions_file_path = os.path.join(root, config['editor_versions_file'].replace('TRACK',str(args.track)))
    if not os.path.exists(editor_versions_file_path):
        open(editor_versions_file_path, 'a').close()

    track_key = str(args.track).replace('.','_')
    current_branch = git_cmd("rev-parse --abbrev-ref HEAD").strip()

    # fetch last revision in the file, or None if revision missing
    editor_versions_file = load_yml(editor_versions_file_path)
    if editor_versions_file.get(track_key):
        last_retrieved_revision = editor_versions_file[track_key]["changeset"]["id"] # TODO parse last revision
    else:
        last_retrieved_revision = None

    try:
        # fetch all ono revisions up until the last revision in the file
        last_revisions_nodes = get_last_revisions_from_ono(args.api_key, last_retrieved_revision, args.ono_branch)
        if len(last_revisions_nodes) == 0:
            print(f'INFO: No revisions to update.')
            return 0

        if args.commit_and_push:
            print(f'INFO: Pulling branch: {current_branch}).')
            git_cmd(f'checkout {current_branch}', root)
            git_cmd(f'pull', root)

            print(f'INFO: Committing and pushing each revision ({len(last_revisions_nodes)}).')
            for revision_node in last_revisions_nodes:
                update_revision_file(editor_versions_file_path, revision_node, track_key, args.ono_branch)
                git_cmd(['add','.'], cwd=root)
                git_cmd(['commit', '-m', f'[CI] [{args.track}] Updated editor to {revision_node["id"]}'], cwd=root)
            git_cmd(['pull'], cwd=root)
            git_cmd(['push'], cwd=root)
        else:
            print(f'INFO: Updating the file to most recent revision, but will not git add/commit/push. Use --commit-and-push to do so.')
            update_revision_file(editor_versions_file_path, last_revisions_nodes[-1], track_key, args.ono_branch)

        print(f'INFO: Done updating editor versions.')
        return 0

    except subprocess.CalledProcessError as err:
        print(f"ERROR: Failed to run '{err.cmd}'\nStdout:\n{err.stdout}\nStderr:\n{err.stderr}")
        return 1


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
