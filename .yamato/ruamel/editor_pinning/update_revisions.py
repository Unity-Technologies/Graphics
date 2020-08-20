# Taken from https://github.com/Unity-Technologies/dots/blob/master/Tools/CI/editor_pinning/update_revisions.py

"""Updates editor versions by calling unity-downloader-cli."""
import argparse
import logging
import os
import re
import subprocess
import sys
import yaml

from util.subprocess_helpers import run_cmd, git_cmd

# These are by convention how the different revisions are categorized.
# These should not be changed unless also updated in Yamato YAML.
SUPPORTED_VERSION_TYPES = ('latest_internal', 'latest_public', 'staging')
PROJECT_VERSION_NAME = 'project_revision'
PLATFORMS = ('windows', 'macos', 'linux', 'android', 'ios')

SCRIPT_DIR = os.path.abspath(os.path.dirname(__file__))
# DEFAULT_CONFIG_FILE = os.path.join(SCRIPT_DIR, 'config.yml')
DEFAULT_CONFIG_FILE = os.path.join(os.path.abspath(git_cmd('rev-parse --show-toplevel', cwd='.').strip()),'.yamato','config','_editor.metafile')
EXPECTATIONS_PATH = os.path.join('.yamato', 'expectations')

INVALID_VERSION_ERROR = 'Are you sure this is actually a valid unity version?'
VERSION_PARSER_RE = re.compile(r'Grabbing unity release ([0-9\.a-z]+) which is revision')


def generate_downloader_cmd(track, version, trunk_track, platform, unity_downloader_components):
    """Generate a list of commmand arguments for the invovation of the unity-downloader-cli."""
    assert platform in PLATFORMS, f'Unsupported platform: {platform}'
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
    components = ' '.join('-c ' + c for c in unity_downloader_components[platform])
    
    if platform.lower() == 'android':
        platform = 'windows'
    elif platform.lower() == 'ios':
        platform = 'macos'
    
    return (f'unity-downloader-cli -o {platform} {components} -s {target_str} '
            '--wait --skip-download').split()


def get_versions_from_unity_downloader(tracks, trunk_track, unity_downloader_components, editor_versions_file):
    """Gets the latest versions for each supported editor track using unity-downloader-cli.
    Args:
        tracks: Tuple of editor tracks, i.e. 2020.1, 2020.2
        trunk_track: String indicating which track is currently trunk.
        unity_downloader_components: Dict containing keys for plaforms mapping to list of
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
    false_keys = [key for key in versions if key.split('_')[0] not in tracks] 
    for key in false_keys: del versions[key] 

    for track in tracks: # pylint: disable=too-many-nested-blocks
        for version_type in SUPPORTED_VERSION_TYPES:

            key = f'{track}_{version_type}'

            if not versions.get(key):
                versions[key] = {}

            for platform in PLATFORMS:
                try:
                    
                    timeout = 120
                    result = subprocess.check_output(generate_downloader_cmd(track, version_type, trunk_track, platform,
                                                unity_downloader_components), stderr=subprocess.STDOUT, universal_newlines=True, timeout=timeout, cwd='.')
                    
                    revision = result.strip().splitlines()[-1]
                    versions[key][platform] = {}
                    versions[key][platform]['revision'] = revision
                   
                    # Parse for the version in stderr (only exists for some cases):
                    versions[key][platform]['version'] = ''
                    for line in result.strip().splitlines():
                        match = VERSION_PARSER_RE.match(line)
                        version = ''
                        if match:
                            version = match.group(1)
                            versions[key][platform]['version'] = version
                            break
                    print(f'INFO: Latest revision for {key} [{platform}]: {revision} (version: {version})')
                except subprocess.TimeoutExpired as err:
                    print(f'WARNING: {key} [{platform}]: Timout {timeout}s exceeded')

                except subprocess.CalledProcessError as err:
                    # Not great error handling but will hold until there's a better way.
                    if err.stderr and INVALID_VERSION_ERROR in err.stderr:
                        print(
                            f'WARNING: {key} [{platform}]: '
                            f'unity-downloader-cli did not find a version for track: {track} '
                            f'and version: {version}. This is expected in some cases, e.g. alphas '
                            'that are not public yet.')
                    else:
                        print(
                            f'ERROR: {key} [{platform}] Revision will not be updated (keeping the previously existing one):\n'
                            f'Failed to run '{err.cmd}'\nStdout:\n{err.stdout}\nStderr:\n{err.stderr} ')
    return versions


def get_current_branch():
    return git_cmd("rev-parse --abbrev-ref HEAD").strip()


def checkout_and_push(editor_versions_file, yml_files_path, target_branch, root, force_push,
                      commit_message_details):
    original_branch = get_current_branch()
    git_cmd(f'checkout -B {target_branch}', cwd=root)
    git_cmd(f'add {editor_versions_file}', cwd=root)
    git_cmd(f'add {yml_files_path}', cwd=root)

    # Expectations generated if yamato-parser is used:
    expectations_dir = os.path.join(root, EXPECTATIONS_PATH)
    if os.path.isdir(expectations_dir):
        git_cmd(f'add {expectations_dir}', cwd=root)

    cmd = ['commit', '-m',
           f'[Automation] Updated pinned editor versions used for CI\n\n{commit_message_details}']
    git_cmd(cmd, cwd=root)

    cmd = ['push', '--set-upstream', 'origin', target_branch]
    if force_push:
        assert not (target_branch in ('master', '9.x.x/release','8.x.x/release','7.x.x/release')), (
            'Error: not allowed to force push to {target_branch}.')
        cmd.append('--force')
    git_cmd(cmd, cwd=root)
    git_cmd(f'checkout {original_branch}', cwd=root)


def versions_file_is_unchanged(file_path, root):
    diff = git_cmd(f'diff {file_path}', cwd=root).strip()
    return len(diff) == 0


def write_versions_file(file_path, header, versions):
    with open(file_path, 'w') as output_file:
        output_file.write(header + os.linesep)

        # Write editor_version_names list:
        editor_version_names = {'editor_version_names': list(versions.keys())}
        yaml.dump(editor_version_names, output_file, indent=2)
        output_file.write(os.linesep)

        # Write editor_versions dict:
        # output_file.write(dict_comment + os.linesep)
        editor_version = {'editor_versions': versions}
        yaml.dump(editor_version, output_file, indent=2)

def load_latest_versions_metafile(filename):
    with open(filename) as yaml_file:
        return yaml.safe_load(yaml_file)
    

def load_config(filename):
    with open(filename) as yaml_file:
        config = yaml.safe_load(yaml_file)
    try:
        # Perform validation checks.
        assert 'editor_tracks' in config
        assert isinstance(config['editor_tracks'], list)
        assert 'trunk_track' in config
        assert isinstance(config['trunk_track'], str)
        assert 'editor_versions_file' in config
        assert isinstance(config['editor_versions_file'], str)

        assert 'unity_downloader_components' in config
        components = config['unity_downloader_components']
        assert isinstance(components, dict)
        assert 'windows' in components
        assert 'macos' in components

        assert 'versions_file_header' in config
        assert isinstance(config['versions_file_header'], str)
        return config
    except AssertionError:
        print('ERROR: Your configuration file {filename} has an error:')
        raise


def parse_args(flags):
    parser = argparse.ArgumentParser()
    parser.add_argument('--local', action='store_true',
                        help='Running locally skips sanity checks that should be applied on CI')
    parser.add_argument('--config', required=False, default=DEFAULT_CONFIG_FILE,
                        help=f'Configuration YAML file to use. Default: {DEFAULT_CONFIG_FILE}')
    parser.add_argument('--target-branch', required=True,
                        help='Branch to push the updated editor revisions to, should run full ' +
                        'CI on commit.')
    parser.add_argument('--yamato-parser', required=False,
                        help='The yamato-parser executable to use (if specified)')
    parser.add_argument('--force-push', action='store_true',
                        help='If --force flag should be used for `git push`.')
    parser.add_argument('-v', '--verbose', default=False, action='store_true', required=False,
                        help='Print verbose output for debugging purposes.')
    args = parser.parse_args(flags)
    if not os.path.isfile(args.config):
        parser.error(f'Cannot find config file {args.config}')
    return args


def main(argv):
    args = parse_args(argv)
    if args.verbose:
        logging.basicConfig(level=logging.DEBUG)
    else:
        logging.basicConfig(level=logging.INFO, format='[%(levelname)s] %(message)s')
    config = load_config(args.config)

    print(f'INFO: Updating editor revisions to the latest found using unity-downloader-cli')
    print(f'INFO: Configuration file: {args.config}')

    ROOT = os.path.abspath(git_cmd('rev-parse --show-toplevel', cwd='.').strip())

    print(f'INFO: Running in {os.path.abspath(os.curdir)}')
    # projectversion_filename = os.path.join(ROOT, config['project_version_file'])
    # assert os.path.isfile(projectversion_filename), f'Cannot find {projectversion_filename}'

    try:
        editor_versions_filename = config['editor_versions_file']
        editor_versions_file = load_latest_versions_metafile(editor_versions_filename)
        versions = get_versions_from_unity_downloader(config['editor_tracks'], config['trunk_track'], config['unity_downloader_components'], editor_versions_file)
        print(f'INFO: Saving {editor_versions_filename}.')
        write_versions_file(os.path.join(ROOT, editor_versions_filename),
                            config['versions_file_header'], 
                            versions)
        if versions_file_is_unchanged(editor_versions_filename, ROOT):
            print(f'INFO: No changes in the versions file, exiting')
        else:
            subprocess.call(['python', config['ruamel_build_file']])
            if args.yamato_parser:
                print(f'INFO: Running {args.yamato_parser} to generate unfolded Yamato YAML...')
                run_cmd(args.yamato_parser, cwd=ROOT)
            if not args.local:
                checkout_and_push(editor_versions_filename, config['yml_files_path'], args.target_branch, ROOT, args.force_push,
                                  'Updating pinned editor revisions')
        print(f'INFO: Done updating editor versions.')
        return 0
    except subprocess.CalledProcessError as err:
        print(f"ERROR: Failed to run '{err.cmd}'\nStdout:\n{err.stdout}\nStderr:\n{err.stderr}")
        return 1


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))