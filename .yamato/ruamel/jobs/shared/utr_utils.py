from .constants import *

def utr_playmode_flags(suite='playmode', testproject='.', editor_location='.Editor', artifacts_path=PATH_TEST_RESULTS, scripting_backend='il2cpp', api_level='NET_4_6', color_space='Linear'):
    '''Sets: suite, testproject, editor-location, artifacts-path.
    Cancellable: none of them'''
    flags =  [
        f'--suite={suite}',
        f'--testproject={testproject}',
        f'--editor-location={editor_location}',
        f'--artifacts_path={artifacts_path}',
        f'--scripting-backend={scripting_backend}',
        #f'--extra-editor-arg="-apicompatibilitylevel={api_level}"',
        f'--extra-editor-arg="-colorspace={color_space}"',
        f'--reruncount=2'
        ]
    return [f for f in flags if f]


def utr_editmode_flags(suite='editor', platform='editmode', testproject='.', editor_location='.Editor', artifacts_path=PATH_TEST_RESULTS, scripting_backend='il2cpp', api_level='NET_4_6', color_space='Linear'):
    '''Sets: suite, platform, testproject, editor-location, artifacts-path.
    Cancellable: none of them'''
    flags = [
        f'--suite={suite}',
        f'--platform={platform}',
        f'--testproject={testproject}',
        f'--editor-location={editor_location}',
        f'--artifacts_path={artifacts_path}',
        f'--scripting-backend={scripting_backend}',
        #f'--extra-editor-arg="-apicompatibilitylevel={api_level}"',
        f'--extra-editor-arg="-colorspace={color_space}"',
        f'--reruncount=2'
        ]
    return [f for f in flags if f]


def utr_standalone_not_split_flags(platform_spec, suite='playmode', platform='Standalone', testproject='.', editor_location='.Editor', artifacts_path=PATH_TEST_RESULTS, player_load_path='../../players', player_conn_ip='auto', scripting_backend='il2cpp', api_level='NET_4_6', color_space='Linear'):
    '''Sets: suite, platform, artifacts-path, testproject, editor-location, timeout.
    Cancellable: timeout'''
    flags = [
        f'--suite={suite}',
        f'--platform={platform}{platform_spec}',
        f'--artifacts_path={artifacts_path}',
        f'--testproject={testproject}',
        f'--editor-location={editor_location}',
        f'--scripting-backend={scripting_backend}',
        #f'--extra-editor-arg="-apicompatibilitylevel={api_level}"',
        f'--extra-editor-arg="-colorspace={color_space}"',
        f'--reruncount=2'
        ]
    return [f for f in flags if f]


def utr_standalone_split_flags(platform_spec, suite='playmode', platform='Standalone', testproject='.', editor_location='.Editor', artifacts_path=PATH_TEST_RESULTS, player_load_path='../../players', player_conn_ip='auto', scripting_backend='il2cpp', api_level='NET_4_6', color_space='Linear'):
    '''Sets: suite, platform, artifacts-path, timeout, player-load-path, player-connection-ip.
    Cancellable: timeout, player-load-path, player-connection-ip'''
    flags = [ 
        f'--suite={suite}',
        f'--platform={platform}{platform_spec}',
        f'--artifacts_path={artifacts_path}',
        f'--player-load-path={player_load_path}' if player_load_path!=None else '',
        f'--player-connection-ip={player_conn_ip}' if player_conn_ip!=None else '',
        f'--scripting-backend={scripting_backend}',
        #f'--extra-editor-arg="-apicompatibilitylevel={api_level}"',
        f'--extra-editor-arg="-colorspace={color_space}"',
        f'--reruncount=2'
        ]
    return [f for f in flags if f]


def utr_standalone_build_flags(platform_spec, suite='playmode', platform='Standalone', testproject='.', editor_location='.Editor', graphics_api='', artifacts_path=PATH_TEST_RESULTS, player_save_path='../../players', scripting_backend='il2cpp', api_level='NET_4_6', color_space='Linear'):
    '''Sets: suite, platform, testproject, editor-location, artifacts-path, timeout, player-save-path, build-only.
    Cancellable: timeout, player-save-path'''
    if graphics_api.startswith('DX'):
        graphics_api = "Direct3D" + graphics_api[2:]
        
    flags = [
        f'--suite={suite}',
        f'--platform={platform}{platform_spec}',
        f'--testproject={testproject}',
        f'--extra-editor-arg="-playergraphicsapi={graphics_api}"',
        f'--editor-location={editor_location}',
        f'--artifacts_path={artifacts_path}',
        f'--player-save-path={player_save_path}' if player_save_path!=None else '',
        f'--build-only',
        f'--scripting-backend={scripting_backend}',
        #f'--extra-editor-arg="-apicompatibilitylevel={api_level}"',
        f'--extra-editor-arg="-colorspace={color_space}"'
        ]
    return [f for f in flags if f]
