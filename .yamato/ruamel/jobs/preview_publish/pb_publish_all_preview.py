from ruamel.yaml.scalarstring import DoubleQuotedScalarString as dss
from ..shared.namer import *
from ..shared.yml_job import YMLJob

class PreviewPublish_PublishAllPreviewJob():
    
    def __init__(self, packages, target_branch, auto_publish):
        self.job_id = pb_job_id_publish_all_preview()
        self.yml = self.get_job_definition(packages, target_branch, auto_publish).get_yml()


    def get_job_definition(self, packages, target_branch, auto_publish):

        # construct job
        job = YMLJob()
        job.set_name(f'Publish all preview packages - nightly')
        job.add_dependencies([f'{pb_filepath()}#{pb_job_id_publish(package["name"])}' for package in packages])
        if auto_publish is True:
            job.add_trigger_recurrent(target_branch, 'daily')
        return job
    
    