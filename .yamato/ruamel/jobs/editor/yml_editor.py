
from .editor_priming import Editor_PrimingJob
from .editor_pinning_merge_all import Editor_PinningMergeAllJob
from .editor_priming_min_editor import Editor_PrimingMinEditorJob
from .editor_pinning_merge_revisions import Editor_PinningMergeRevisionsJob
from .editor_pinning_target_to_ci import Editor_PinningTargetToCIJob
from .editor_pinning_update import Editor_PinningUpdateJob
from ..shared.namer import editor_priming_filepath, editor_pinning_filepath

def create_editor_yml(metafile):

    yml_files = {}

    #### editor priming jobs
    yml = {}
    for platform in metafile["platforms"]:
        for editor in metafile['editors']:
            # only create editor priming jobs for editor configs which dont use editor_pinning:
            # creating them causes no harm, but we just may end up with priming jobs with exactly the same content but different id, causing confusion of what's the difference
            if editor['editor_pinning']: 
                continue

            job = Editor_PrimingJob(platform, editor, metafile["editor_priming_agent"])
            yml[job.job_id] = job.yml
        
        job = Editor_PrimingMinEditorJob(platform, metafile["editor_priming_agent"])
        yml[job.job_id] = job.yml

    yml_files[editor_priming_filepath()] = yml


    #### editor pinning jobs
    if any([editor['editor_pinning'] for editor in metafile['editors']]): # only enter this if there exists an track that needs editor pinning
        
        yml = {}

        # sync job
        job = Editor_PinningTargetToCIJob(metafile["editor_pin_agent"], metafile["target_branch"], metafile["target_branch_editor_ci"])
        yml[job.job_id] = job.yml 

        job = Editor_PinningUpdateJob(metafile["editor_pin_agent"], metafile["target_branch"], metafile["target_branch_editor_ci"])
        yml[job.job_id] = job.yml

        for editor in metafile['editors']:
            if not editor['editor_pinning']:
                continue
            
            # no ci/abv 
            job = Editor_PinningMergeRevisionsJob(editor, metafile["editor_pin_agent"], metafile["target_branch"], metafile["target_branch_editor_ci"], abv=False)
            yml[job.job_id] = job.yml 
                
            # ci + abv flow
            job = Editor_PinningMergeRevisionsJob(editor, metafile["editor_pin_agent"], metafile["target_branch"], metafile["target_branch_editor_ci"], abv=True)
            yml[job.job_id] = job.yml  

        # no ci/abv
        job = Editor_PinningMergeAllJob(metafile['editors'], metafile["editor_pin_agent"], metafile["target_branch"], metafile["target_branch_editor_ci"], abv=False)
        yml[job.job_id] = job.yml

        # ci + abv
        job = Editor_PinningMergeAllJob(metafile['editors'], metafile["editor_pin_agent"], metafile["target_branch"], metafile["target_branch_editor_ci"], abv=True)
        yml[job.job_id] = job.yml

        yml_files[editor_pinning_filepath()] = yml


    return yml_files