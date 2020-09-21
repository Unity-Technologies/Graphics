from .constants import *

def utr_playmode_flags(suite='playmode', testproject='.', editor_location='.Editor', artifacts_path=PATH_TEST_RESULTS):
    '''Sets: suite, testproject, editor-location, artifacts-path.
    Cancellable: none of them'''
    flags =  [
        f'--suite={suite}',
        f'--testproject={testproject}',
        f'--editor-location={editor_location}',
        f'--artifacts_path={artifacts_path}'
        ]
    return [f for f in flags if f]


def utr_editmode_flags(suite='editor', platform='editmode', testproject='.', editor_location='.Editor', artifacts_path=PATH_TEST_RESULTS):
    '''Sets: suite, platform, testproject, editor-location, artifacts-path.
    Cancellable: none of them'''
    flags = [
        f'--suite={suite}',
        f'--platform={platform}',
        f'--testproject={testproject}',
        f'--editor-location={editor_location}',
        f'--artifacts_path={artifacts_path}'
        ]
    return [f for f in flags if f]


def utr_standalone_not_split_flags(platform_spec, suite='playmode', platform='Standalone', testproject='.', editor_location='.Editor', artifacts_path=PATH_TEST_RESULTS, player_load_path='../../players', player_conn_ip='auto'):
    '''Sets: suite, platform, artifacts-path, testproject, editor-location, timeout.
    Cancellable: timeout'''
    flags = [
        f'--suite={suite}',
        f'--platform={platform}{platform_spec}',
        f'--artifacts_path={artifacts_path}',
        f'--testproject={testproject}',
        f'--editor-location={editor_location}'
        ]
    return [f for f in flags if f]


def utr_standalone_split_flags(platform_spec, suite='playmode', platform='Standalone', testproject='.', editor_location='.Editor', artifacts_path=PATH_TEST_RESULTS, player_load_path='../../players', player_conn_ip='auto'):
    '''Sets: suite, platform, artifacts-path, timeout, player-load-path, player-connection-ip.
    Cancellable: timeout, player-load-path, player-connection-ip'''
    flags = [ 
        f'--suite={suite}',
        f'--platform={platform}{platform_spec}',
        f'--artifacts_path={artifacts_path}',
        f'--player-load-path={player_load_path}' if player_load_path!=None else '',
        f'--player-connection-ip={player_conn_ip}' if player_conn_ip!=None else ''
        ]
    return [f for f in flags if f]


def utr_standalone_build_flags(platform_spec, suite='playmode', platform='Standalone', testproject='.', editor_location='.Editor', artifacts_path=PATH_TEST_RESULTS, player_save_path='../../players'):
    '''Sets: suite, platform, testproject, editor-location, artifacts-path, timeout, player-save-path, build-only.
    Cancellable: timeout, player-save-path'''
    flags = [
        f'--suite={suite}',
        f'--platform={platform}{platform_spec}',
        f'--testproject={testproject}',
        f'--editor-location={editor_location}',
        f'--artifacts_path={artifacts_path}',
        f'--player-save-path={player_save_path}' if player_save_path!=None else '',
        f'--build-only'
        ]
    return [f for f in flags if f]
