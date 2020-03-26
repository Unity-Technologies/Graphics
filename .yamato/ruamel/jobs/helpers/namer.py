def file_path(project_name, platform_name, api_name):
    return f'.yamato/{project_name}/{project_name}-{platform_name}-{api_name}.yml'.lower()

def file_path_all(project_name):
    return f'.yamato/{project_name}/{project_name}-all.yml'.lower()