from .constants import *

def extract_flags(extra_utr_flags, platform_name, api_name):
    
    # get base flags from shared metafile per platform_api
    flags = []
    for extra_utr_flag in extra_utr_flags:
        for platform_apis,flag in extra_utr_flag.items():
            if f'{platform_name}_{api_name}'.lower() in map(str.lower, platform_apis) or 'all' in map(str.lower, platform_apis) :
                flags.append(flag)
    return flags

    # get extra flags from project testplatform

    # override any extra flags 


