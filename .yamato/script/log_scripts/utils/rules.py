
def add_successful_retry_if(matches):
    '''Add only if failed retry has not matched'''
    if len(matches) == 1 and 'failed after' in matches[0][0]['pattern'].lower():
        return False
    return True
