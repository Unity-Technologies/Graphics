import os
import glob
from .shared_utils import load_json, find_matching_patterns
from .constants import *

COMMAND_START = '################################### Running next command ###################################'
COMMAND_END = '############################################################################################'
AFTER_BLOCK_START = 'Starting After Block'

class Execution_log():
    '''Handles log parsing and error matching of the execution log'''

    def __init__(self, path_to_log=""):
        self.path = glob.glob(os.path.join(os.path.dirname(os.path.dirname(os.getcwd())),'Execution-*.log'))[0] if path_to_log=="" else path_to_log
        self.patterns = self.get_patterns()

    def get_patterns(self):
        '''Returns error patterns to match against. Each pattern has:
        pattern: regex to match some string against
        tags: tags to be added to Yamato additional results, typically one as identifier, and one as category such as instability, ...
        conclusion: success/failure/cancelled/inconclusive (if many patterns are matched for a command, most severe is chosen in the end)'''
        return [
            # Order: retry blocks must be matched first
            # If either retry pattern is matched, the failures are further parsed to match any specific failure patterns written below
            {
                # This is matched if all retries fail.
                'pattern': r'(Failed after)(.+)(retries)',
                'tags': ['retry'],
                'conclusion': 'failure',
            },
            {
                # This matches both successful/failed retries.
                # Successful retries have no concrete pattern to match: the only difference with failed retry is that it does not contain 'Failed after n retries',
                # but no working regex for matching multiline against a negative lookahead was found yet.
                # Therefore, this pattern must come after failed retry pattern (python logic will handle recognizing this block as a successful retry)
                'pattern': r'(Retrying)',
                'tags': ['retry'],
                'conclusion': 'success',
            },
            # Order: patterns below can be in any order, and the script can match multiple patterns
            {
                'pattern': r'(command not found)',
                'tags': ['failure'],
                'conclusion': 'failure',
            },
            {
                #  Or with newlines: r'(packet_write_poll: Connection to)((.|\n)+)(Operation not permitted)((.|\n)+)(lost connection)',
                'pattern': r'(packet_write_poll: Connection to)(.+)(Operation not permitted)',
                'tags': ['packet_write_poll','instability'],
                'conclusion': 'inconclusive',
            },
            {
                # Or: r'(LTO : error: L0496: error during communication with the LTO process: The pipe has been ended)'
                'pattern': r'(orbis-ld stderr :LLVM ERROR: out of memory)((.|\n)+)(LLVM ERROR: out of memory)',
                'tags': ['oom','instability'],
                'conclusion': 'failure',
            },
            {
                'pattern': r'(fatal: not a git repository (or any of the parent directories): .git)',
                'tags': ['git'],
                'conclusion': 'failure',
            },
            {
                'pattern': r'(LTO : error: L0492: LTOP internal error: bad allocation)',
                'tags': ['instability', 'bad-allocation'],
                'conclusion': 'failure',
            },
            {
                'pattern': r'Reason\(s\): One or more tests have failed.', # this one is unused right now since yamato does it automatically
                'tags': ['tests'],
                'conclusion': 'failure',
            },
            {
                'pattern': r'Reason\(s\): One or more non-test related errors or failures occurred.', # if hit this, read hoarder file
                'tags': ['non-test'],
                'conclusion': 'failure',
                'redirect': [
                    UTR_LOG,
                    UNITY_LOG
                ]
            },
            # Order: this matches everything and must therefore be the last item in the list
            # If any previous pattern has been matched, this one is skipped
            {
                'pattern': r'.+',
                'tags': ['unknown'],
                'conclusion': 'failure',
            }
        ]

    def read_log(self):
        '''Reads execution logs and returns:
        logs: dictionary with keys corresponding to commands, and values containing log output and status
        job_succeeded: boolean indicating if the job succeeded
        '''

        with open(self.path, encoding='utf-8') as f:
            lines = [l.replace('\n','') for l in f.readlines() if l != '\n'] # remove empty lines and all newline indicators

        # after block index
        after_idx = [i for i,line  in enumerate(lines) if AFTER_BLOCK_START in line][0]

        # all log line idx starting/ending a new command
        cmd_idxs = [i for i,line  in enumerate(lines) if COMMAND_START in line]
        cmd_idxs_end = [i for i,line  in enumerate(lines) if COMMAND_END in line]
        cmd_idxs.append(len(lines)) # add dummy idx to handle the last command

        # get output (list of lines) for each command
        logs = {}
        for i, cmd_idx in enumerate(cmd_idxs):
            if cmd_idx == len(lines) or cmd_idx >= after_idx:
                break
            cmd = '\n'.join(lines[cmd_idx+1: cmd_idxs_end[i]])
            output = lines[cmd_idx+3: cmd_idxs[i+1]-1]
            logs[cmd] = {}
            logs[cmd]['output'] = output
            logs[cmd]['status'] = 'Failed' if any("Command failed" in line for line in output) else 'Success'

        # if the command block succeeded overall
        overall_status = [line for line in lines if 'Commands finished with result:' in line][0].split(']')[1].split(': ')[1]
        job_succeeded = False if 'Failed' in overall_status else True
        return logs, job_succeeded
