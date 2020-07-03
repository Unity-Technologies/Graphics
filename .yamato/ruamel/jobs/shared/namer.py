
parent_dir = '.yamato'

# editor specific 
def editor_filepath():
    return f'{parent_dir}/_editor.yml'.lower()

def editor_job_id(editor_version, platform_os):
    return f'editor:priming:{editor_version}:{platform_os}'

# package specific
def packages_filepath():
    return f'{parent_dir}/_packages.yml'.lower()

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


# template specific
def templates_filepath():
    return f'{parent_dir}/_templates.yml'.lower()

def template_job_id_test(template_id, platform_name, editor_version):
    return f'test_{ template_id }_{ platform_name }_{editor_version}'

def template_job_id_test_dependencies(template_id, platform_name, editor_version):
    return f'test_{ template_id }_{ platform_name }_{editor_version}_dependencies'

def template_job_id_pack(template_id):
    return f'pack_{template_id}'

def template_job_id_test_all(editor_version):
    return f'all_template_ci_{editor_version}'

# project specific
def project_filepath_specific(project_name, platform_name, api_name):
    # return f'{parent_dir}/{project_name}/{project_name}-{platform_name}-{api_name}.yml'.lower().replace('-.','.')
    return f'{parent_dir}/{project_name}-{platform_name}-{api_name}.yml'.lower().replace('-.','.')

def project_filepath_all(project_name):
    # return f'{parent_dir}/{project_name}/all-{project_name}.yml'.lower()
    return f'{parent_dir}/all-{project_name}.yml'.lower()

def project_job_id_test(project_name, platform_name, api_name, test_platform_name, editor_version):
    return f'{project_name}_{platform_name}_{api_name}_{test_platform_name}_{editor_version}'.replace('__','_')

def project_job_id_build(project_name, platform_name, api_name, editor_version):
    return f'Build_{project_name}_{platform_name}_{api_name}_Player_{editor_version}'.replace('__','_')

def project_job_id_all(project_name, editor_version):
    return f'All_{project_name}_{ editor_version}'


# abv specific
def abv_filepath():
    return f'{parent_dir}/_abv.yml'.lower()

def abv_job_id_all_project_ci(editor_version):
    return f'all_project_ci_{editor_version}'

def abv_job_id_all_project_ci_nightly(editor_version):
    return f'all_project_ci_nightly_{editor_version}'

def abv_job_id_smoke_test(editor_version, test_platform_name):
    return f'smoke_test_{test_platform_name}_{editor_version}'

def abv_job_id_all_smoke_tests(editor_version):
    return f'all_smoke_tests_{editor_version}'

def abv_job_id_trunk_verification(editor_version):
    return f'trunk_verification_{editor_version}'


# preview publish specific
def pb_filepath():
    return f'{parent_dir}/_preview_publish.yml'.lower()

def pb_job_id_auto_version():
    return 'auto-version'

def pb_job_id_publish(package_name):
    return f'publish_{package_name}'

def pb_job_id_promote(package_name):
    return f'promote_{package_name}'

def pb_job_id_wait_for_nightly():
    return f'wait_for_nightly'

def pb_job_id_publish_all_preview():
    return f'publish_all_preview'

def pb_job_id_promote_all_preview():
    return f'promote_all_preview'