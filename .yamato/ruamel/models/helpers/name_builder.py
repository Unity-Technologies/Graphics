def get_job_name_test(project, editor, platform, test_platform, api):
    return f'{ project["name"] } on {platform["name"]}_{api["name"]}_{test_platform["name"]} on version {editor["version"]}' 

def get_job_name_build(project, editor, platform, test_platform, api):
    return f'Build { project["name"] } on {platform["name"]}_{api["name"]}_Player on version {editor["version"]}' 



def get_job_id_test(project, editor, platform, test_platform, api):
    return f'{project["name"]}_{platform["name"]}_{api["name"]}_{test_platform["name"]}_{editor["version"]}' 

def get_job_id_build(project, editor, platform, test_platform, api):
    return f'Build_{project["name"]}_{platform["name"]}_{api["name"]}_Player_{editor["version"]}'



def get_yml_name(project, platform, api):
    return f'upm-ci-{project["name"]}-{platform["name"]}-{api["name"]}.yml'.lower()