import os
from .shared_utils import load_json, find_matching_patterns
from .shared_utils import *

class UTR_log():
    ''''Handles parsing UTR logs (TestResults.json) against known error patterns'''

    def __init__(self, path_to_log):
        self.path = os.path.join(path_to_log,'TestResults.json')
        self.patterns = self.get_patterns()

    def get_patterns(self):
        '''Returns error patterns to match against. Each pattern has:
        pattern: regex to match some string against
        tags: tags to be added to Yamato additional results, typically one as identifier, and one as category such as instability, ...
        conclusion: success/failure/cancelled/inconclusive (if many patterns are matched for a command, most severe is chosen in the end)'''
        return [
            {
                'pattern': r'System.TimeoutException: Timeout while waiting',
                'tags': ['System.TimeoutException'],
                'conclusion': 'failure',
            },
            {
                'pattern': r'System.AggregateException: One or more errors occurred. \(Detected that ios-deploy is not running when attempting to establish player connection.\)',
                'tags': ['System.AggregateException', 'ios-deploy'],
                'conclusion': 'failure',
            },
            {
                # this matches everything and must therefore be the last item in the list
                'pattern': r'.+',
                'tags': ['unknown'],
                'conclusion': 'failure',
            }
        ]

    def read_log(self):
        '''Returns the string output, which is then matched against known patterns.
        In this case, the output corresponds to the error messages of the object marked as rootcause in TestResults.json'''
        logs = load_json(self.path)
        error_logs = [log for log in logs if log.get('rootCause')]
        if len(error_logs) > 0:
            return ' '.join(error_logs[0].get('errors',['']))
        return ''
