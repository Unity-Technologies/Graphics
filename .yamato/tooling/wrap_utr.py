import subprocess
import argparse
import platform

if __name__ == "__main__":
    parser = argparse.ArgumentParser("Wraps a UTR call to swallow the output for expected failing runs")
    parser.add_argument("--args")

    args = parser.parse_args()
    cur_sys = platform.system()
    if cur_sys == 'Windows':
        subprocess.call(["utr.bat"] + args.args.split())
    else:
        subprocess.call(["utr"] + args.args.split())

