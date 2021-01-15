from .project_not_standalone import Project_NotStandaloneJob
from .project_standalone import Project_StandaloneJob
from ..shared.namer import project_filepath_specific
from .project_pr import Project_PRJob
from .project_nightly import Project_NightlyJob
from ..shared.namer import project_filepath_all

def create_project_ymls(metafile):

    yml_files = {}
    
    # project_all yml file
    yml = {}
    for editor in metafile['editors']:
        expression = ""
        if metafile["expression_trigger"]["expression"] != "":
            expression = metafile["expression_trigger"]["expression"]
        job = Project_PRJob(metafile["project"], editor, expression, metafile["pr"]["dependencies"])
        yml[job.job_id] = job.yml

        job = Project_NightlyJob(metafile["project"], editor, metafile["nightly"]["dependencies"])
        yml[job.job_id] = job.yml

    yml_file = project_filepath_all(metafile["project"]["name"])
    yml_files[yml_file] = yml

    #     # project_all yml file
    # nightly_yml = {}
    # for editor in metafile['editors']:
    #     job = Project_NightlyJob(metafile["project"]["name"], editor, metafile["nightly"]["dependencies"])
    #     nightly_yml[job.job_id] = job.nightly_yml

    # yml_file = project_filepath_nightly(metafile["project"]["name"])
    # yml_files[yml_file] = nightly_yml

    # project platform_api specific yml files
    project = metafile["project"]
    for platform in metafile['platforms']:
        for api in platform['apis'] or [""]:
            
            yml = {}
            for editor in metafile['editors']:
                for build_config in platform['build_configs']:
                    for color_space in platform['color_spaces']:
                        for test_platform in metafile['test_platforms']:
                            
                            exclude_tp = [tp["name"] for tp in api.get('exclude_test_platforms', [])]
                            if test_platform['name'].lower() not in map(str.lower, exclude_tp):

                                if test_platform['type'].lower() == 'standalone':
                                    job = Project_StandaloneJob(project, editor, platform, api, test_platform, build_config, color_space)
                                    yml[job.job_id] = job.yml
                                        
                                    if job.build_job is not None:
                                        yml[job.build_job.job_id] = job.build_job.yml
                                
                                else: 
                                    job = Project_NotStandaloneJob(project, editor, platform, api, test_platform, build_config, color_space)
                                yml[job.job_id] = job.yml
                    
            # store yml per [project]-[platform]-[api]
            yml_file = project_filepath_specific(project["name"], platform["name"], api["name"])
            yml_files[yml_file] = yml

    return yml_files