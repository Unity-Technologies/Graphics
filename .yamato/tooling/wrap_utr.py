import subprocess
import platform
import sys
from os.path import expanduser
from sys import path

if __name__ == "__main__":
    cur_sys = platform.system()
    if cur_sys == 'Windows':
        subprocess.call(["utr.bat"] + sys.argv[1:])
    elif cur_sys == 'Linux':
        subprocess.call(["utr"] + sys.argv[1:])
    else:
        cwd = path.abspath('.')
        # expanduser("~") is the same as ~
        utr_path = path.join(expanduser("~"), "Graphics/utr")
        output = subprocess.call([utr_path] + sys.argv[1:])
