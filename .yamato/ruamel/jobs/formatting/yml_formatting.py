from ..shared.namer import formatting_filepath
from jobs.formatting.formatting import Formatting_Job

def create_formatting_ymls(metafile):
    yml_files = {}
    yml = {}

    job = Formatting_Job(metafile)
    yml[job.job_id] = job.yml

    yml_files[formatting_filepath()] = yml
    return yml_files
