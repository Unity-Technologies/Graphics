
parent_dir = '.yamato'

# editor specific 
def editor_filepath():
    return f'{parent_dir}/_editor.yml'.lower()

def editor_job_id(editor_version, platform_os):
    return f'editor:priming:{editor_version}:{platform_os}'

# package specific
def packages_filepath():
    return f'{parent_dir}/packages.yml'.lower()

def package_job_id_test(package_id, platform_name, editor_version):
    return f'test_{ package_id }_{ platform_name }_{editor_version}'

def package_job_id_test_dependencies(package_id, platform_name, editor_version):
    return f'test_{ package_id }_{ platform_name }_{editor_version}_dependencies'

def package_job_id_pack(package_id):
    return f'pack_{package_id}'

def package_job_id_publish(package_id):
    return f'publish_{package_id}'

def package_job_id_publish_all():
    return f'publish_all'

def package_job_id_test_all(editor_version):
    return f'all_package_ci_{editor_version}'


# project specific
def project_filepath_specific(project_name, platform_name, api_name):
    return f'{parent_dir}/{project_name}/{project_name}-{platform_name}-{api_name}.yml'.lower()

def project_filepath_all(project_name):
    return f'{parent_dir}/{project_name}/{project_name}-all.yml'.lower()

def project_job_id_test(project_name, platform_name, api_name, test_platform_name, editor_version):
    return f'{project_name}_{platform_name}_{api_name}_{test_platform_name}_{editor_version}'

def project_job_id_build(project_name, platform_name, api_name, editor_version):
    return f'Build_{project_name}_{platform_name}_{api_name}_Player_{editor_version}'

def project_job_id_all(project_name, editor_version):
    return f'All_{project_name}_{ editor_version}'