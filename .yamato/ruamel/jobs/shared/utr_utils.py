from .constants import *

def extract_flags(utr_flags, platform_name, api_name):
    
    # get base flags from shared metafile per platform_api
    flags = []
    for extra_utr_flag in utr_flags:
        for platform_apis,flag in extra_utr_flag.items():
            if f'{platform_name}_{api_name}'.lower() in map(str.lower, platform_apis) or 'all' in map(str.lower, platform_apis) :

                flag_keys = flag.split("=")
                if len(flag_keys) == 3:
                    flag_key = "".join(flag_keys[:-1])
                else:
                    flag_key = flag_keys[0]

                existing_indices = [i for i, existing_flag in enumerate(flags) if flag_key in existing_flag]
                if flag_key != '--extra-editor-arg' and len(existing_indices)>0:
                    flags[existing_indices[0]]=flag
                else:
                    flags.append(flag)
    return flags

    # get extra flags from project testplatform

    # override any extra flags 


