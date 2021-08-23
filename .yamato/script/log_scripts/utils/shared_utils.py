import json
import re

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

            # if a matching patterns was found, skip the general unknown pattern
            if len(matches) > 0 and not all([p['tags'][0]=='retry' for p,m in matches]) and pattern['pattern'] == '.+':
                continue

            print('Found match for pattern: ',  pattern['pattern'])
            matches.append((pattern, match))
    return matches

def flatten_tags(tags):
    '''Tags param: 2d arr of tags gathered from patterns. Returns a 1d arr.'''
    return [tag for tag_list in tags for tag in tag_list]

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
