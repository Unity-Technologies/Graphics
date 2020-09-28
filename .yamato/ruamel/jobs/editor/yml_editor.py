from .editor import Editor_PrimingJob
from ..shared.namer import editor_filepath
from .editor_priming_min_editor import Editor_PrimingMinEditorJob

def create_editor_yml(metafile):

    yml_files = {}

    yml = {}
    for platform in metafile["platforms"]:
        for editor in metafile['editors']:
            job = Editor_PrimingJob(platform, editor, metafile["agent"])
            yml[job.job_id] = job.yml
        
        job = Editor_PrimingMinEditorJob(platform, metafile["agent"])
        yml[job.job_id] = job.yml

    yml_files[editor_filepath()] = yml
    return yml_files