from copy import deepcopy

def format_metafile(metafile, shared, unfold_agents_root_keys=[], unfold_test_platforms_root_keys=[]):
    '''Formats the metafile by retrieving all missing information from the shared metafile. This includes unfolding platform details, agent aliases etc.'''
    metafile['editors'] = _get_editors(metafile, shared)
    metafile['target_editor'] = shared['target_editor']
    metafile['target_branch'] = shared['target_branch']
    metafile['platforms'] = _unfold_platforms(metafile, shared)
    metafile = _unfold_individual_agents(metafile, shared, root_keys=unfold_agents_root_keys)
    metafile = _unfold_test_platforms(metafile, shared, root_keys=unfold_test_platforms_root_keys)
    return metafile

def _get_editors(metafile, shared):
    '''Retrieves the editors from shared metafile, if not overriden by 'override_editors' in metafile.'''
    override_editors = metafile.get("override_editors", None)
    return override_editors if override_editors is not None else shared['editors']

def _unfold_individual_agents(metafile, shared, root_keys=[]):
    '''Unfolds all agents by their alias names corresponding to 'non_project_agents' in the shared metafile.
    First loops over keys under the whole metafile (root or 0th level) containing word 'agent' and replaces all of these.
    Then loops over all agents marked under root_keys (1st level) in similar fashion.'''

    # unfold all agents marked as keys directly under metafile
    for key, value in metafile.items():
        if 'agent' in key.lower():
            metafile[key] = dict(shared['non_project_agents'][value])
    
    # unfold any agents marked under any of the other keys (max 1 level depth)
    for root_key in root_keys:
        for key, value in metafile[root_key].items():
            if 'agent' in key.lower():
                metafile[root_key][key] = shared['non_project_agents'][value]
    return metafile


def _unfold_platforms(metafile, shared):
    '''Unfolds the metafile platform details by retrieving the corresponding platform from shared metafile, and then adjusting the details 
    to necessery level (removes unwanted apis from the shared platform object, 
    overrides all specified keys and unfolds agent aliases (if any overriden agent is specified by alias).
    Returns the new formatted 'platforms' section'''
    
    formatted_platforms = []
    for platform_meta in metafile.get('platforms',[]):
        platform_formatted = deepcopy(shared['platforms'][platform_meta['name']])
        
        # remove unwanted apis from deepcopy
        if platform_meta.get('apis') is not None:
            accepted_apis = []
            for api_shared in platform_formatted['apis']:
                if api_shared['name'].lower()  in map(str.lower, platform_meta['apis']):
                    accepted_apis.append(dict(api_shared))
            platform_formatted['apis'] = accepted_apis
        else:
            platform_formatted['apis'] = [{"name" : ""}] # needed for stereos

        # allow to override all keys
        if platform_meta.get('overrides', None) is not None:
            for override_key in platform_meta['overrides'].keys():
                
                # replace any overriden key with whats found in metafile
                platform_formatted[override_key] = (platform_meta['overrides'][override_key])

                # replace all named agents with actual agent dicts (unfold)
                # e.g. if one of the non_project_agent names is used instead
                if override_key.lower() == 'agent_package':
                    agent_name = platform_meta['overrides'][override_key]
                    platform_formatted[override_key] = shared['non_project_agents'][agent_name]
                if override_key.lower() == 'agents_project':
                    for agent_key, agent_name in platform_meta['overrides'][override_key].items():
                        if isinstance(agent_name, str):
                            platform_formatted[override_key][agent_key] = shared['non_project_agents'][agent_name]

        formatted_platforms.append(platform_formatted)
    return formatted_platforms 



def _unfold_test_platforms(metafile, shared, root_keys=[]):
    '''Retrieves test platform details from shared metafile, corresponding to the specific metafile. 
    Returns the new 'test_platforms' section.'''

    def replace_test_platforms(target_dict):
        test_platforms = []
        for tp in target_dict.get("test_platforms", []):
            tp["name"] = tp["type"] if not tp.get("name") else tp.get("name")
            tp["is_performance"] = False if not tp.get("is_performance") else tp.get("is_performance")
            tp["extra_utr_flags"] = [] if not tp.get("extra_utr_flags") else tp.get("extra_utr_flags")
            
            if tp["type"].lower()=="standalone":
                tp["extra_utr_flags_build"] = [] if not tp.get("extra_utr_flags_build") else tp.get("extra_utr_flags_build")
            test_platforms.append(tp)

        target_dict['test_platforms'] = test_platforms
        return target_dict

    # replace all test platforms found directly under root of metafile
    metafile = replace_test_platforms(metafile)

    # replace any additional test platforms found under other specified keys
    for root_key in root_keys:
        metafile[root_key] = replace_test_platforms(metafile[root_key])
    
    return metafile
