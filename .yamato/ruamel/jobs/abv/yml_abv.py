from ..shared.namer import abv_filepath
from jobs.abv.abv_all_project_ci import ABV_AllProjectCiJob
from jobs.abv.abv_all_project_ci_nightly import ABV_AllProjectCiNightlyJob
from jobs.abv.abv_all_project_ci_weekly import ABV_AllProjectCiWeeklyJob
from jobs.abv.abv_trunk_verification import ABV_TrunkVerificationJob

def create_abv_ymls(metafile):
    yml_files = {}
    yml = {}
    
    for editor in metafile["editors"]:
        job = ABV_AllProjectCiJob(editor, metafile["abv"]["projects"],metafile["target_branch"])
        yml[job.job_id] = job.yml

        if editor.get("nightly"):
            job = ABV_AllProjectCiNightlyJob(editor, metafile["abv"]["projects"], metafile["nightly"], metafile["target_branch"], metafile["abv"]["build_configs"], metafile["abv"]["color_spaces"])
            yml[job.job_id] = job.yml
        
        if editor.get("weekly"):
            job = ABV_AllProjectCiWeeklyJob(editor, metafile["abv"]["projects"], metafile["weekly"], metafile["target_branch"], metafile["abv"]["build_configs"], metafile["abv"]["color_spaces"])
            yml[job.job_id] = job.yml

        job = ABV_TrunkVerificationJob(editor, metafile["trunk_verification"]["dependencies"])
        yml[job.job_id] = job.yml

    yml_files[abv_filepath()] = yml
    return yml_files