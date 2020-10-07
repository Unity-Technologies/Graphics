from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import *
from ..shared.yml_job import YMLJob
from ..shared.constants import NPM_UPMCI_INSTALL_URL

class Template_AllTemplateCiJob():
    
    def __init__(self, templates, agent, platforms, editor):
        self.job_id = template_job_id_test_all(editor["track"])
        self.yml = self.get_job_definition(templates, agent, platforms, editor).get_yml()


    def get_job_definition(self, templates, agent, platforms, editor):

        # define dependencies
        dependencies = []
        for platform in platforms:
            for template in templates:
                dependencies.append(f'{templates_filepath()}#{template_job_id_test(template["id"],platform["os"],editor["track"])}')
                dependencies.append(f'{templates_filepath()}#{template_job_id_test_dependencies(template["id"],platform["os"],editor["track"])}')
        
        # construct job
        job = YMLJob()
        job.set_name(f'Pack and test all templates - { editor["track"] }')
        job.set_agent(agent)
        job.add_dependencies(dependencies)
        job.add_commands([
                f'npm install upm-ci-utils@stable -g --registry {NPM_UPMCI_INSTALL_URL}',
                f'upm-ci package izon -t',
                f'upm-ci package izon -d'])
        return job
        
    