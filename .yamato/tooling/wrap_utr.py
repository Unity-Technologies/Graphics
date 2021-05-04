import subprocess
import platform
import sys

if __name__ == "__main__":
    cur_sys = platform.system()
    if cur_sys == 'Windows':
        subprocess.call(["utr.bat"] + sys.argv[1:])
    else:
        subprocess.call(["utr"] + sys.argv[1:])

