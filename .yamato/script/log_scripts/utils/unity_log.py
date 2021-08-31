import os
import glob
from .shared_utils import load_json, find_matching_patterns

class Unity_log():
    ''''Handles parsing Unity log (UnityLog.txt) against known error patterns'''

    def __init__(self, path_to_log):
        self.path = glob.glob(os.path.join(path_to_log,"*/**/",'UnityLog.txt'))[0]
        self.patterns = self.get_patterns()

    def get_patterns(self):
        '''Returns error patterns to match against. Each pattern has:
        pattern: regex to match some string against
        tags: tags to be added to Yamato additional results, typically one as identifier, and one as category such as instability, ...
        conclusion: success/failure/cancelled/inconclusive (if many patterns are matched for a command, most severe is chosen in the end)'''
        return [
            {
                'pattern': r'TcpProtobufSession::SendMessageAsync',
                'tags': ['network','instability'],
                'conclusion': 'failure',
            },
            {
                'pattern': r'AcceleratorClientConnectionCallback - disconnected - cacheserver-slo',
                'tags': ['cache','instability'],
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
        In this case, returns the whole UnityLog.txt contents.'''
        with open(self.path, encoding='utf-8') as f:
            return f.read()
