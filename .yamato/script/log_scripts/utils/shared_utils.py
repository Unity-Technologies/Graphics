import json
import re
from .constants import *

def load_json(file_path):
    with open(file_path) as f:
        json_data = f.readlines()
        json_data[0] = json_data[0].replace('let testData = ','') # strip the beginning
        json_data[-1] = json_data[-1][:-1] # strip the ; from end
        return json.loads(' '.join(json_data))
        #return json.load(f)

def find_matching_patterns(patterns, failure_string):
    '''Finds a matching pattern from a specified list of pattern objects for a specified failure string (e.g. command output).
    Returns the matching pattern object, and the matched substring.'''
    matches = []
    for pattern in patterns:
        match = re.search(pattern['pattern'], failure_string)
        if match:

            # if a pattern is added conditionally, skip it if condition is not fulfilled
            if pattern.get('add_if'):
                if not pattern['add_if'](matches):
                    continue

            print('Found match for pattern: ',  pattern['pattern'])
            matches.append((pattern, match))
    return matches

def format_tags(tags):
    '''Flattens tags, removes duplicates, removes TAG_INSTABILITY if retry was successful
     (latter is because we need to have either one or another, we cannot have both, so that we can distinguish them by tags)'''
    tags = sorted(list(set([tag for tag_list in tags for tag in tag_list]))) # flatten and remove duplicates
    if TAG_INSTABILITY in tags and TAG_SUCCESFUL_RETRY in tags:
        tags.remove(TAG_INSTABILITY)
    return tags

def get_ruling_conclusion(conclusions, tags):
    '''Pick a single conclusion out of several matches in the order of severity'''
    if TAG_SUCCESFUL_RETRY in tags:
        return 'success'
    elif 'failure' in conclusions:
        return 'failure'
    elif 'inconclusive' in conclusions:
        return 'inconclusive'
    elif 'cancelled' in conclusions:
        return 'cancelled'
    elif 'success' in conclusions:
        return 'success'
    else:
        return 'failure'

def add_unknown_pattern_if_appropriate(cmd):
    '''Adds an unknown failure pattern if no patterns were matched at all, or only successful retry was matched.'''

    if (len(cmd['tags']) == 0 # no pattern matched at all
        or (len(cmd['tags']) == 1 and TAG_SUCCESFUL_RETRY in cmd['tags'])): # only successful retry pattern matched

        cmd['conclusion'].append('failure')
        cmd['tags'].append('unknown')
        cmd['summary'].append( 'Unknown failure: check logs for more details. ')
