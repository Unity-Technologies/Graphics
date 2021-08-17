
import argparse
import os
import sys
import requests
import json
import glob
import re
from utils.execution_log_patterns import execution_log_patterns
from utils.utr_log_patterns import utr_log_patterns

'''
This script runs for extended Yamato reporting. It
1. Parses the execution log and for each command and its output (except successful non-retries and failed tests)
    - finds matching patterns from execution_log_patterns
    - sends to Yamato extended reporting server
        title: command itself
        summary: substring(s) of the command output matching the specified pattern(s)
        tags: all (distinct) tags beloging to the matched pattern(s)
        conclusion: failure/inconclusive/cancelled/success, which applies to the parsed command (not the whole job status)
2. In case non-test related failure from UTR is matched, the script also reads TestResults.json and
    - finds matching patterns from utr_log_patterns
    - extends the data sent to Yamato extended reporting server in the previous point, by
        extending the summary with matched substrings from the utr pattern and by
        appending the utr pattern tags to the list of previous tags

By default, the script requires no parameters and uses default execution log location in Yamato.
To run it locally, specify
    --local
    --execution-log "<path to execution log file>"
'''

def load_json(file_path):
    with open(file_path) as f:
        json_data = f.readlines()
        json_data[0] = json_data[0].replace('let testData = ','') # strip the beginning
        json_data[-1] = json_data[-1][:-1] # strip the ; from end
        return json.loads(' '.join(json_data))
        #return json.load(f)

def get_execution_log():
    '''Returns the path to execution log file.'''
    path_to_execution_log = os.path.join(os.path.dirname(os.path.dirname(os.getcwd())),'Execution-*.log')
    print('Searching for logs in: ', path_to_execution_log)

    execution_log_file = glob.glob(path_to_execution_log)[0]
    print('Reading log: ', execution_log_file)
    return execution_log_file


def read_execution_log(log_file_path):
    '''Reads execution logs and returns:
    logs: dictionary with keys corresponding to commands, and values containing log output and status
    job_succeeded: boolean indicating if the job succeeded
    '''

    with open(log_file_path, encoding='utf-8') as f:
        lines = [l.replace('\n','') for l in f.readlines() if l != '\n'] # remove empty lines and all newline indicators

    # all log line idx starting/ending a new command
    command_idxs = [i for i,line  in enumerate(lines) if '################################### Running next command ###################################' in line]
    command_idxs_end = [i for i,line  in enumerate(lines) if '############################################################################################' in line]
    command_idxs.append(len(lines)) # add dummy idx to handle the last command

    # get output (list of lines) for each command
    logs = {}
    for i, command_idx in enumerate(command_idxs):
        if command_idx == len(lines):
            break
        command = '\n'.join(lines[command_idx+1: command_idxs_end[i]])
        output = lines[command_idx+3: command_idxs[i+1]-1]
        logs[command] = {}
        logs[command]['output'] = output
        logs[command]['status'] = 'Failed' if any("Command failed" in line for line in output) else 'Success'

    # if the command block succeeded overall
    overall_status = [line for line in lines if 'Commands finished with result:' in line][0].split(']')[1].split(': ')[1]
    job_succeeded = False if 'Failed' in overall_status else True
    return logs, job_succeeded

def parse_failures(logs, local):
    '''Parses each command in the execution log (and possibly UTR logs),
    recognizes any known errors, and posts additional data to Yamato.'''
    for cmd in logs.keys():

        # skip parsing successful commands which have not retried, or failed tests (these get automatically parsed in yamato results)
        # TODO: do we also want to add additional yamato results for these?
        if ((logs[cmd]['status'] == 'Success' and not any("Retrying" in line for line in logs[cmd]['output']))
                or any("Reason(s): One or more tests have failed." in line for line in logs[cmd]['output'])):
            print('Skipping: ', cmd)
            continue

        print('\nReading: ', cmd)

        # check if the error matches any known pattern marked in log_patterns.py
        output = '\n'.join(logs[cmd]['output'])
        matching_patterns, tags, conclusion = find_matching_patterns(execution_log_patterns, output)

        logs[cmd]['title'] = cmd
        logs[cmd]['conclusion'] = conclusion
        logs[cmd]['tags'] = tags
        logs[cmd]['summary'] = ''
        for pattern,match in matching_patterns: # update the command log with input from the matched pattern
            logs[cmd]['summary'] +=  match.group(0) + ' | ' if pattern['tags'][0] != 'unknown' else 'Unknown failure: check logs for more details. '



        # if it is an UTR non-test related error message not shown in Execution log but in test-results, append that to summary
        if  'non-test' in logs[cmd]['tags']:
            test_results_match = re.findall(r'(--artifacts_path=)(.+)(test-results)', cmd)[0]
            test_results_path = test_results_match[1] + test_results_match[2]
            # utr_failures, utr_tags, utr_conclusion = read_hoarder_log(os.path.join(test_results_path,'HoarderData.json'))
            utr_failures, utr_tags, utr_conclusion = read_test_results_json(os.path.join(test_results_path,'TestResults.json'))
            logs[cmd]['summary'] += utr_failures + ' | '
            logs[cmd]['tags'].extend(utr_tags)
            logs[cmd]['conclusion'] = utr_conclusion

        # post additional results to Yamato
        post_additional_results(logs[cmd], local)
    return


def find_matching_patterns(patterns, failure_string):
    '''Finds a matching pattern from a specified list of pattern objects for a specified failure string (e.g. command output).
    Returns the matching pattern object, and the matched substring.'''
    matches = []
    for pattern in patterns:
            match = re.search(pattern['pattern'], failure_string)
            if match:

                # if a matching patterns was found, skip the general unknown pattern
                if len(matches) > 0 and pattern['pattern'] == '.+':
                    continue

                print('Found match for pattern: ',  pattern['pattern'])
                matches.append((pattern, match))

    tags = [pattern['tags'] for (pattern,_) in matches]
    flattened_tags = [tag for tag_list in tags for tag in tag_list]

    conclusion = get_ruling_conclusion([pattern['conclusion'] for (pattern,_) in matches])

    return matches, flattened_tags, conclusion


def get_ruling_conclusion(conclusions):
    '''Pick a single conclusion out of several matches in the order of severity'''
    if 'failure' in conclusions:
        return 'failure'
    elif 'inconclusive' in conclusions:
        return 'inconclusive'
    elif 'cancelled' in conclusions:
        return 'cancelled'
    elif 'success' in conclusions:
        return 'success'
    else:
        return 'failure'


def read_test_results_json(log_file_path):
    '''Reads error messages from TestResults.json.'''
    logs = load_json(log_file_path)
    error_logs = [log for log in logs if log.get('rootCause')]
    if len(error_logs) > 0:
        failure_reasons = ' '.join(error_logs[0].get('errors',['']))

        _, tags, conclusion = find_matching_patterns(utr_log_patterns, failure_reasons)
        return failure_reasons, tags, conclusion
    else:
        return [], [], ''


def post_additional_results(cmd, local):
    '''Posts additional results to Yamato reporting server'''

    data = {
        'title': cmd['title'],
        'summary': cmd['summary'][:500] + '...',
        'conclusion': cmd['conclusion'],
        'tags' : list(set(cmd['tags']))
    }

    if local:
        print('Posting: ', json.dumps(data,indent=2), '\n')
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

        # read execution logs
        execution_log_file = get_execution_log() if not args.execution_log else args.execution_log
        logs, job_succeeded = read_execution_log(execution_log_file)

        if not job_succeeded:
            parse_failures(logs, args.local)

    except Exception as e:
        print('Failed to parse logs: ', str(e))


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
