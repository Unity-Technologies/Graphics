import subprocess
import platform
import sys
from os import path
from os.path import expanduser

if __name__ == "__main__":
    cur_sys = platform.system()
    output = 0

    if cur_sys == 'Windows':
        output = subprocess.call(["utr.bat"] + sys.argv[2:])
    elif cur_sys == 'Linux':
        output = subprocess.call(["utr"] + sys.argv[2:])
    else:
        cwd = path.abspath('.')
        # expanduser("~") is the same as ~
        utr_path = path.join(expanduser("~"), "Graphics/utr")
        output = subprocess.call([utr_path] + sys.argv[2:])



    update_tests_file_path = path.join(sys.argv[1], "Assets/Resources/UpdateTests.txt")
    if path.exists(update_tests_file_path):
        print("Error: Test assets need to be updated")
        exit(1)
    exit(output)
