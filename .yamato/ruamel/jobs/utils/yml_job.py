from .constants import VAR_UPM_REGISTRY, PATH_TEST_RESULTS_padded, PATH_PLAYERS, PATH_PACKAGES, PATH_UNITY_REVISION
from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss


class YMLJob():
    
    def __init__(self):
        self.yml = {}

    def set_name(self, name):
        self.yml['name'] = name
    
    def set_agent(self, agent):
        self.yml['agent'] = dict(agent)
    
    def set_skip_checkout(self, value):
        self.yml['skip_checkout'] = value

    def set_trigger_on_expression(self, expression):
        self.yml['triggers'] = {'expression':expression}

    def set_trigger_recurrent(self, branch, cron_expression): # TODO append
        self.yml['triggers'] = {'recurring': [{
                    'branch' : branch,
                    'frequency' : cron_expression}]}
    
    def add_dependencies(self, dependencies):
        if 'dependencies' in self.yml.keys():
            self.yml['dependencies'].extend(dependencies)
        else:
            self.yml['dependencies'] = dependencies
    
    def set_commands(self, commands): # TODO append
        self.yml['commands'] = commands
    

    def add_var_custom_revision(self, editor_version):
        if editor_version == 'CUSTOM-REVISION':
            self._create_or_append_child('variables', 'CUSTOM_REVISION', 'custom_revision_not_set')
    
    def add_var_upm_registry(self):
        self._create_or_append_child('variables', 'UPM_REGISTRY', VAR_UPM_REGISTRY)

    def add_var_custom(self, var_key, var_value): # used by editor. allows to set other variables without cluttering this class
        self._create_or_append_child('variables', var_key, var_value)


    def add_artifacts_test_results(self):
        self._create_or_append_child('artifacts', 'logs', { 'paths' : [dss(PATH_TEST_RESULTS_padded)]})

    def add_artifacts_players(self):
        self._create_or_append_child('artifacts', 'players', { 'paths' : [dss(PATH_PLAYERS)]})

    def add_artifacts_packages(self):
        self._create_or_append_child('artifacts', 'packages', { 'paths' : [dss(PATH_PACKAGES)]})

    def add_artifacts_unity_revision(self): # used by editor
        self._create_or_append_child('artifacts', 'unity_revision.zip', { 'paths': [dss(PATH_UNITY_REVISION)]})


    def _create_or_append_child(self, parent_key, key, value): # adds a child_key under parent_key, without overwriting the entire parent by accident
        if parent_key in self.yml.keys():
            self.yml[parent_key][key] = value
        else:
            self.yml[parent_key] = {key:value}
