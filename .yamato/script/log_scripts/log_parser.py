
import argparse
import os
import requests
import sys
import json
import glob
import re
from utils.execution_log import Execution_log
from utils.utr_log import UTR_log
from utils.unity_log import Unity_log
from utils.shared_utils import *
from utils.constants import *

'''
This script runs for extended Yamato reporting. It
1. Parses the execution log and for each command and its output (except successful non-retries and failed tests)
    - finds matching patterns from execution_log.py patterns
    - for each matched pattern, recursively finds matches also for any pattern with 'redirect' specified
    - sends to Yamato extended reporting server
        title: command itself
        summary: substring(s) of the command output matching the specified pattern(s)
        tags: all (distinct) tags beloging to the matched pattern(s)
        conclusion: failure/inconclusive/cancelled/success, which applies to the parsed command (not the whole job status)

By default, the script requires no parameters and uses default execution log location in Yamato.
To run it locally, specify
    --local
    --execution-log "<path to execution log file>"
'''



def parse_failures(execution_log, logs, local):
    '''Parses each command in the execution log (and possibly UTR logs),
    recognizes any known errors, and posts additional data to Yamato.'''
    for cmd in logs.keys():

        # skip parsing successful commands which have not retried, or failed tests (these get automatically parsed in yamato results)
        # TODO: do we also want to add additional yamato results for these?
        if ((logs[cmd]['status'] == 'Success' and not any("Retrying" in line for line in logs[cmd]['output']))
                or any("Reason(s): One or more tests have failed." in line for line in logs[cmd]['output'])):
            continue

        print('\nFound failed command: ', cmd, '\n')

        # initialize command data
        logs[cmd]['title'] = cmd
        logs[cmd]['conclusion'] = []
        logs[cmd]['tags'] = []
        logs[cmd]['summary'] = []

        # check if the error matches any known pattern marked in log_patterns.py, fill the command data for each match
        cmd_output = '\n'.join(logs[cmd]['output'])
        recursively_match_patterns(logs, cmd, execution_log.get_patterns(), cmd_output)

        # post additional results to Yamato
        post_additional_results(logs[cmd], local)
    return


def recursively_match_patterns(logs, cmd, patterns, failure_string):
    '''Match the given string against any known patterns. If any of the patterns contains a 'redirect',
    parse also the directed log in a recursive fashion.'''
    matches = find_matching_patterns(patterns, failure_string)
    for pattern, match in matches:

        logs[cmd]['conclusion'].append(pattern['conclusion'])
        logs[cmd]['tags'].append(pattern['tags'])
        logs[cmd]['summary'].append(match.group(0) if pattern['tags'][0] != 'unknown' else 'Unknown failure: check logs for more details. ')

        if pattern.get('redirect'):
            test_results_match = re.findall(r'(--artifacts_path=)(.+)(test-results)', cmd)[0]
            test_results_path = test_results_match[1] + test_results_match[2]
            for redirect in pattern['redirect']:

                if redirect == UTR_LOG:
                    try:
                        df = UTR_log(test_results_path)
                        recursively_match_patterns(logs, cmd, df.get_patterns(), df.read_log())
                    except Exception as e:
                        print(f'! Failed to parse UTR TestResults.json: ', str(e))
                elif redirect == UNITY_LOG:
                    try:
                        df = Unity_log(test_results_path)
                        recursively_match_patterns(logs, cmd, df.get_patterns(), df.read_log())
                    except Exception as e:
                        print(f'! Failed to parse UnityLog.txt', str(e))

                else:
                    print('! Invalid redirect: ', redirect)


def post_additional_results(cmd, local):
    '''Posts additional results to Yamato reporting server:
        - title: command itself
        - summary: concatenated summary of all matched patterns, each capped at 500 char
        - tags: non-duplicate tags of all matched patterns
        - conclusion: most severe conclusion of all matched patterns
        '''

    data = {
        'title': cmd['title'],
        'summary': ' | '.join(list(set([s[:500] for s in cmd['summary']]))),
        'conclusion': get_ruling_conclusion(cmd['conclusion']),
        'tags' : list(set(flatten_tags(cmd['tags'])))
    }

    if local:
        print('\nPosting: ', json.dumps(data,indent=2), '\n')
    else:
        server_url = os.environ['YAMATO_REPORTING_SERVER'] + '/result'
        headers = {'Content-Type':'application/json'}
        res = requests.post(server_url, json=data, headers=headers)
        if res.status_code != 200:
            raise Exception(f'!! Error: Got {res.status_code}')


def parse_args(argv):
    parser = argparse.ArgumentParser()
    parser.add_argument("--execution-log", required=False, help='Path to execution log file. If not specified, ../../Execution-*.log is used.', default=None)
    parser.add_argument("--local", action='store_true', help='If specified, API call to post additional results is skipped.', default=False)
    args = parser.parse_args(argv)
    return args


def main(argv):

    try:
        args = parse_args(argv)

        # read execution log
        execution_log = Execution_log(args.execution_log)
        logs, job_succeeded = execution_log.read_log()

        if not job_succeeded:
            parse_failures(execution_log, logs, args.local)

    except Exception as e:
        print('Failed to parse logs: ', str(e))


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
