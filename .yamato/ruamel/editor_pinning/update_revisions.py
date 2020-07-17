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
PLATFORMS = ('windows', 'macos')

SCRIPT_DIR = os.path.abspath(os.path.dirname(__file__))
DEFAULT_CONFIG_FILE = os.path.join(SCRIPT_DIR, 'config.yml')
EXPECTATIONS_PATH = os.path.join('.yamato', 'expectations')

INVALID_VERSION_ERROR = 'Are you sure this is actually a valid unity version?'
VERSION_PARSER_RE = re.compile(r'Grabbing unity release ([0-9\.a-z]+) which is revision')

def parse_projectversion_file(projectversion_filename):
    """Parse ProjectSettings.txt and return the version and revision as a tuple."""
    REVISION_KEY = 'm_EditorVersionWithRevision'
    with open(projectversion_filename) as projectversion_file:
        for line in projectversion_file.readlines():
            line = line.strip()
            if REVISION_KEY in line:
                index = line.find('(')
                version = line[len(REVISION_KEY) + 2: index - 1]
                revision = line[index + 1: -1]
                return (version, revision)
    return None

def generate_downloader_cmd(track, version, trunk_track, platform, unity_downloader_components):
    """Generate a list of commmand arguments for the invovation of the unity-downloader-cli."""
    assert platform in PLATFORMS, f'Unsupported platform: {platform}'
    if version == 'staging':
        # --fast avoids problems with ongoing builds. If we hit such it will
        # return an older build instead, which is fine for us for this tool.
        if track == trunk_track:
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
    return (f'unity-downloader-cli -o {platform} {components} -s {target_str} '
            '--wait --skip-download').split()


def get_all_versions(tracks, trunk_track, unity_downloader_components, projectversion_filename):
    """Gets all versions from unity-downloader-cli and the ProjectVersion.txt"""
    versions = get_versions_from_unity_downloader(tracks, trunk_track, unity_downloader_components)

    # Add ProjectVersion.txt version and revision.
    version, revision = parse_projectversion_file(projectversion_filename)
    versions[PROJECT_VERSION_NAME] = {
        'display_name': PROJECT_VERSION_NAME.replace('_', ' '),
        'revision': revision,
        'version': version,
    }
    return versions


def get_versions_from_unity_downloader(tracks, trunk_track, unity_downloader_components):
    """Gets the latest versions for each supported editor track using unity-downloader-cli.
    Args:
        tracks: Tuple of editor tracks, i.e. 2020.1, 2020.2
        trunk_track: String indicating which track is currently trunk.
        unity_downloader_components: Dict containing keys for plaforms mapping to list of
            components.
    Returns: A dict of version keys where each points to a dict containing three
        key-value pairs, e.g.
        {
            "2020.1_latest_internal": {
                "display_name": "2020.1 latest internal",
                "version": "2020.1.0b8",
                "revision": "3f2ec5b3ee51"
            },
            "2020.1_latest_public": {
                "display_name": "2020.1 latest public",
                "version": "2020.1.0b7",
                "revision": "b3ee513f2ec5"
            },
            "2020.1_staging": {
                "display_name": "2020.1 staging",
                "version": "",
                "revision": "e513f2ec5b3e"
            }
        }
    """
    versions = {}
    for track in tracks: # pylint: disable=too-many-nested-blocks
        for platform in PLATFORMS:
            for version_type in SUPPORTED_VERSION_TYPES:
                try:
                    result = subprocess.run(
                        generate_downloader_cmd(track, version_type, trunk_track, platform,
                                                unity_downloader_components),
                        cwd='.', stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                        check=True, universal_newlines=True)
                    key = f'{track}_{version_type}'
                    revision = result.stdout.strip()
                    versions[key] = {
                        'display_name': key.replace('_', ' '),
                        'revision': revision,
                        'version': '',
                    }

                    # Parse for the version in stderr (only exists for some cases):
                    for line in result.stderr.strip().splitlines():
                        match = VERSION_PARSER_RE.match(line)
                        version = ''
                        if match:
                            version = match.group(1)
                            versions[key]['version'] = version
                            break
                    logging.info(f'[{platform}] Latest revision for track: {track} '
                                f'type: {version_type} is {revision} (version: {version})')

                except subprocess.CalledProcessError as err:
                    # Not great error handling but will hold until there's a better way.
                    if err.stderr and INVALID_VERSION_ERROR in err.stderr:
                        logging.warning(
                            f'unity-downloader-cli did not find a version for track: {track} '
                            f'and version: {version}. This is expected in some cases, e.g. alphas '
                            'that are not public yet.')
                    raise err
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
        assert not (target_branch in ('master', 'stable')), (
            'Error: not allowed to force push to {target_branch}.')
        cmd.append('--force')
    git_cmd(cmd, cwd=root)
    git_cmd(f'checkout {original_branch}', cwd=root)


def versions_file_is_unchanged(file_path, root):
    diff = git_cmd(f'diff {file_path}', cwd=root).strip()
    return len(diff) == 0


def write_versions_file(file_path, header, dict_comment, versions):
    with open(file_path, 'w') as output_file:
        output_file.write(header + os.linesep)

        # Write editor_version_names list:
        editor_version_names = {'editor_version_names': list(versions.keys())}
        yaml.dump(editor_version_names, output_file, indent=2)
        output_file.write(os.linesep)

        # Write editor_versions dict:
        output_file.write(dict_comment + os.linesep)
        editor_version = {'editor_versions': versions}
        yaml.dump(editor_version, output_file, indent=2)


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

        assert 'project_version_file' in config
        assert isinstance(config['project_version_file'], str)

        assert 'unity_downloader_components' in config
        components = config['unity_downloader_components']
        assert isinstance(components, dict)
        assert 'windows' in components
        assert 'macos' in components

        assert 'versions_file_header' in config
        assert isinstance(config['versions_file_header'], str)
        assert 'versions_dict_comment' in config
        assert isinstance(config['versions_dict_comment'], str)
        return config
    except AssertionError:
        logging.error('Your configuration file {filename} has an error:')
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

    logging.info('Updating editor revisions to the latest found using unity-downloader-cli')
    logging.info(f'Configuration file: {args.config}')

    ROOT = os.path.abspath(git_cmd('rev-parse --show-toplevel', cwd='.').strip())

    logging.info(f'Running in {os.path.abspath(os.curdir)}')
    projectversion_filename = os.path.join(ROOT, config['project_version_file'])
    assert os.path.isfile(projectversion_filename), f'Cannot find {projectversion_filename}'

    try:
        versions = get_all_versions(config['editor_tracks'], config['trunk_track'],
                                    config['unity_downloader_components'],
                                    projectversion_filename)
        editor_versions_file = config['editor_versions_file']
        logging.info(f'Saving {editor_versions_file}.')
        write_versions_file(os.path.join(ROOT, editor_versions_file),
                            config['versions_file_header'], config['versions_dict_comment'],
                            versions)
        if versions_file_is_unchanged(editor_versions_file, ROOT):
            logging.info('No changes in the versions file, exiting')
        else:
            subprocess.call(['python', config['ruamel_build_file']])
            if args.yamato_parser:
                logging.info(f'Running {args.yamato_parser} to generate unfolded Yamato YAML...')
                run_cmd(args.yamato_parser, cwd=ROOT)
            if not args.local:
                checkout_and_push(editor_versions_file, config['yml_files_path'], args.target_branch, ROOT, args.force_push,
                                  'Updating pinned editor revisions')
        logging.info('Done updating editor versions.')
        return 0
    except subprocess.CalledProcessError as err:
        logging.error(f"Failed to run '{err.cmd}'\nStdout:\n{err.stdout}\nStderr:\n{err.stderr}")
        return 1


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))