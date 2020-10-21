from .constants import *

def extract_flags(utr_flags, platform_name, api_name, build_config, color_space, project_folder):
    '''Given a list of utr flags (composed of flags under shared + project metafiles), filters out and returns flags relevant for this platform_api.
    Adds scripting backend colorspace flags
    '''
    
    flags = []
    for utr_flag in utr_flags:
        for platform_apis,flag in utr_flag.items():

            # if the flag is relevant for this platform + api
            if f'{platform_name}_{api_name}'.lower() in map(str.lower, platform_apis) or 'all' in map(str.lower, platform_apis) :
                
                # get the the flag without its value
                flag_keys = flag.split("=")
                if len(flag_keys) == 3: # for cases with additonal flag nested inside --extra-editor-arg
                    flag_key = "".join(flag_keys[:-1])
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



