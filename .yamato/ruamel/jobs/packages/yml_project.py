from ..shared.namer import projectcontext_filepath
from .project_pack import Project_PackJob
from .project_publish import Project_PublishJob
from .project_test import Project_TestJob


def create_projectcontext_ymls(metafile):

    yml_files = {}
    yml = {}

    job = Project_PackJob(metafile["agent_pack"])
    yml[job.job_id] = job.yml
    for package in metafile["packages"]:
        job = Project_PublishJob(package, metafile["agent_publish"], metafile["platforms"], metafile["target_editor"])
        yml[job.job_id] = job.yml

    for editor in metafile["editors"]:
        for platform in metafile["platforms"]:
            job = Project_TestJob(platform, editor)
            yml[job.job_id] = job.yml


    yml_files[projectcontext_filepath()] = yml
    return yml_files