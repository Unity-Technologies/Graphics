def add_custom_revision_var(job, editor_version):
    if editor_version == 'CUSTOM-REVISION':
        if 'variables' in job.keys():
            job['variables']['CUSTOM_REVISION'] = 'custom_revision_not_set'
        else:
            job['variables'] = {'CUSTOM_REVISION':'custom_revision_not_set'}
    return job