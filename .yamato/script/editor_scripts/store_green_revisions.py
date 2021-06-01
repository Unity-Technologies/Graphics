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
from update_revisions import DEFAULT_CONFIG_FILE, DEFAULT_SHARED_FILE
from util.subprocess_helpers import git_cmd, run_cmd

yaml = ruamel.yaml.YAML()


def load_yml(filepath):
    with open(filepath) as f:
        return yaml.load(f)

def ordereddict_to_dict(d):
    return {k: ordereddict_to_dict(v) for k, v in d.items()} if isinstance(d, OrderedDict) else d


def checkout_and_pull_branch(branch, working_dir, development_mode=False):
    if not development_mode:
        git_cmd(f'checkout {branch}', working_dir)
        git_cmd('pull', working_dir)

def commit_and_push(commit_msg, working_dir, development_mode=False):
    if not development_mode:
        git_cmd(['commit', '-m', commit_msg], working_dir)
        git_cmd('pull', working_dir)
        git_cmd('push', working_dir)

def get_last_nightly_id(api_key, yamato_project_id, yamato_branch, yamato_nightly_job_definition):
    try:
        current_date = str(datetime.date.today())
        url = f"http://yamato-api.cds.internal.unity3d.com/jobs?filter=project eq {yamato_project_id} and branch eq '{yamato_branch}' and filename eq '{yamato_nightly_job_definition}' and submitted gt '{current_date}'"
        print(f'Calling [{url}]')
        headers={"Authorization":f"ApiKey {api_key}"}
        response = requests.get(url=url, headers=headers)
        if response.status_code != 200:
            raise Exception()

        jobs = response.json()["items"]
        if len(jobs) == 0:
            print(f"!! WARNING: No jobs found on {current_date}")
            return None

        for job in jobs:
            if job["links"]["triggeredBy"] == "/users/0":
                return job["id"]
        print(f"!! WARNING: No jobs submitted by CI found on {current_date}")
        return None

    except:
        print(f"!! ERROR: Failed to call Yamato API. Got {response.json()}")
        return None


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
        print(f"!! ERROR: Failed to call Yamato API. Got {response.json()}")
        return None

def update_green_project_revisions(editor_versions_file, project_versions_file, track, green_revision_jobs, job_id, api_key, working_dir):
    """Updates green project revisions file for given track. If any updates present, adds to git and returns True. If not, returns False."""

    # get the revisions used for the job, the last green project revisions, and Yamato dependency tree
    updated_at = str(datetime.datetime.utcnow())
    revisions = load_yml(editor_versions_file)[track.replace('.','_')]
    last_green_job_revisions = load_yml(project_versions_file)
    dependency_tree = get_yamato_dependency_tree(job_id, api_key)
    if not dependency_tree:
        return False

    # update revisions for each project
    is_updated = False
    for job_name in green_revision_jobs:
        jobs = [node for node in dependency_tree["nodes"] if node["name"].lower()==job_name.lower()]
        if len(jobs) == 0:
            print(f'Skipped "{job_name}" [not found in dependency tree]')
            continue

        job = jobs[0]
        if job["status"] == 'success':
            print(f'Updating "{job_name}" [job status: {job["status"]}]')

            job_name = job_name.replace(' ', '_').lower()
            if not last_green_job_revisions.get(job_name):
                last_green_job_revisions[job_name] = {}
            last_green_job_revisions[job_name]["updated_at"] = updated_at
            last_green_job_revisions[job_name]["last_green_revisions"] = ordereddict_to_dict(revisions)
            is_updated = True
        else:
            print(f'Skipped "{job_name}" [job status: {job["status"]}]')

    if is_updated: # at least one project got updated
        last_green_job_revisions = ordereddict_to_dict(last_green_job_revisions)
        with open(project_versions_file, 'w') as f:
            yaml.dump(last_green_job_revisions, f)

        git_cmd(f'add {project_versions_file}', working_dir)
        return True

    return False


def parse_args(flags):
    parser = argparse.ArgumentParser()
    parser.add_argument('--local', action='store_true',
                    help='For local development (doesn\'t switch branches, pull or push)')
    parser.add_argument("--track", required=True)
    parser.add_argument("--apikey", required=True,
                        help='Needed for Yamato auth if jobid arg is specified.')
    parser.add_argument("--target-branch", required=True,
                        help='The Git branch to merge the changes in the file into.')
    args = parser.parse_args(flags)
    return args


def main(argv):
    logging.basicConfig(level=logging.INFO, format='[%(levelname)s] %(message)s')
    args = parse_args(argv)
    config = load_yml(DEFAULT_CONFIG_FILE)
    shared = load_yml(DEFAULT_SHARED_FILE)

    editor_versions_file = config['editor_versions_file'].replace('TRACK',str(args.track))
    green_revisions_file = config['green_revisions_file'].replace('TRACK',str(args.track))
    yamato_branch = shared['target_branch']
    yamato_project_id = config['yamato_project_id']
    yamato_nightly_job_definition = config['nightly_job_definition']

    try:
        working_dir = os.path.abspath(git_cmd('rev-parse --show-toplevel', cwd='.').strip())
        print(f'Working directory: {working_dir}')

        if args.local:
            logging.warning('\n\n!! DEVELOPMENT MODE: will not switch branch, pull or push !!\n')
        else:
            checkout_and_pull_branch(args.target_branch, working_dir, args.local)

        nightly_job_id = get_last_nightly_id(args.apikey, yamato_project_id, yamato_branch, yamato_nightly_job_definition)
        print(f'Updating green project revisions according to job {nightly_job_id}.')
        if nightly_job_id:
            if update_green_project_revisions(editor_versions_file, green_revisions_file, str(args.track), config['green_revision_jobs'], nightly_job_id, args.apikey, working_dir):
                commit_and_push(f'[CI] [{str(args.track)}] Updated green project revisions', working_dir, args.local)
            else:
                print('No projects to update. Exiting successfully without any commit/push.')

        return 0
    except subprocess.CalledProcessError as err:
        logging.error(f"Failed to run '{err.cmd}'\nStdout:\n{err.stdout}\nStderr:\n{err.stderr}")
        return 1


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
