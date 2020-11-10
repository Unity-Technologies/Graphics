# Taken from https://github.com/Unity-Technologies/dots/blob/master/Tools/CI/editor_pinning/merge_revisions_file.py

"""Merges the latest editor version file to a target branch."""
import argparse
import logging
import os
import subprocess
import sys
import requests
import datetime
import ruamel.yaml
from collections import OrderedDict
from update_revisions import load_config, DEFAULT_CONFIG_FILE, EXPECTATIONS_PATH
from util.subprocess_helpers import git_cmd, run_cmd

yaml = ruamel.yaml.YAML()


def load_yml(filepath):
    with open(filepath) as f:
        return yaml.load(f)

def ordereddict_to_dict(d):
    return {k: ordereddict_to_dict(v) for k, v in d.items()} if isinstance(d, OrderedDict) else d


def checkout_and_pull_branch(branch, working_dir):
    git_cmd(f'checkout {branch}', working_dir)
    git_cmd('pull', working_dir)

def commit_and_push(commit_msg, working_dir, development_mode=False):
    if not development_mode:
        git_cmd(['commit', '-m', commit_msg], working_dir)
        git_cmd('pull', working_dir)
        git_cmd('push', working_dir)

def get_commit_message(git_hash):
    return git_cmd(f'log --format=%B -n 1 {git_hash}')


def verify_changed_files(editor_versions_file, commit_hash, working_dir):
    """Verifies that only either .ymls or editor version files have been changed"""
    cmd = ['show', '--pretty=format:', '--name-only', commit_hash]
    filenames = git_cmd(cmd, working_dir).strip().replace('\\', '/').split()

    assert editor_versions_file in filenames, f'Cannot find {editor_versions_file} in {filenames}'
    filenames.remove(editor_versions_file)
    assert all('_latest_editor_versions' in filename or filename.endswith('.yml') for filename in filenames), (
        f'Found other files than {editor_versions_file}, .yml, and expectation files in {filenames}')

def apply_target_revision_changes(editor_versions_file, yml_files_path, commit, working_dir):
    """Apply the changes for the .metafile only (since expectations might have conflicts)
        Returns: True if any changes were applied, False otherwise.
    """
    print(f'RUNNING: git diff HEAD..{commit} -- {editor_versions_file}')
    diff = git_cmd(f'diff HEAD..{commit} -- {editor_versions_file}')
    if len(diff.strip()) > 0:
        print('RUNNING: git apply diff.patch')
        diff_filename = 'diff.patch'
        with open(diff_filename, 'w') as f:
            f.write(diff)
        git_cmd(f'apply {diff_filename}')
        os.remove(diff_filename)
        git_cmd(f'add {editor_versions_file}', working_dir)
        return True
    return False

def get_yamato_dependency_tree(job_id, api_key):
    """Calls Yamato API (GET/jobid/tree)for given job id. Returns JSON dependency tree if success, and None if fails."""
    try:
        url = f'http://yamato-api.cds.internal.unity3d.com/jobs/{job_id}/tree'
        headers={"Authorization":f"ApiKey {api_key}"}
        response = requests.get(url=url, headers=headers)
        dependency_tree = response.json()
        if response.status_code != 200:
            raise Exception()
        return dependency_tree
    except:
        print(f"Failed to call Yamato API. Got {response.json()}")
        return None
    
def update_green_project_revisions(editor_versions_file, project_versions_file, track, projects, job_id, api_key, working_dir):
    """Updates green project revisions file for given track. If any updates present, adds to git and returns True. If not, returns False."""
    
    # get the revisions used for the job, the last green project revisions, and Yamato dependency tree  
    updated_at = str(datetime.datetime.utcnow())
    revisions_key = f"{track}_latest_internal" if track=="trunk" else f"{track}_staging"
    revisions = load_yml(editor_versions_file)["editor_versions"][revisions_key]
    last_green_projects = load_yml(project_versions_file)
    dependency_tree = get_yamato_dependency_tree(job_id, api_key)
    if not dependency_tree:
        return False
    
    # update revisions for each project
    is_updated = False 
    for project in projects:
        jobs = [node for node in dependency_tree["nodes"] if node["name"].lower()==f"all {project} ci - {track}"] 
        if len(jobs) == 0:
            continue

        job = jobs[0]
        if job["status"] == 'success': 
            print(f'Updating for {project}')
            if not last_green_projects.get(project):
                last_green_projects[project] = {}
            last_green_projects[project]["updated_at"] = updated_at
            last_green_projects[project]["last_green_revisions"] = revisions
            is_updated = True

    if is_updated: # at least one project got updated
        last_green_projects = ordereddict_to_dict(last_green_projects)
        with open(project_versions_file, 'w') as f:
            yaml.dump(last_green_projects, f)
        
        git_cmd(f'add {project_versions_file}', working_dir)
        return True
    
    return False


def parse_args(flags):
    parser = argparse.ArgumentParser()
    parser.add_argument('--local', action='store_true',
                    help='For local development (doesn\'t switch branches, pull or push)')
    parser.add_argument('--config', required=False, default=DEFAULT_CONFIG_FILE,
                        help=f'Configuration YAML file to use. Default: {DEFAULT_CONFIG_FILE}')
    parser.add_argument("--revision", required=True)
    parser.add_argument("--track", required=True)
    parser.add_argument("--jobid", required=False, 
                        help='If specified, Yamato API is called to update green revisions for projects according to dependencies.')
    parser.add_argument("--apikey", required=False, 
                        help='Needed for Yamato auth if jobid arg is specified.')
    parser.add_argument("--working-dir", required=False,
                        help='Working directory (optional). If omitted the root '
                        'of the repo will be used.')
    parser.add_argument("--target-branch", required=True,
                        help='The Git branch to merge the changes in the file into.')
    args = parser.parse_args(flags)
    if not os.path.isfile(args.config):
        parser.error(f'Cannot find config file {args.config}')
    return args


def main(argv):
    logging.basicConfig(level=logging.INFO, format='[%(levelname)s] %(message)s')
    args = parse_args(argv)
    config = load_config(args.config)
    editor_versions_file = config['editor_versions_file'].replace('TRACK',str(args.track))
    project_versions_file = config['project_versions_file'].replace('TRACK',str(args.track))

    try:
        working_dir = args.working_dir or os.path.abspath(git_cmd('rev-parse --show-toplevel', cwd='.').strip())
        print(f'Working directory: {working_dir}')
        
        verify_changed_files(editor_versions_file, args.revision, working_dir)

        if args.local:
            logging.warning('\n\n!! DEVELOPMENT MODE: will not switch branch, pull or push !!\n')
        else:
            checkout_and_pull_branch(args.target_branch, working_dir)
            if git_cmd('rev-parse HEAD').strip() == args.revision:
                print('No changes compared to current revision. Exiting...')
                return 0
           
        # Update, commit and push editor versions file
        if apply_target_revision_changes(editor_versions_file, config['yml_files_path'], args.revision, working_dir):
            commit_and_push(f'[CI] [{str(args.track)}] Updated latest editors metafile', working_dir, args.local)

            # Update, commit and push green project versions file
            if args.jobid and args.apikey:
                print(f'Updating green project revisions according to job {args.jobid}.')
                if update_green_project_revisions(editor_versions_file, project_versions_file, str(args.track), config['projects'], args.jobid, args.apikey, working_dir):
                    commit_and_push(f'[CI] [{str(args.track)}] Updated green project revisions', working_dir, args.local)
        else:
            print('No revision changes to merge. Exiting successfully without any '
                         'commit/push.')
        return 0
    except subprocess.CalledProcessError as err:
        logging.error(f"Failed to run '{err.cmd}'\nStdout:\n{err.stdout}\nStderr:\n{err.stderr}")
        return 1


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))