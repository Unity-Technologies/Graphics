from .constants import *
import itertools

def get_repeated_utr_calls(test_platform, platform, api, build_config, color_space, project_folder, utr_flags_key="utr_flags"):
    '''Returns a list with each element corresponding to list-of-utr-flags per each utr run within testplatform.
    If UTR is called just once (no utr_repeat specified in metafile), list has length of 1.'''
    utr_calls = []
    if test_platform.get("utr_repeat"):
        for utr_repeat in test_platform["utr_repeat"]:
            utr_calls.append(extract_flags(test_platform[utr_flags_key], platform["name"], api["name"], build_config, color_space, project_folder, utr_repeat.get(utr_flags_key,[])))
    else:
        utr_calls.append(extract_flags(test_platform[utr_flags_key], platform["name"], api["name"], build_config, color_space, project_folder))
    utr_calls = list(utr_calls for utr_calls,_ in itertools.groupby(utr_calls)) # removes duplicates which happen if repeated utr is specified but not for this platform+api
    return utr_calls

def extract_flags(utr_flags, platform_name, api_name, build_config, color_space, project_folder, utr_repeat_flags=[]):
    '''Given a list of utr flags (composed of flags under shared + project metafiles), filters out and returns flags relevant for this platform_api.
    If a flag is specified multiple times, the last value is taken, order being first shared metafile, then project metafile
    (inside the metafiles, the order in which flags are written is preserved). Thus, flags wtih [all] must be specified before api specific flags.  
    '''
    
    flags = []
    for utr_flag in utr_flags + utr_repeat_flags:
        for platform_apis,flag in utr_flag.items():

            # if the flag is relevant for this platform + api
            if f'{platform_name}_{api_name}'.lower() in map(str.lower, platform_apis) or 'all' in map(str.lower, platform_apis) :
                
                # get the the flag without its value
                flag_keys = flag.split("=")
                if len(flag_keys) > 1 and flag_keys[1].startswith('"-') and len(flag_keys[1].split(' '))>1: # for cases with additional flag nested inside with space, e.g --extra-editor-arg="-executemethod Editor.Setup"
                    flag_key = f'{flag_keys[0]}={flag_keys[1].split(" ")[0]}'
                elif len(flag_keys) == 3: # for cases with additonal flag nested inside, e.g. -extra-editor-arg="-playergraphicsapi=Direct3D11"
                    flag_key = "".join(flag_keys[:-1])               
                else: # most of the cases (--flag=value)
                    flag_key = flag_keys[0]

                # handle all dynamic flags
                flag = flag.replace('<SCRIPTING_BACKEND>',build_config["scripting_backend"])
                flag = flag.replace('<COLORSPACE>',color_space)
                flag = flag.replace('<PROJECT_FOLDER>',project_folder)
                if f'{platform_name}_{api_name}'.lower() in ['osx_metal', 'iphone_metal','osx_openglcore', 'linux_vulkan', 'linux_openglcore']:
                    if '%' in flag:
                        indices = [pos for pos, char in enumerate(flag) if char == '%']
                        if len(indices) != 2:
                            print(f'WARNING :: check utr flags for variables for {flag}')
                        flag = flag[:indices[0]] + '$' + flag[indices[0]+1:]
                        flag = flag.replace('%','')

                # check if such a flag is already present, if it is then overwrite. otherwise just append it
                existing_indices = [i for i, existing_flag in enumerate(flags) if flag_key in existing_flag]
                if flag_key != '--extra-editor-arg' and len(existing_indices)>0:
                    flags[existing_indices[0]]=flag
                else:
                    flags.append(flag)
    return sorted(flags)
