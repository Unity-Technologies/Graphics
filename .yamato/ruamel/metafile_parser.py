from copy import deepcopy
import json

def format_metafile(metafile, shared):
    '''Formats the metafile by retrieving all missing information from the shared metafile. This includes unfolding platform details, agent aliases etc.'''
    metafile['editors'] = shared['editors']
    metafile['target_editor'] = metafile.get('target_editor', shared.get('target_editor'))
    metafile['target_branch'] = metafile.get('target_branch', shared.get('target_branch'))
    metafile['target_branch_editor_ci'] = metafile.get('target_branch_editor_ci', shared.get('target_branch_editor_ci'))
    metafile['platforms'] = _unfold_platforms(metafile, shared)
    metafile = _unfold_individual_agents(metafile, shared)
    metafile = _unfold_test_platforms(metafile, shared)
    return metafile

def _unfold_platforms(metafile, shared):

    platforms = []
    for m_plat in metafile.get('platforms',[]):
        s_plat = shared['platforms'][m_plat['name']]

        # join details from the shared and project metafile platform (with metafile overwriting shared in case of same keys)
        joint_plat = {**s_plat, **m_plat} 

        # handle cases where api not specified (stereo projects)
        if joint_plat.get('apis')=="" or joint_plat.get('apis')==None:
            joint_plat['apis'] = [{
                "name": ""
            }]

        # retrieve build configs from shared.metafile based on the name specified under project.metafile platforms
        build_configs = []
        for build_config_name in m_plat.get("build_configs",[]):
            build_configs.extend([bc for bc in shared["build_configs"] if bc["name"]==build_config_name["name"]])
        joint_plat["build_configs"] = build_configs
        
        platforms.append(joint_plat)
    
    return platforms

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


def _unfold_test_platforms(metafile, shared):
    '''Concatenates test platform details from shared and project metafiles'''

    test_platforms = []
    for tp in metafile.get("test_platforms", []):

        # initialize possibly empty properties
        tp["name"] = tp["type"] if not tp.get("name") else tp.get("name")
        tp["utr_flags"] = [] if not tp.get("utr_flags") else tp.get("utr_flags")
        if tp["type"].lower()=="standalone":
            tp["utr_flags_build"] = [] if not tp.get("utr_flags_build") else tp.get("utr_flags_build")
        

        # get the matching test platform from shared metafile and
        # concatenate utr_flags and utr_flags_build for this test platform from shared.metafile + project.metafile
        shared_tp = [t for t in shared["test_platforms"] if t["type"].lower() == tp["type"].lower()][0]
        tp["utr_flags"] = shared_tp["utr_flags"] + tp["utr_flags"]
        if tp["type"].lower()=="standalone":
            tp["utr_flags_build"] = shared_tp.get("utr_flags_build",[]) + tp.get("utr_flags_build",[])


        # updating the testplatform done, append it to metafile
        test_platforms.append(tp)

    metafile['test_platforms'] = test_platforms 
    return metafile