import ruamel.yaml
from collections import OrderedDict
yaml = ruamel.yaml.YAML()

def load_yml(filepath):
    '''Returns either yml content of a file, or an empty dict {} if file is empty'''
    with open(filepath) as f:
        yml_body = yaml.load(f)
        return yml_body if yml_body else {}

def ordereddict_to_dict(d):
    '''Handles dumping nested dictionaries'''
    return {k: ordereddict_to_dict(v) for k, v in d.items()} if isinstance(d, OrderedDict) else d
