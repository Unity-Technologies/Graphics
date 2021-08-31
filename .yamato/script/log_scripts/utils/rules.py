def add_unknown_pattern_if(matches):
    '''Only add unknown pattern if no other pattern has been matched yet,
    i.e. skip if no matches are found, or only matches indicate a retry.'''
    if len(matches) == 0:
        return True
    elif len(matches) == 1:
        if ('successful-retry' in matches[0][0]['tags'] or 'failed after' in matches[0][0]['pattern'].lower()):
            return True
    return False


def add_successful_retry_if(matches):
    '''Add only if failed retry has not matched'''
    if len(matches) == 1 and 'failed after' in matches[0][0]['pattern'].lower():
        return False
    return True
