import os
import subprocess
import platform
import sys
from os import path

if __name__ == "__main__":
    cur_sys = platform.system()
    output = 0

    print("Working directory: " + os.getcwd())
    print("Resources dir contents: " + os.listdir(path.join(sys.argv[1], "Assets/Resources/")))

    if cur_sys == 'Windows':
        output = subprocess.call(["utr.bat"] + sys.argv[2:])
    else:
        output = subprocess.call(["utr"] + sys.argv[2:])

    update_tests_file_path = path.join(sys.argv[1], "Assets/Resources/UpdateTests.txt")
    if not path.exists(update_tests_file_path):
        print("Error: Test assets need to be updated")
        exit(1)
    exit(output)
