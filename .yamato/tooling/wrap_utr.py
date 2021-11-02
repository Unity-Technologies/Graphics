import subprocess
import platform
import sys
from os.path import expanduser
from os import path
from os import getcwd
from os import listdir

if __name__ == "__main__":
    cur_sys = platform.system()
    if cur_sys == 'Windows':
        args = [path.join(getcwd(), "utr.bat")] + sys.argv[1:]
        subprocess.call(["utr.bat"] + args)
    elif cur_sys == 'Linux':
        subprocess.call(["utr"] + sys.argv[1:])
    else:
        cwd = path.abspath('.')
        # expanduser("~") is the same as ~
        utr_path = path.join(expanduser("~"), "Graphics/utr")
        if path.exists(utr_path):
            args = [path.join(getcwd(), utr_path)] + sys.argv[1:]
            print(args)
            output = subprocess.call(args)
        else:
            args = [path.join(getcwd(), "utr")] + sys.argv[1:]
            print(args)
            output = subprocess.call(args)
