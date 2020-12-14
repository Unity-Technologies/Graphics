# Taken from https://github.com/Unity-Technologies/dots/blob/master/Tools/CI/util/subprocess_helpers.py

"""
Helper functions around subprocess
"""
import logging
import subprocess


def run_cmd(cmd, cwd=None):
    """Runs a command and returns its output as an UTF-8 string.
    NOTICE: The output normally ends with a newline that might need to be stripped.
    NOTICE: This does not return any stderr if the command succeeds, only on
        non-zero exit code when the error is raised.
    Args:
        cmd: Command line as a string or a list (if any item contains spaces).
        cwd: Working directory to run the command in. If omitted the
            interpreter's working dir will be used.
    Raises: subprocess.CalledProcessError similar to subprocess.check_output.
    """
    if isinstance(cmd, str):
        cmd = cmd.split()
    assert isinstance(cmd, list), 'cmd must be of list type, but was "{}"'.format(type(cmd))
    logging.info("  Running: {0} (cwd: {1})".format(' '.join(cmd), cwd))
    return subprocess.check_output(cmd, cwd=cwd, universal_newlines=True)


def npm_cmd(cmd, registry=None, cwd=None):
    """Runs a npm command and returns its output as an UTF-8 string.
    NOTICE: The command shall not include 'npm', since it will be added.
    NOTICE: The output normally ends with a newline that might need to be stripped.
    Args:
        cmd: Command line as a string or a list (if any item contains spaces).
        registry: NPM registry to use, if specified (otherwise default will be used).
        cwd: Working directory to run the command in. If omitted the
            interpreter's working dir will be used.
    Raises: subprocess.CalledProcessError similar to subprocess.check_output.
    """
    if isinstance(cmd, str):
        cmd = cmd.split()
    assert isinstance(cmd, list), 'cmd must be of list type, but was "{}"'.format(type(cmd))
    assert cmd[0] != 'npm', 'Please omit "npm" from the command: {0}'.format(' '.join(cmd))
    commands = ['npm'] + cmd[:]  # Use a new list to avoid modifying passed object.
    if registry:
        commands.append(['--registry', registry])
    logging.info("  Running: {0} (cwd: {1})".format(' '.join(commands), cwd))
    return subprocess.check_output(commands, cwd=cwd, universal_newlines=True,
                                   stderr=subprocess.STDOUT)


def git_cmd(cmd, cwd=None):
    """Runs a git command and returns its output as an UTF-8 string.
    NOTICE: The command shall not include 'git', since it will be added.
    NOTICE: The output normally ends with a newline that might need to be stripped.
    Args:
        cmd: Command line as a string or a list (if any item contains spaces).
        cwd: Working directory to run the command in. If omitted the
            interpreter's working dir will be used.
    Raises: subprocess.CalledProcessError similar to subprocess.check_output.
    """
    if isinstance(cmd, str):
        cmd = cmd.split()
    assert isinstance(cmd, list), 'cmd must be of list type, but was "{}"'.format(type(cmd))
    assert cmd[0] != 'git', 'Please omit "git" from the command: {0}'.format(' '.join(cmd))
    commands = ['git'] + cmd[:]  # Use a new list to avoid modifying passed object.
    logging.info("  Running: {0} (cwd: {1})".format(' '.join(commands), cwd))
    return subprocess.check_output(commands, cwd=cwd, universal_newlines=True,
                                   stderr=subprocess.STDOUT)