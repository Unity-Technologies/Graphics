import json
import os
import shutil
import tarfile
import zipfile
import subprocess

import requests

import sys

target_os = sys.platform

#original code: https://gitlab.cds.internal.unity3d.com/burst/burst/tree/ci/run_katana_builds/Tools/CI

def get_current_os():
    p = sys.platform
    if p == "darwin":
        return "macOS"
    if p == "win32":
        return "windows"
    return "linux"


def get_target_os():
    if target_os != sys.platform:
        return target_os

    return get_current_os()


def set_target_os(target):
    global target_os
    target_os = target

def get_url_json(url):
    print("  Getting json from {0}".format(url))
    from urllib.request import urlopen
    response = urlopen(url)
    return json.loads(response.read())


def extract_tarball(download_path, extract_path):
    print("  Extracting %s into %s" % (download_path, extract_path))
    tar = tarfile.open(download_path, "r:gz")
    tar.extractall(extract_path)
    tar.close()


def download_url(url, filename):
    print("  Downloading %s to %s" % (url, filename))

    r = requests.get(url, stream=True)
    with open(filename, 'wb') as f:
        shutil.copyfileobj(r.raw, f)


def extract_zip(archive, destination):
    print("  Extracting %s into %s" % (archive, destination))
    import zipfile
    if get_current_os() == "windows":
        zip_ref = ZipfileLongWindowsPaths(archive, 'r')
    else:
        zip_ref = zipfile.ZipFile(archive, 'r')
    zip_ref.extractall(destination)
    zip_ref.close()


def winapi_path(dos_path, encoding=None):
    path = os.path.abspath(dos_path)

    if path.startswith("\\\\"):
        path = "\\\\?\\UNC\\" + path[2:]
    else:
        path = "\\\\?\\" + path

    return path


def npm_cmd(cmd, registry):
    registry_cmd = ''
    if registry:
        registry_cmd = "--registry {0}".format(registry)

    formatted_cmd = 'npm {0} {1}'.format(cmd, registry_cmd)

    print("  Running: {0}".format(formatted_cmd))
    return subprocess.check_output(formatted_cmd, shell=True, stderr=subprocess.STDOUT)


def git_cmd(cmd, print_command=True):
    formatted_cmd = "git {0}".format(cmd)
    if print_command:
        print("  Running: {0}".format(formatted_cmd))
    return subprocess.check_output(formatted_cmd, shell=True, stderr=subprocess.STDOUT)


def git_cmd_code_only(cmd, print_command=True):
    formatted_cmd = "git {0}".format(cmd)
    if print_command:
        print("  Running: {0}".format(formatted_cmd))
    with open(os.devnull, 'w') as devnull:
        return subprocess.call(formatted_cmd, shell=True, stderr=devnull, stdout=devnull)


class ZipfileLongWindowsPaths(zipfile.ZipFile):

    def _extract_member(self, member, targetpath, pwd):
        targetpath = winapi_path(targetpath)
        return zipfile.ZipFile._extract_member(self, member, targetpath, pwd)
