import json
import os
import re
import time
import urllib
#import urllib.parse.quote

import requests
from enum import Enum

import logger
import utils

katana_url = 'https://katana.bf.unity3d.com/json/'

#original code: https://gitlab.cds.internal.unity3d.com/burst/burst/tree/ci/run_katana_builds/Tools/CI

class KatanaResults(Enum):
    # https://github.com/Unity-Technologies/katana/blob/katana/master/buildbot/status/results.py (dead)
    UNKNOWN = -1
    SUCCESS = 0
    WARNINGS = 1
    FAILURE = 2
    SKIPPED = 3
    EXCEPTION = 4
    RETRY = 5
    CANCELED = 6
    NOT_REBUILT = 7
    DEPENDENCY_FAILURE = 8
    RESUME = 9
    MERGED = 10
    INTERRUPTED = 11


katana_success_results = [KatanaResults.SUCCESS,
                          KatanaResults.NOT_REBUILT]
katana_failure_results = [KatanaResults.DEPENDENCY_FAILURE,
                          KatanaResults.EXCEPTION,
                          KatanaResults.FAILURE,
                          KatanaResults.SKIPPED]

katana_retry_results = [KatanaResults.CANCELED,
                        KatanaResults.INTERRUPTED,
                        KatanaResults.MERGED,
                        KatanaResults.RETRY]


class TestResults(Enum):
    NOT_RUN = 0
    INCONCLUSIVE = 1
    SKIPPED = 2
    IGNORED = 3
    SUCCESS = 4
    FAILED = 5
    ERROR = 6
    CANCELLED = 7

#proj57-Test%20PlayMode%20-%20Mac%20%28Intel%29
def start_katana_build(project, properties):
    url = katana_url + "builders/%s/start-build/" % urllib.parse.quote(project)
    request = {
        "owner": "GFX Foundation Yamato <sophia@unity3d.com>",
        "sources_stamps": [
    {     
        "branch": "release/2019.1",
        "repository": "ScriptableRenderLoop",
        "revision": ""
    },
    {
        "branch": "master",
        "repository": "automation-tools",
        "revision": ""
    },
    {
        "branch": "2019.1/staging",
        "repository": "unity",
        "revision":""
    }
],
        "build_properties": properties
    }
    result = requests.post(url, json=request)
    if result.status_code != 200:
        logger.log(
            "Something went wrong when starting the build. The status code is %s. Message:\n%s" % (result.status_code,
                                                                                                   result.content))

    return result.json()


def has_katana_finished(build_info, project):
    build_number = get_build_number(build_info)
    if not isinstance(build_number, str):
                    build_number = build_number.decode('ascii', 'replace')

    if build_number == "null":
        return False
    elif build_number == "b'null'":
        return False

    build_status = get_build_status(build_number, project)

    build_result_code = get_result_code(build_status)
    return build_result_code != KatanaResults.UNKNOWN


def get_build_status(build_number, project):
    if not isinstance(build_number, str):
                    build_number = build_number.decode('ascii', 'replace')
    build_status_request = "%s?select=project&select=builders/%s/builds/%s&as_json=1&steps=0" % (katana_url, urllib.parse.quote(project), build_number)
    build_status = utils.get_url_json(build_status_request)['builders'][project]['builds'][build_number]
    return build_status


def process_running_builds(build_info, project):
    build_number = get_build_number(build_info)

    if build_number == "null":
        print("Build number is null; probably the build hasn't started yet. Lets wait a while before trying again")
        return False

    build_status = get_build_status(build_number, project)

    build_result_code = get_result_code(build_status)
    if build_result_code == KatanaResults.UNKNOWN:
        print("The build is still going on...")
        if isinstance(build_status['eta'], float):
            print("The eta for build completion is %s min" % (build_status['eta'] / 60))

        return False

    elif build_result_code in katana_success_results:
        print("Build Finished Successfully. All is green now :)")

    elif build_result_code in katana_failure_results:
        print("Looks like the build #%s (%s) has finished but wasn't successful. The result is %s." % (
                build_number, project, KatanaResults(build_status['results']).name))
        if build_result_code == KatanaResults.DEPENDENCY_FAILURE:
            analyze_dependencies(build_status)

        analyze_logs(build_status, project)

    elif build_result_code in katana_retry_results:
        logger.accumulate_for_slack("Looks like the build has finished but wasn't successful. " \
                                    "The result is %s." % KatanaResults(build_status['results']).name)

        analyze_logs(build_status, project)

    return True


def get_build_number(build_info):
    time.sleep(3)
    build_number_request = "%sbuild_request/%s/build_number/" % (katana_url, build_info[0]['build_request_id'])
    build_number_result = requests.get(build_number_request)
    if build_number_result.status_code != 200:
        print("Failed to get build number from build request number:\n%s" % build_number_result.content)
        raise Exception
    return build_number_result.content.strip()


def extract_project_from_web_url(url):
    url_path = url.split('/')
    return urllib.parse.unquote(url_path[6])


def extract_step_from_web_url(url):
    url_path = url.split('/')
    for i in range(len(url_path)):
        if url_path[i] == 'steps':
            return url_path[i + 1]
    raise Exception('Step not found')


def analyze_logs(build_status, project):
    for log in build_status["logs"]:
        if 'Upload%20Artifact' in log[1] or 'Copy%20crashDump%20for%20upload' in log[1] or 'Move%20LogFiles%20for%20Upload' in log[1]:
            continue

        log_content = requests.get(log[1] + '/plaintext_with_headers')

        if not 'code 0' in log_content.content:
            parse_failure(log[1], log_content)


def parse_failure(log_url, log_content):
    logger.accumulate_for_slack(log_url)
    if log_url.endswith('interrupt'):
        logger.accumulate_for_slack(log_content.content)
    if log_url.endswith('json'):
        analyze_test_results(log_content)
    elif log_url.endswith('stdio'):
        analyze_console_failure(log_content)

    logger.notify('')


def analyze_console_failure(log_content):
    clean_content = re.sub('\[[:0-9]*\]', '', log_content.content)
    splitted_clean_content = clean_content.split('\n')
    for word in bad_words:
        # todo: use regex case insensitive
        x = [str for str in splitted_clean_content if word in str]
        print('\n'.join(x))

    footer = clean_content.split('#####')[1:]
    for line in footer:
        logger.accumulate_for_slack(line)


def show_tests_details_by_result(tests, result_type, tag):
    filtered_tests = [i for i in tests if TestResults(i['state']) == result_type]
    if len(filtered_tests) != 0:
        logger.accumulate_for_slack("{0} {1} Tests found\n".format(len(filtered_tests), tag))
        for i in filtered_tests:
            print('----------------------------------------------------------------------------------------------------')
            print(i['name'], i['message'], i['stackTrace'])


def analyze_test_results(log_content):
    test_results = json.loads(log_content.content)
    if len(test_results['suites']) == 0 and test_results['summary']['testsCount'] == 0:
        logger.accumulate_for_slack('It failed because there were no tests to run')
        return
    for suite in test_results['suites']:
        if TestResults(suite['summary']['result']) == TestResults.SUCCESS:
            continue
        if len(suite['tests']) == 0:
            if 'failureConclusion' in test_results['summary']:
                logger.accumulate_for_slack(test_results['summary']['failureConclusion'])
            logger.accumulate_for_slack(suite['failureReasons'][0])
        else:
            show_tests_details_by_result(suite['tests'], TestResults.ERROR, 'Crashed')
            show_tests_details_by_result(suite['tests'], TestResults.FAILED, 'Failed')


def analyze_dependencies(build_status):
    print("Investigating failing dependencies...")
    for step in build_status['steps']:
        if get_result_code(step) not in katana_success_results and get_result_code(step) is not KatanaResults.SKIPPED:
            for dependency_name in step['dependencyUrls']:
                dependency = step['dependencyUrls'][dependency_name]

                if get_result_code(dependency) in katana_failure_results:
                    project = extract_project_from_web_url(dependency['url'])
                    process_running_builds({u'build_request_id': dependency['brid']}, project)


def get_result_code(build_status):
    build_result_code = KatanaResults.UNKNOWN
    if 'results' in build_status and build_status['results'] is not None:
        if 'isStarted' in build_status and build_status['isStarted'] == False:
            build_result_code = KatanaResults.SKIPPED
        elif isinstance(build_status['results'], list):
            build_result_code = KatanaResults(build_status['results'][0])
        else:
            build_result_code = KatanaResults(build_status['results'])

    return build_result_code
