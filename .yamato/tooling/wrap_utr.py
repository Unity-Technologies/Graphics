import subprocess
import argparse

if __name__ == "__main__":
    parser = argparse.ArgumentParser("Wraps a UTR call to swallow the output for expected failing runs")
    parser.add_argument("--args")

    args = parser.parse_args()
    subprocess.call(["utr"] + args.args.split())
