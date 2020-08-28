from .constants import *

def utr_playmode_flags(suite='playmode', testproject='.', editor_location='.Editor', artifacts_path=PATH_TEST_RESULTS, extra_utr_flags=[]):
    flags = [
        f'--suite={suite}',
        f'--testproject={testproject}',
        f'--editor-location={editor_location}',
        f'--artifacts_path={artifacts_path}']
    
    flags.extend(extra_utr_flags)
    return flags 

def utr_editmode_flags(suite='editor', platform='editmode', testproject='.', editor_location='.Editor', artifacts_path=PATH_TEST_RESULTS, extra_utr_flags=[]):
    flags = [
        f'--suite={suite}',
        f'--platform={platform}',
        f'--testproject={testproject}',
        f'--editor-location={editor_location}',
        f'--artifacts_path={artifacts_path}']
    
    flags.extend(extra_utr_flags)
    return flags

def utr_standalone_not_split_flags(platform_spec, suite='playmode', platform='Standalone', testproject='.', editor_location='.Editor', artifacts_path=PATH_TEST_RESULTS, timeout=1200, player_load_path='../../players', player_conn_ip='auto', extra_utr_flags=[]):
    flags = [
        f'--suite={suite}',
        f'--platform={platform}{platform_spec}',
        f'--artifacts_path={artifacts_path}',
        f'--testproject={testproject}',
        f'--editor-location={editor_location}'

#--extra-editor-arg="-executemethod" 
#--extra-editor-arg="CustomBuild.BuildLinuxVulkanLinear" 
]

    flags.extend(extra_utr_flags)
    return flags

def utr_standalone_split_flags(platform_spec, suite='playmode', platform='Standalone', testproject='.', editor_location='.Editor', artifacts_path=PATH_TEST_RESULTS, timeout=1200, player_load_path='../../players', player_conn_ip='auto', extra_utr_flags=[]):
    flags = [
        f'--suite={suite}',
        f'--platform={platform}{platform_spec}',
        f'--artifacts_path={artifacts_path}',
        f'--timeout={timeout}',
        f'--player-load-path={player_load_path}',
        f'--player-connection-ip={player_conn_ip}']

    flags.extend(extra_utr_flags)
    return flags

def utr_standalone_build_flags(platform_spec, suite='playmode', platform='Standalone', testproject='.', editor_location='.Editor', artifacts_path=PATH_TEST_RESULTS, timeout=1200, player_save_path='../../players', extra_utr_flags=[]):
    flags = [
        f'--suite={suite}',
        f'--platform={platform}{platform_spec}',
        f'--testproject={testproject}',
        f'--editor-location={editor_location}',
        f'--artifacts_path={artifacts_path}',
        f'--timeout={timeout}',
        f'--player-save-path={player_save_path}',
        f'--build-only']

    flags.extend(extra_utr_flags)
    return flags