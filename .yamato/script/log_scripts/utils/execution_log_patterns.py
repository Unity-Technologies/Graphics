# Contains patterns to match in the execution log
#
# Conclusion can be either: success, failure, cancelled, inconclusive.
# Conclusions of utr_log_patterns overwrite conclusions of execution_log_patterns.
# Tags of utr_log_patterns get appended to tags of execution_log_patterns.

execution_log_patterns = [
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
    },
    # Order: this matches everything and must therefore be the last item in the list
    # If any previous pattern has been matched, this one is skipped
    {
        'pattern': r'.+',
        'tags': ['unknown'],
        'conclusion': 'failure',
    }
]
