from copy import deepcopy
import json

def format_metafile(metafile, shared, latest_editor_versions, unfold_agents_root_keys=[], unfold_test_platforms_root_keys=[]):
    '''Formats the metafile by retrieving all missing information from the shared metafile. This includes unfolding platform details, agent aliases etc.'''
    metafile['editors'] = _get_editors(metafile, shared, latest_editor_versions)
    metafile['target_editor'] = metafile.get('target_editor', shared.get('target_editor'))
    metafile['target_branch'] = metafile.get('target_branch', shared.get('target_branch'))
    metafile['target_branch_editor_ci'] = metafile.get('target_branch_editor_ci', shared.get('target_branch_editor_ci'))
    metafile['platforms'] = _unfold_platforms(metafile, shared)
    metafile = _unfold_individual_agents(metafile, shared, root_keys=unfold_agents_root_keys)
    metafile = _unfold_test_platforms(metafile, shared, root_keys=unfold_test_platforms_root_keys)
    return metafile

def _get_editors(metafile, shared, latest_editor_versions):
    '''Retrieves the editors from shared metafile, if not overriden by 'override_editors' in metafile.'''
    editors = shared['editors']
    for editor in editors:
        if editor["editor_pinning"]:
            editor['revisions'] = {}
            revisions = [{k:v} for k,v in latest_editor_versions[editor['track']]['editor_versions'].items() if str(editor['track']) in k] # get all revisions for this track
            for rev in revisions:
                for k,v in rev.items(): # TODO loops over the single dict value, see if there is a better way
                    editor['revisions'][k] = v
            
    #print(json.dumps(editors, indent=2))
    return editors

def _unfold_platforms(metafile, shared):

    platforms = []
    for m_plat in metafile.get('platforms',[]):
        s_plat = shared['platforms'][m_plat['name']]

        joint_plat = {**s_plat, **m_plat}
        joint_plat["extra_utr_flags"] = [] if not joint_plat.get("extra_utr_flags") else joint_plat.get("extra_utr_flags")
        joint_plat["extra_utr_flags_build"] = [] if not joint_plat.get("extra_utr_flags_build") else joint_plat.get("extra_utr_flags_build")

        if joint_plat.get('apis')=="" or joint_plat.get('apis')==None:
            joint_plat['apis'] = [{
                "name": ""
            }]
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


def _join_utr_flags(metafile, shared):
    for test_platform in metafile["test_platforms"]:
        shared_tp = [tp for tp in shared["test_platforms"] if tp["type"].lower() == test_platform["type"].lower()][0]
        test_platform["extra_utr_flags"].extend(shared_tp["extra_utr_flags"])
        if test_platform["type"].lower()=="standalone":
            test_platform["extra_utr_flags_build"].extend(shared_tp["extra_utr_flags_build"])
    return metafile



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
    
    metafile = _join_utr_flags(metafile, shared)
    return metafile