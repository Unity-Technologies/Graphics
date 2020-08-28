from .constants import *

def utr_playmode_flags(suite='playmode', testproject='.', editor_location='.Editor', artifacts_path=PATH_TEST_RESULTS, extra_utr_flags=[]):
    flags = [
        f'--suite={suite}',
        f'--testproject={testproject}',
        f'--editor-location={editor_location}',
        f'--artifacts_path={artifacts_path}']
    
    flags.extend(extra_utr_flags)
    return [f for f in flags if f] 

def utr_editmode_flags(suite='editor', platform='editmode', testproject='.', editor_location='.Editor', artifacts_path=PATH_TEST_RESULTS, extra_utr_flags=[]):
    flags = [
        f'--suite={suite}',
        f'--platform={platform}',
        f'--testproject={testproject}',
        f'--editor-location={editor_location}',
        f'--artifacts_path={artifacts_path}']
    
    flags.extend(extra_utr_flags)
    return [f for f in flags if f] 

def utr_standalone_not_split_flags(platform_spec, suite='playmode', platform='Standalone', testproject='.', editor_location='.Editor', artifacts_path=PATH_TEST_RESULTS, timeout=1200, player_load_path='../../players', player_conn_ip='auto', extra_utr_flags=[]):
    flags = [
        f'--suite={suite}',
        f'--platform={platform}{platform_spec}',
        f'--artifacts_path={artifacts_path}',
        f'--testproject={testproject}',
        f'--editor-location={editor_location}'
]

    flags.extend(extra_utr_flags)
    return [f for f in flags if f] 

def utr_standalone_split_flags(platform_spec, suite='playmode', platform='Standalone', testproject='.', editor_location='.Editor', artifacts_path=PATH_TEST_RESULTS, timeout=1200, player_load_path='../../players', player_conn_ip='auto', extra_utr_flags=[]):
    flags = [
        f'--suite={suite}',
        f'--platform={platform}{platform_spec}',
        f'--artifacts_path={artifacts_path}',
        f'--timeout={timeout}' if timeout!=None else '',
        f'--player-load-path={player_load_path}' if player_load_path!=None else '',
        f'--player-connection-ip={player_conn_ip}' if player_conn_ip!=None else '']

    flags.extend(extra_utr_flags)
    return [f for f in flags if f] 

def utr_standalone_build_flags(platform_spec, suite='playmode', platform='Standalone', testproject='.', editor_location='.Editor', artifacts_path=PATH_TEST_RESULTS, timeout=1200, player_save_path='../../players', extra_utr_flags=[]):
    flags = [
        f'--suite={suite}',
        f'--platform={platform}{platform_spec}',
        f'--testproject={testproject}',
        f'--editor-location={editor_location}',
        f'--artifacts_path={artifacts_path}',
        f'--timeout={timeout}' if timeout!=None else '',
        f'--player-save-path={player_save_path}' if player_save_path!=None else '',
        f'--build-only']

    flags.extend(extra_utr_flags)
    return [f for f in flags if f] 