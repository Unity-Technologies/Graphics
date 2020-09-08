from jobs.templates.template_pack import Template_PackJob
from jobs.templates.template_test import Template_TestJob
from jobs.templates.template_test_dependencies import Template_TestDependenciesJob
from jobs.templates.test_all import Template_AllTemplateCiJob
from ..shared.namer import templates_filepath


def create_template_ymls(metafile):
    yml_files = {}
    
    yml = {}

    for template in metafile["templates"]:
        job = Template_PackJob(template, metafile["agent_pack"])
        yml[job.job_id] = job.yml

    for editor in metafile['editors']:
        for platform in metafile["platforms"]:
            for template in metafile["templates"]:
                job = Template_TestJob(template, platform, editor)
                yml[job.job_id] = job.yml

                job = Template_TestDependenciesJob(template, platform, editor)
                yml[job.job_id] = job.yml

    for editor in metafile['editors']:
        job = Template_AllTemplateCiJob(metafile["templates"], metafile["agent_all_ci"], metafile["platforms"], editor)
        yml[job.job_id] = job.yml
    

    yml_files[templates_filepath()] = yml
    return yml_files