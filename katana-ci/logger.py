import datetime
import os

log_file_name = 'log.txt'

#original code: https://gitlab.cds.internal.unity3d.com/burst/burst/tree/ci/run_katana_builds/Tools/CI

def log(message):
    global log_file_name
    print(message)
    with open(log_file_name, 'a+') as f:
        f.write('[{0}] {1}\n'.format(datetime.datetime.now(), message))