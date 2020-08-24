from ..shared.namer import projectcontext_filepath
from .project_pack import Project_PackJob
from .project_publish import Project_PublishJob
from .project_test import Project_TestJob
from .project_publish_all import Project_PublishAllJob
from .project_publish_all_tag import Project_PublishAllTagJob
from .project_test_all import Project_AllPackageCiJob
from .project_publish_dry import Project_PublishJob_DryRun

def create_projectcontext_ymls(metafile):

    yml_files = {}
    yml = {}

    job = Project_PackJob(metafile["agent_pack"])
    yml[job.job_id] = job.yml
    for package in metafile["packages"]:
        job = Project_PublishJob(package, metafile["agent_publish"], metafile["platforms"], metafile["target_editor"])
        yml[job.job_id] = job.yml

        job = Project_PublishJob_DryRun(package, metafile["agent_publish"], metafile["platforms"], metafile["target_editor"])
        yml[job.job_id] = job.yml

    for editor in metafile["editors"]:
        for platform in metafile["platforms"]:
            job = Project_TestJob(platform, editor)
            yml[job.job_id] = job.yml
    
    for editor in metafile['editors']:
        job = Project_AllPackageCiJob(metafile["packages"], metafile["agent_publish"], metafile["platforms"], metafile["target_editor"], metafile["target_branch"], editor)
        yml[job.job_id] = job.yml

    job = Project_PublishAllJob(metafile["packages"], metafile["target_branch"], metafile["agent_publish_all"])
    yml[job.job_id] = job.yml

    job = Project_PublishAllTagJob(metafile["packages"], metafile["target_branch"], metafile["agent_publish_all"])
    yml[job.job_id] = job.yml

    yml_files[projectcontext_filepath()] = yml
    return yml_files