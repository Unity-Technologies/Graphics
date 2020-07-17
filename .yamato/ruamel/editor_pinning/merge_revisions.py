# Taken from https://github.com/Unity-Technologies/dots/blob/master/Tools/CI/editor_pinning/merge_revisions_file.py

"""Merges the latest editor version file to a target branch."""
import argparse
import logging
import os
import subprocess
import sys

from update_revisions import load_config, DEFAULT_CONFIG_FILE, EXPECTATIONS_PATH
from util.subprocess_helpers import git_cmd, run_cmd


def verify_changed_files(editor_versions_file, commit_hash, working_dir):
    cmd = ['show', '--pretty=format:', '--name-only', commit_hash]
    filenames = git_cmd(cmd, working_dir).strip().replace('\\', '/').split()

    assert editor_versions_file in filenames, f'Cannot find {editor_versions_file} in {filenames}'
    filenames.remove(editor_versions_file)
    assert all(filename.startswith(EXPECTATIONS_PATH) or filename.endswith('.yml') for filename in filenames), (
        f'Found other files than {editor_versions_file}, .yml, and expectation files in {filenames}')


def checkout_and_pull_branch(branch, working_dir):
    git_cmd(f'checkout {branch}', working_dir)
    git_cmd('pull --ff-only', working_dir)


def apply_target_revision_changes(editor_versions_file, yml_files_path, commit, working_dir):
    """Apply the changes for the .metafile only (since expectations might have conflicts)
        Returns: True if any changes were applied, False otherwise.
    """
    diff = git_cmd(f'diff HEAD..{commit} -- {editor_versions_file}')
    if len(diff.strip()) > 0:
        diff_filename = 'diff.patch'
        with open(diff_filename, 'w') as diff_file:
            diff_file.write(diff)
        git_cmd(f'apply {diff_filename}')
        os.remove(diff_filename)
        git_cmd(f'add {editor_versions_file}', working_dir)
        
        
        diff_yml = git_cmd(f'diff HEAD..{commit} -- {yml_files_path}')
        with open(diff_filename, 'w') as diff_file:
            diff_file.write(diff_yml)
        git_cmd(f'apply {diff_filename}')
        os.remove(diff_filename)
        git_cmd(f'add {yml_files_path}', working_dir)
        return True
    return False

def regenerate_expectations(yamato_parser, working_dir):
    logging.info(f'Running {yamato_parser} to generate unfolded Yamato YAML...')
    run_cmd(yamato_parser, cwd=working_dir)
    git_cmd(f'add {EXPECTATIONS_PATH}', working_dir)


def get_commit_message(git_hash):
    return git_cmd(f'log --format=%B -n 1 {git_hash}')


def commit_and_push(commit_msg, working_dir, development_mode=False):
    git_cmd(['commit', '-m', commit_msg], working_dir)
    if not development_mode:
        git_cmd('pull --ff-only', working_dir)
        git_cmd('push', working_dir)


def parse_args(flags):
    parser = argparse.ArgumentParser()
    parser.add_argument('--local', action='store_true',
                    help='For local development (doesn\'t switch branches, pull or push)')
    parser.add_argument('--config', required=False, default=DEFAULT_CONFIG_FILE,
                        help=f'Configuration YAML file to use. Default: {DEFAULT_CONFIG_FILE}')
    parser.add_argument("--revision", required=True)
    parser.add_argument("--working-dir", required=False,
                        help='Working directory (optional). If omitted the root '
                        'of the repo will be used.')
    parser.add_argument("--target-branch", required=True,
                        help='The Git branch to merge the changes in the file into.')
    parser.add_argument('--yamato-parser', required=False,
                        help='The yamato-parser executable to use')
    args = parser.parse_args(flags)
    if not os.path.isfile(args.config):
        parser.error(f'Cannot find config file {args.config}')
    return args


def main(argv):
    logging.basicConfig(level=logging.INFO, format='[%(levelname)s] %(message)s')
    args = parse_args(argv)
    config = load_config(args.config)
    editor_versions_file = config['editor_versions_file']

    try:
        working_dir = args.working_dir or os.path.abspath(
            git_cmd('rev-parse --show-toplevel', cwd='.').strip())
        if args.local:
            logging.warning('\n\n!! DEVELOPMENT MODE: will not switch branch, pull or push !!\n')
        logging.info(f'Working directory: {working_dir}')
        verify_changed_files(editor_versions_file, args.revision, working_dir)
        if not args.local:
            checkout_and_pull_branch(args.target_branch, working_dir)
            if git_cmd('rev-parse HEAD').strip() == args.revision:
                logging.info('No changes compared to current revision. Exiting...')
                return 0
        if apply_target_revision_changes(editor_versions_file, config['yml_files_path'], args.revision, working_dir):
            if args.yamato_parser:
                regenerate_expectations(args.yamato_parser, working_dir)
            commit_msg = get_commit_message(args.revision)
            commit_and_push(commit_msg, working_dir, args.local)
        else:
            logging.info('No revision changes to merge. Exiting successfully without any '
                         'commit/push.')
        return 0
    except subprocess.CalledProcessError as err:
        logging.error(f"Failed to run '{err.cmd}'\nStdout:\n{err.stdout}\nStderr:\n{err.stderr}")
        return 1


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))