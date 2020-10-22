
parent_dir = '.yamato'

# editor specific 
def editor_priming_filepath():
    return f'{parent_dir}/_editor_priming.yml'.lower()

def editor_pinning_filepath():
    return f'{parent_dir}/_editor_pinning.yml'.lower()

def editor_job_id(editor_name, platform_os):
    return f'editor:priming:{editor_name}:{platform_os}'

def editor_job_id_test_min_editor(platform_os):
    return f'editor:priming:test_min_editor:{platform_os}'

def editor_job_id_update():
    return 'editor-pinning-update'

def editor_job_id_target_to_ci():
    return 'editor-pinning-target-to-ci'

def editor_job_id_merge_revisions(editor_track, abv):
    return f'editor-pinning-merge-revisions-{editor_track}-abv' if abv else f'editor-pinning-merge-revisions-{editor_track}' 

def editor_job_id_merge_all(abv):
    return f'editor-pinning-merge-all-abv' if abv else f'editor-pinning-merge-all'

# package specific
def packages_filepath():
    return f'{parent_dir}/_packages.yml'.lower()

def package_job_id_test(package_id, platform_os, editor_name):
    return f'test_{ package_id }_{ platform_os }_{editor_name}'

def package_job_id_test_dependencies(package_id, platform_os, editor_name):
    return f'test_{ package_id }_{ platform_os }_{editor_name}_dependencies'

def package_job_id_pack(package_id):
    return f'pack_{package_id}'

def package_job_id_publish(package_id):
    return f'publish_{package_id}'

def package_job_id_publish_dry(package_id):
    return f'publish_{package_id}_dry'

def package_job_id_publish_all():
    return f'publish_all'

def package_job_id_publish_all_tag():
    return f'publish_all_tag'

def package_job_id_test_all(editor_name):
    return f'all_package_ci_{editor_name}'

def projectcontext_filepath():
    return f'{parent_dir}/_projectcontext.yml'.lower()

def projectcontext_job_id_pack():
    return f'pack_all_project'

def projectcontext_job_id_test(platform_os, editor_name):
    return f'test_all_project_{ platform_os }_{editor_name}'

def projectcontext_job_id_test_min_editor(platform_os):
    return f'test_all_project_{ platform_os }_min_editor'

def projectcontext_job_id_publish(package_id):
    return f'publish_{package_id}_project'

def projectcontext_job_id_publish_dry(package_id):
    return f'publish_{package_id}_project_dry'

def projectcontext_job_id_publish_all():
    return f'publish_all_project'

def projectcontext_job_id_publish_all_tag():
    return f'publish_all_project_tag'

def projectcontext_job_id_test_all(editor_name):
    return f'all_package_ci_project_{editor_name}'

def pb_projectcontext_job_id_promote(package_name):
    return f'promote_{package_name}_project'

def pb_projectcontext_job_id_promote_dry(package_name):
    return f'promote_{package_name}_project_dry'

def pb_projectcontext_job_id_promote_all_preview():
    return f'promote_all_preview_project'

# template specific
def templates_filepath():
    return f'{parent_dir}/_templates.yml'.lower()

def template_job_id_test(template_id, platform_os, editor_name):
    return f'test_{ template_id }_{ platform_os }_{editor_name}'

def template_job_id_test_dependencies(template_id, platform_os, editor_name):
    return f'test_{ template_id }_{ platform_os }_{editor_name}_dependencies'

def template_job_id_pack(template_id):
    return f'pack_{template_id}'

def template_job_id_test_all(editor_name):
    return f'all_template_ci_{editor_name}'

# project specific
def project_filepath_specific(project_name, platform_name, api_name):
    # return f'{parent_dir}/{project_name}/{project_name}-{platform_name}-{api_name}.yml'.lower().replace('-.','.')
    return f'{parent_dir}/{project_name}-{platform_name}-{api_name}.yml'.lower().replace('-.','.')

def project_filepath_all(project_name):
    # return f'{parent_dir}/{project_name}/all-{project_name}.yml'.lower()
    return f'{parent_dir}/all-{project_name}.yml'.lower()

def project_job_id_test(project_name, platform_name, api_name, test_platform_name, editor_name, scripting_backend, color_space):
    return f'{project_name}_{platform_name}_{api_name}_{test_platform_name}_{scripting_backend}_{color_space}_{editor_name}'.replace('__','_')

def project_job_id_build(project_name, platform_name, api_name, editor_name, scripting_backend, color_space):
    return f'Build_{project_name}_{platform_name}_{api_name}_Player_{scripting_backend}_{color_space}_{editor_name}'.replace('__','_')

def project_job_id_all(project_name, editor_name):
    return f'All_{project_name}_{ editor_name}'


# abv specific
def abv_filepath():
    return f'{parent_dir}/_abv.yml'.lower()

def abv_job_id_all_project_ci(editor_name):
    return f'all_project_ci_{editor_name}'

def abv_job_id_all_project_ci_nightly(editor_name):
    return f'all_project_ci_nightly_{editor_name}'

def abv_job_id_all_project_ci_weekly(editor_name):
    return f'all_project_ci_weekly_{editor_name}'

def abv_job_id_trunk_verification(editor_name):
    return f'trunk_verification_{editor_name}'


# preview publish specific
def pb_filepath():
    return f'{parent_dir}/_preview_publish.yml'.lower()

def pb_job_id_auto_version():
    return 'auto-version'

def pb_job_id_publish(package_name):
    return f'publish_{package_name}'

def pb_job_id_promote(package_name):
    return f'promote_{package_name}'

def pb_job_id_promote_dry(package_name):
    return f'promote_{package_name}_dry'

def pb_job_id_wait_for_nightly():
    return f'wait_for_nightly'

def pb_job_id_publish_all_preview():
    return f'publish_all_preview'

def pb_job_id_promote_all_preview():
    return f'promote_all_preview'