# Contains patterns to match errors in the hoarder json suites.failureReasons in case of non-test related errors
#
# Conclusion can be either: success, failure, cancelled, inconclusive.
# Conclusions of utr_log_patterns overwrite conclusions of execution_log_patterns.
# Tags of utr_log_patterns get appended to tags of execution_log_patterns.

# TODO: patterns for indvidual test cases, warnings
# TODO: proper tags
execution_log_patterns = [
    {
        'pattern': r'(command not found)',
        'tags': ['failure'],
        'conclusion': 'failure',
    },
    {
        #  Or with newlines: r'(packet_write_poll: Connection to)((.|\n)+)(Operation not permitted)((.|\n)+)(lost connection)',
        'pattern': r'(packet_write_poll: Connection to)(.+)(Operation not permitted)',
        'tags': ['instability'],
        'conclusion': 'inconclusive',
    },
    {
        # Or: r'(LTO : error: L0496: error during communication with the LTO process: The pipe has been ended)'
        'pattern': r'(orbis-ld stderr :LLVM ERROR: out of memory)((.|\n)+)(LLVM ERROR: out of memory)',
        'tags': ['oom','instability'],
        'conclusion': 'inconclusive',
    },
    {
        'pattern': r'(fatal: not a git repository (or any of the parent directories): .git)',
        'tags': ['instability'],
        'conclusion': 'inconclusive',
    },
    {
        'pattern': r'(LTO : error: L0492: LTOP internal error: bad allocation)',
        'tags': ['instability'],
        'conclusion': 'inconclusive',
    },
    {
        'pattern': r'(Failed after)(.+)(retries)',
        'tags': ['retry'],
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
    {
        # this matches everything and must therefore be the last item in the list
        'pattern': r'.+',
        'tags': ['unknown'],
        'conclusion': 'failure',
    }
]
