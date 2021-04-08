# Taken from https://github.com/Unity-Technologies/dots/blob/master/Tools/CI/editor_pinning/update_revisions.py

"""Updates editor versions by calling unity-downloader-cli."""
import argparse
import logging
import os
import re
import subprocess
import sys
import datetime
from ruamel.yaml import YAML

from util.subprocess_helpers import run_cmd, git_cmd

# EDITOR REVISION UPDATE SCRIPT
# Updates .yamato/_latest_editor_versions_[TRACK].metafile for each editor specified in _editor.metafile editor_tracks.
#
# Run locally (updates the file, but does not add/commit/push to git):
# python .\.yamato\script\editor_scripts\update_revisions.py
#
# Run in CI (updates the file, and adds/commits/pushes it to current branch):
# python .\.yamato\script\editor_scripts\update_revisions.py --commit-and-push



# These are by convention how the different revisions are categorized.
# These should not be changed unless also updated in Yamato YAML.
SUPPORTED_VERSION_TYPES = ('latest_internal', 'latest_public', 'staging')
UPDATED_AT = str(datetime.datetime.utcnow())

SCRIPT_DIR = os.path.abspath(os.path.dirname(__file__))
# DEFAULT_CONFIG_FILE = os.path.join(SCRIPT_DIR, 'config.yml')
DEFAULT_CONFIG_FILE = os.path.join(os.path.abspath(git_cmd('rev-parse --show-toplevel', cwd='.').strip()),'.yamato','config','_editor.metafile')
DEFAULT_SHARED_FILE = os.path.join(os.path.abspath(git_cmd('rev-parse --show-toplevel', cwd='.').strip()),'.yamato','config','__shared.metafile')

INVALID_VERSION_ERROR = 'Are you sure this is actually a valid unity version?'
VERSION_PARSER_RE = re.compile(r'Grabbing unity release ([0-9\.a-z]+) which is revision')

yaml = YAML()

def generate_downloader_cmd(track, version, trunk_track, platform):
    """Generate a list of commmand arguments for the invovation of the unity-downloader-cli."""
    if version == 'staging':
        # --fast avoids problems with ongoing builds. If we hit such it will
        # return an older build instead, which is fine for us for this tool.
        if track == trunk_track or track == 'trunk':
            target_str = '-u trunk --fast'
        else:
            target_str = f'-u {track}/staging --fast'
    elif version == 'latest_public':
        target_str = f'-u {track} --published-only'
    elif version == 'latest_internal':
        target_str = f'-u {track}'
    else:
        raise ValueError(f'Could not parse track: {track} version: {version}.')
    components = ' '.join('-c ' + c for c in platform["components"])

    return (f'unity-downloader-cli -o {platform["os"]} {components} -s {target_str} --wait --skip-download').split()

def create_version_files(config, root):

    editor_version_files =[]
    editor_versions_filename = config['editor_versions_file']
    for track in config['editor_tracks']:

        editor_versions_filename_track = editor_versions_filename.replace('TRACK',str(track))
        editor_versions_file = load_yml(editor_versions_filename_track)
        versions = get_versions_from_unity_downloader([track], config['trunk_track'], config['platforms'], editor_versions_file)

        print(f'INFO: Saving {editor_versions_filename_track}.')
        write_versions_file(os.path.join(root, editor_versions_filename_track), config['versions_file_header'], versions)

        if versions_file_is_unchanged(editor_versions_filename_track, root):
                print(f'INFO: No changes in {editor_versions_filename_track}, or file is not tracked by git diff')
        else:
            editor_version_files.append(editor_versions_filename_track)
    return editor_version_files


def get_versions_from_unity_downloader(tracks, trunk_track, platforms, editor_versions_file):
    """Gets the latest versions for each supported editor track using unity-downloader-cli.
    Args:
        tracks: Tuple of editor tracks, i.e. 2020.1, 2020.2
        trunk_track: String indicating which track is currently trunk.
        platforms: Dict containing keys for plaforms mapping to list of
            components.
    Returns: A dict of version keys where each points to a dict containing three
        key-value pairs (one per version_type) containing versions and revisions per each platform, e.g
        {
            2020.2_latest_internal:
                android:
                    revision: 3e0d5f775006
                    version: 2020.2.0a21
                ios:
                    revision: 3e0d5f775006
                    version: 2020.2.0a21
                linux:
                    revision: 3e0d5f775006
                    version: 2020.2.0a21
                macos:
                    revision: 3e0d5f775006
                    version: 2020.2.0a21
                windows:
                    revision: 3e0d5f775006
                    version: 2020.2.0a21
        }
    """

    # load existing latest_editor_versions
    versions = editor_versions_file.get("editor_versions", {})

    # drop all the keys that don't correspond to specified tracks (useful when different tracks are used between branches)
    false_keys = [key for key in versions if not key.startswith(tuple([str(t).replace('.','_') for t in tracks]))]
    for key in false_keys: del versions[key]

    for track in tracks: # pylint: disable=too-many-nested-blocks
        for version_type in SUPPORTED_VERSION_TYPES:

            key = f'{str(track).replace(".","_")}_{version_type}'

            if not versions.get(key):
                versions[key] = {}

            for platform in platforms:
                try:
                    platform_name = platform["name"]
                    timeout = 120
                    result = subprocess.check_output(generate_downloader_cmd(track, version_type, trunk_track, platform),
                                                    stderr=subprocess.STDOUT, universal_newlines=True, timeout=timeout, cwd='.')

                    revision = result.strip().splitlines()[-1]
                    if not versions.get(key).get(platform["name"]):
                        versions[key][platform_name] = {}

                    if not versions.get(key).get(platform_name).get('revision') or versions[key][platform_name]['revision'] != revision:
                        versions[key][platform_name]['updated_at'] = UPDATED_AT
                        versions[key][platform_name]['revision'] = revision

                        # Parse for the version in stderr (only exists for some cases):
                        versions[key][platform_name]['version'] = ''
                        for line in result.strip().splitlines():
                            match = VERSION_PARSER_RE.match(line)
                            version = ''
                            if match:
                                version = match.group(1)
                                versions[key][platform_name]['version'] = version
                                break
                        print(f'INFO: Latest revision for {key} [{platform_name}]: {revision} (version: {version})')
                    else:
                        print(f'INFO: Latest revision for {key} [{platform_name}] matches existing one: {revision}')
                except subprocess.TimeoutExpired as err:
                    print(f'WARNING: {key} [{platform_name}]: Timout {timeout}s exceeded')

                except subprocess.CalledProcessError as err:
                    # Not great error handling but will hold until there's a better way.
                    if err.stderr and INVALID_VERSION_ERROR in err.stderr:
                        print(
                            f'WARNING: {key} [{platform_name}]: '
                            f'unity-downloader-cli did not find a version for track: {track} '
                            f'and version: {version}. This is expected in some cases, e.g. alphas '
                            'that are not public yet.')
                    else:
                        print(
                            f'ERROR: {key} [{platform_name}] Revision will not be updated (keeping the previously existing one):\n'
                            f'Failed to run {err.cmd} \nStdout:\n{err.stdout}\nStderr:\n{err.stderr} ')

    return versions


def versions_file_is_unchanged(file_path, root):
    diff = git_cmd(f'diff {file_path}', cwd=root).strip()
    return len(diff) == 0


def write_versions_file(file_path, header, versions):
    with open(file_path, 'w') as output_file:

        # Write editor_version_names list:
        editor_version_names = {'editor_version_names': list(versions.keys())}
        yaml.dump(editor_version_names, output_file)

        # Write editor_versions dict:
        editor_version = {'editor_versions': versions}
        yaml.dump(editor_version, output_file)

def load_yml(filename):
    with open(filename) as yaml_file:
        return yaml.load(yaml_file)


def parse_args(flags):
    parser = argparse.ArgumentParser()
    parser.add_argument('--commit-and-push', action='store_true', help='Commits the changed revisions to current branch.')
    args = parser.parse_args(flags)
    return args


def main(argv):
    args = parse_args(argv)
    config = load_yml(DEFAULT_CONFIG_FILE)
    ROOT = os.path.abspath(git_cmd('rev-parse --show-toplevel', cwd='.').strip())

    print(f'INFO: Updating editor revisions to the latest found using unity-downloader-cli')
    print(f'INFO: Configuration file: {DEFAULT_CONFIG_FILE}')
    print(f'INFO: Running in {os.path.abspath(os.curdir)}')

    try:

        current_branch = git_cmd("rev-parse --abbrev-ref HEAD").strip()
        print(f'INFO: Running on branch: {current_branch}')

        editor_version_files = create_version_files(config, ROOT)
        if args.commit_and_push and len(editor_version_files) > 0:
            print(f'INFO: Committing and pushing to branch.')
            git_cmd(['add','.'], cwd=ROOT)
            git_cmd(['commit', '-m', f'[CI] Updated pinned editor versions'], cwd=ROOT)
            git_cmd(['pull'], cwd=ROOT)
            git_cmd(['push'], cwd=ROOT)
        else:
            print(f'INFO: Will not commit or push to current branch. Use --commit-and-push to do so.')

        print(f'INFO: Done updating editor versions.')
        return 0

    except subprocess.CalledProcessError as err:
        print(f"ERROR: Failed to run '{err.cmd}'\nStdout:\n{err.stdout}\nStderr:\n{err.stderr}")
        return 1


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
