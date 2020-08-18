
from .editor_priming import Editor_PrimingJob
from .editor_pinning_merge_to_target import Editor_PinningMergeToTargetJob
from .editor_pinning_merge_from_target import Editor_PinningMergeFromTargetJob
from .editor_pinning_update import Editor_PinningUpdateJob
from ..shared.namer import editor_priming_filepath, editor_pinning_filepath

def create_editor_yml(metafile):

    yml_files = {}

    # editor priming jobs
    yml = {}
    for platform in metafile["platforms"]:
        for editor in metafile['editors']:
            job = Editor_PrimingJob(platform, editor, metafile["editor_priming_agent"])
            yml[job.job_id] = job.yml

    yml_files[editor_priming_filepath()] = yml


    # editor pinning jobs
    yml = {}
    job = Editor_PinningUpdateJob(metafile["editor_pin_agent"], metafile["target_branch"], metafile["target_branch_editor_ci"])
    yml[job.job_id] = job.yml

    job = Editor_PinningMergeToTargetJob(metafile["target_editor"], metafile["editor_pin_agent"], metafile["target_branch"], metafile["target_branch_editor_ci"])
    yml[job.job_id] = job.yml 

    job = Editor_PinningMergeFromTargetJob(metafile["editor_pin_agent"], metafile["target_branch"], metafile["target_branch_editor_ci"])
    yml[job.job_id] = job.yml 

    yml_files[editor_pinning_filepath()] = yml


    return yml_files