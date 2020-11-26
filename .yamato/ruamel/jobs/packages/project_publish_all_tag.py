from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import projectcontext_filepath, projectcontext_job_id_publish, projectcontext_job_id_publish_all_tag
from ..shared.yml_job import YMLJob

class Project_PublishAllTagJob():
    
    def __init__(self, packages, target_branch, agent):
        self.job_id = projectcontext_job_id_publish_all_tag()
        self.yml = self.get_job_definition(packages, target_branch, agent).get_yml()


    def get_job_definition(self, packages, target_branch, agent):

        # construct job
        job = YMLJob()
        job.set_name(f'Publish all packages [project context][manual][tag]')
        job.set_agent(agent)
        job.add_dependencies([f'{projectcontext_filepath()}#{projectcontext_job_id_publish(package["id"])}' for package in packages])
        job.add_commands([
                f'git tag v$(cd com.unity.render-pipelines.core && node -e "console.log(require(\'./package.json\').version)")',
                f'git push origin --tags'])
        return job


    
    
    