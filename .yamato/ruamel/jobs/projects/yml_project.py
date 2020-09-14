from .project_not_standalone import Project_NotStandaloneJob
from .project_standalone import Project_StandaloneJob
from ..shared.namer import project_filepath_specific
from .project_all import Project_AllJob
from ..shared.namer import project_filepath_all


def create_project_ymls(metafile):

    yml_files = {}
    
    # project_all yml file
    yml = {}
    for editor in metafile['editors']:
        job = Project_AllJob(metafile["project"]["name"], editor, metafile["all"]["dependencies"])
        yml[job.job_id] = job.yml

    yml_file = project_filepath_all(metafile["project"]["name"])
    yml_files[yml_file] = yml

    # project platform_api specific yml files
    project = metafile["project"]
    for platform in metafile['platforms']:
        for api in platform['apis'] or [""]:
            if platform["name"]=='Android':
                m=5
            yml = {}
            for editor in metafile['editors']:
                for test_platform in metafile['test_platforms']:

                    if test_platform['name'].lower() not in map(str.lower, api.get('exclude_test_platforms', [])):

                        if test_platform['name'].lower() == 'standalone':
                            job = Project_StandaloneJob(project, editor, platform, api, test_platform)
                            yml[job.job_id] = job.yml
                                
                            if job.build_job is not None:
                                yml[job.build_job.job_id] = job.build_job.yml
                        
                        else: 
                            job = Project_NotStandaloneJob(project, editor, platform, api, test_platform)
                            yml[job.job_id] = job.yml
                    
            # store yml per [project]-[platform]-[api]
            yml_file = project_filepath_specific(project["name"], platform["name"], api["name"])
            yml_files[yml_file] = yml

    return yml_files