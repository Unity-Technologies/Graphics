from .constants import *

def extract_flags(utr_flags, platform_name, api_name, build_config, color_space, project_folder):
    '''Given a list of utr flags (composed of flags under shared + project metafiles), filters out and returns flags relevant for this platform_api.
    If a flag is specified multiple times, the last value is taken, order being first shared metafile, then project metafile
    (inside the metafiles, the order in which flags are written is preserved). Thus, flags wtih [all] must be specified before api specific flags.  
    '''
    
    flags = []
    for utr_flag in utr_flags:
        for platform_apis,flag in utr_flag.items():

            # if the flag is relevant for this platform + api
            if f'{platform_name}_{api_name}'.lower() in map(str.lower, platform_apis) or 'all' in map(str.lower, platform_apis) :
                
                # get the the flag without its value
                flag_keys = flag.split("=")
                if len(flag_keys) == 3: # for cases with additonal flag nested inside, e.g. -extra-editor-arg="-playergraphicsapi=Direct3D11"
                    flag_key = "".join(flag_keys[:-1])
                elif len(flag_keys) > 1 and flag_keys[1].startswith('"-') and len(flag_keys[1].split(' '))>1: # for cases with additional flag nested inside with space, e.g --extra-editor-arg="-executemethod Editor.Setup"
                    flag_key = f'{flag_keys[0]}={flag_keys[1].split(" ")[0]}'
                else: # most of the cases (--flag=value)
                    flag_key = flag_keys[0]

                # handle all dynamic flags
                flag = flag.replace('<SCRIPTING_BACKEND>',build_config["scripting_backend"])
                flag = flag.replace('<COLORSPACE>',color_space)
                flag = flag.replace('<PROJECT_FOLDER>',project_folder)

                # check if such a flag is already present, if it is then overwrite. otherwise just append it
                existing_indices = [i for i, existing_flag in enumerate(flags) if flag_key in existing_flag]
                if flag_key != '--extra-editor-arg' and len(existing_indices)>0:
                    flags[existing_indices[0]]=flag
                else:
                    flags.append(flag)
    return sorted(flags)


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
        f'--extra-editor-arg="-executemethod" --extra-editor-arg="SetColorSpace{color_space}"',
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
        f'--extra-editor-arg="-executemethod" --extra-editor-arg="SetColorSpace{color_space}"',
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
        f'--extra-editor-arg="-executemethod" --extra-editor-arg="SetColorSpace{color_space}"',
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
        #f'--scripting-backend={scripting_backend}',
        #f'--extra-editor-arg="-apicompatibilitylevel={api_level}"',
        #f'--extra-editor-arg="-executemethod" --extra-editor-arg="SetColorSpace{color_space}"',
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
        f'--editor-location={editor_location}',
        f'--artifacts_path={artifacts_path}',
        f'--player-save-path={player_save_path}' if player_save_path!=None else '',
        f'--build-only',
        f'--scripting-backend={scripting_backend}',
        f'--extra-editor-arg="-executemethod" --extra-editor-arg="SetColorSpace{color_space}"',
        f'--extra-editor-arg="-executemethod" --extra-editor-arg="SetupProject.ApplySettings"',
        f'--extra-editor-arg="{graphics_api}"',
        #f'--extra-editor-arg="-apicompatibilitylevel={api_level}"',
        ]
    return [f for f in flags if f]
