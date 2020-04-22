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

    def set_trigger_on_expression(self, expression): # TODO this is exclusive, one trigger cancelles another atm
        self.yml['triggers'] = {'expression':expression}

    def set_trigger_recurrent(self, branch, frequency): # TODO this is exclusive, one trigger cancelles another atm
        self.yml['triggers'] = {'recurring': [{
                    'branch' : branch,
                    'frequency' : frequency}]}
    
    def set_trigger_integration_branch(self, integration_branch): # TODO this is exclusive, one trigger cancelles another atm
        self.yml['triggers'] = {'branches': {'only' : [integration_branch]}}
    
    def add_dependencies(self, dependencies):
        self._populate_list('dependencies', dependencies)
    
    def add_commands(self, commands):
        self._populate_list('commands', commands)

    def add_var_custom_revision(self, editor_version):
        if editor_version == 'CUSTOM-REVISION':
            self._populate_child_key('variables', 'CUSTOM_REVISION', 'custom_revision_not_set')
    
    def add_var_upm_registry(self):
        self._populate_child_key('variables', 'UPM_REGISTRY', VAR_UPM_REGISTRY)

    def add_var_custom(self, var_key, var_value): # used by editor. allows to set other variables without cluttering this class
        self._populate_child_key('variables', var_key, var_value)


    def add_artifacts_test_results(self):
        self._populate_child_key('artifacts', 'logs', { 'paths' : [dss(PATH_TEST_RESULTS_padded)]})

    def add_artifacts_players(self):
        self._populate_child_key('artifacts', 'players', { 'paths' : [dss(PATH_PLAYERS)]})

    def add_artifacts_packages(self):
        self._populate_child_key('artifacts', 'packages', { 'paths' : [dss(PATH_PACKAGES)]})

    def add_artifacts_unity_revision(self): # used by editor
        self._populate_child_key('artifacts', 'unity_revision.zip', { 'paths': [dss(PATH_UNITY_REVISION)]})


    def _populate_child_key(self, parent_key, child_key, child_value): # adds a child_key under parent_key, without overwriting the entire parent by accident
        if parent_key in self.yml.keys():
            self.yml[parent_key][child_key] = child_value
        else:
            self.yml[parent_key] = {child_key:child_value}

    def _populate_list(self, key, values): # creates new list or appends to existing one under the key
        if key in self.yml.keys():
            self.yml[key].extend(values)
        else:
            self.yml[key] = values
