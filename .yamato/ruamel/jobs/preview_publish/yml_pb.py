
from ..shared.namer import pb_filepath
from .pb_promote import PreviewPublish_PromoteJob
from .pb_promote_dry import PreviewPublish_PromoteDryJob
from .pb_auto_version import PreviewPublish_AutoVersionJob
from .pb_promote_all_preview import PreviewPublish_PromoteAllPreviewJob
from .pb_wait_for_nightly import PreviewPublish_WaitForNightlyJob
from .pb_promote_project import PreviewPublish_ProjectContext_PromoteJob
from .pb_promote_all_preview_project import PreviewPublish_ProjectContext_PromoteAllPreviewJob
from .pb_promote_project_dry import PreviewPublish_ProjectContext_PromoteJob_DryRun

def create_preview_publish_ymls(metafile):
    
    yml_files = {}
    yml = {}

    job = PreviewPublish_AutoVersionJob(metafile["agent_auto_version"], metafile["packages"], metafile["target_branch"], metafile["publishing"]["auto_version"])
    yml[job.job_id] = job.yml

    job = PreviewPublish_PromoteAllPreviewJob(metafile["packages"], metafile["target_branch"], metafile["publishing"]["auto_publish"])
    yml[job.job_id] = job.yml

    job = PreviewPublish_ProjectContext_PromoteAllPreviewJob(metafile["packages"], metafile["target_branch"], metafile["publishing"]["auto_publish"])
    yml[job.job_id] = job.yml

    job = PreviewPublish_WaitForNightlyJob(metafile["packages"],  metafile["platforms"], metafile["target_editor"])
    yml[job.job_id] = job.yml

    for package in metafile["packages"]:

        if package["publish_source"] == True:

            job = PreviewPublish_PromoteJob(metafile["agent_promote"], package,  metafile["platforms"], metafile["target_editor"])
            yml[job.job_id] = job.yml

            job = PreviewPublish_ProjectContext_PromoteJob(metafile["agent_promote"], package,  metafile["platforms"], metafile["target_editor"])
            yml[job.job_id] = job.yml

            job = PreviewPublish_ProjectContext_PromoteJob_DryRun(metafile["agent_promote"], package,  metafile["platforms"], metafile["target_editor"])
            yml[job.job_id] = job.yml

            job = PreviewPublish_PromoteDryJob(metafile["agent_promote"], package,  metafile["platforms"], metafile["target_editor"])
            yml[job.job_id] = job.yml

    yml_files[pb_filepath()] = yml
    return yml_files
