from .constants import VAR_UPM_REGISTRY, PATH_TEST_RESULTS_padded, PATH_PLAYERS, PATH_PACKAGES, PATH_UNITY_REVISION, PATH_TEMPLATES
from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from collections import defaultdict
import pickle

def defaultdict_to_dict(d):
    return {k: defaultdict_to_dict(v) for k, v in d.items()} if isinstance(d, defaultdict) else d

class YMLJob():
    
    def __init__(self):
        self.yml = defaultdict(lambda: defaultdict(lambda: defaultdict(list)))
        #self.yml = defaultdict(dict)
    
    def get_yml(self):
        #return dict(self.yml)
        return defaultdict_to_dict(self.yml)

    def set_name(self, name):
        self.yml['name'] = name
    
    def set_agent(self, agent):
        self.yml['agent'] = dict(agent)
    
    def set_skip_checkout(self, value):
        self.yml['skip_checkout'] = value

    def set_trigger_on_expression(self, expression): 
        self.yml['triggers']['expression'] = expression

    def set_trigger_recurrent(self, branch, frequency): # TODO make it appendable
        self.yml['triggers']['recurring'] = [{
                    'branch' : branch,
                    'frequency' : frequency}]
    
    def set_trigger_integration_branch(self, integration_branch):
        self.yml['triggers']['branches']['only'].append(integration_branch)
    
    def add_dependencies(self, dependencies): # TODO see if can be done with the defaultdict trick, also for commands
        if 'dependencies' in self.yml.keys():
            self.yml['dependencies'].extend(dependencies)
        else:
            self.yml['dependencies'] = dependencies
    
    def add_commands(self, commands):
        self.yml['commands'] = commands

    def add_var_custom_revision(self, editor_version):
        if editor_version == 'CUSTOM-REVISION':
            self.yml['variables']['CUSTOM_REVISION'] = 'custom_revision_not_set'
    
    def add_var_upm_registry(self):
        self.yml['variables']['UPM_REGISTRY'] = VAR_UPM_REGISTRY

    def add_var_custom(self, var_key, var_value): # used by editor. allows to set other variables without cluttering this class
        self.yml['variables'][var_key] = var_value


    def add_artifacts_test_results(self):
        self.yml['artifacts']['logs']['paths'].append(dss(PATH_TEST_RESULTS_padded)) 

    def add_artifacts_players(self):
        self.yml['artifacts']['players']['paths'].append(dss(PATH_PLAYERS)) 

    def add_artifacts_packages(self):
        self.yml['artifacts']['packages']['paths'].append(dss(PATH_PACKAGES)) 

    def add_artifacts_templates(self):
        self.yml['artifacts']['packages']['paths'].append(dss(PATH_TEMPLATES)) 

    def add_artifacts_unity_revision(self): # used by editor
        self.yml['artifacts']['unity_revision.zip']['paths'].append(dss(PATH_UNITY_REVISION)) 




