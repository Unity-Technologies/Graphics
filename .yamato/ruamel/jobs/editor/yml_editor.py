
from .editor_priming import Editor_PrimingJob
from .editor_priming_min_editor import Editor_PrimingMinEditorJob
from .editor_pinning_merge_revisions import Editor_PinningMergeRevisionsJob
from .editor_pinning_merge_revisions_abv import Editor_PinningMergeRevisionsABVJob
from .editor_pinning_target_to_ci import Editor_PinningTargetToCIJob
from .editor_pinning_update import Editor_PinningUpdateJob
from ..shared.namer import editor_priming_filepath, editor_pinning_filepath

def create_editor_yml(metafile):

    yml_files = {}

    yml = {}
    for platform in metafile["platforms"]:
        for editor in metafile['editors']:
            job = Editor_PrimingJob(platform, editor, metafile["agent"])
            yml[job.job_id] = job.yml
        
        job = Editor_PrimingMinEditorJob(platform, metafile["editor_priming_agent"])
        yml[job.job_id] = job.yml

    yml_files[editor_filepath()] = yml
    return yml_files