from ..shared.namer import abv_filepath
from jobs.abv.abv_all_project_ci import ABV_AllProjectCiJob
from jobs.abv.abv_all_project_ci_nightly import ABV_AllProjectCiNightlyJob
from jobs.abv.abv_all_smoke_tests import ABV_AllSmokeTestsJob
from jobs.abv.abv_smoke_test import ABV_SmokeTestJob
from jobs.abv.abv_trunk_verification import ABV_TrunkVerificationJob

def create_abv_ymls(metafile):
    yml_files = {}
    yml = {}
    
    for editor in metafile["editors"]:
        for test_platform in metafile["smoke_test"]["test_platforms"]:
            job = ABV_SmokeTestJob(editor, test_platform, metafile["smoke_test"])
            yml[job.job_id] = job.yml
        
        job = ABV_AllSmokeTestsJob(editor, metafile["smoke_test"]["test_platforms"])
        yml[job.job_id] = job.yml

        job = ABV_AllProjectCiJob(editor, metafile["abv"]["projects"], metafile["abv"]["trigger_editors"], metafile["target_branch"])
        yml[job.job_id] = job.yml

        if editor["track"] in metafile["nightly"]["allowed_editors"]:
            job = ABV_AllProjectCiNightlyJob(editor, metafile["abv"]["projects"], metafile["smoke_test"]["test_platforms"], metafile["nightly"], metafile["target_branch"])
            yml[job.job_id] = job.yml

        job = ABV_TrunkVerificationJob(editor, metafile["trunk_verification"]["dependencies"])
        yml[job.job_id] = job.yml

    yml_files[abv_filepath()] = yml
    return yml_files