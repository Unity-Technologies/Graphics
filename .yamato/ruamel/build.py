import sys, glob, os
import ruamel
from jobs.shared.namer import *
from jobs.projects.project_standalone import Project_StandaloneJob
from jobs.projects.project_standalone_build import Project_StandaloneBuildJob
from jobs.projects.project_not_standalone import Project_NotStandaloneJob
from jobs.projects.project_all import Project_AllJob
from jobs.editor.editor import Editor_PrimingJob
from jobs.packages.package_pack import Package_PackJob
from jobs.packages.package_publish import Package_PublishJob
from jobs.packages.package_test import Package_TestJob
from jobs.packages.package_test_dependencies import Package_TestDependenciesJob
from jobs.packages.publish_all import Package_PublishAllJob
from jobs.packages.test_all import Package_AllPackageCiJob
from jobs.abv.all_project_ci import ABV_AllProjectCiJob
from jobs.abv.all_project_ci_nightly import ABV_AllProjectCiNightlyJob
from jobs.abv.all_smoke_tests import ABV_AllSmokeTestsJob
from jobs.abv.smoke_test import ABV_SmokeTestJob
from jobs.abv.trunk_verification import ABV_TrunkVerificationJob
from jobs.preview_publish.pb_publish import PreviewPublish_PublishJob
from jobs.preview_publish.pb_promote import PreviewPublish_PromoteJob
from jobs.preview_publish.pb_auto_version import PreviewPublish_AutoVersionJob
from jobs.templates.template_pack import Template_PackJob
from jobs.templates.template_test import Template_TestJob
from jobs.templates.template_test_dependencies import Template_TestDependenciesJob
from jobs.templates.test_all import Template_AllTemplateCiJob

save_dir = os.path.dirname(os.path.dirname(os.getcwd()))

def load_yml(filepath):
    with open(filepath) as f:
        return yaml.load(f)

def dump_yml(filepath, yml_dict):
    
    with open(os.path.join(save_dir,filepath), 'w') as f:
        yaml.dump(yml_dict, f)

def create_project_specific_jobs(metafile_name):

    metafile = load_yml(metafile_name)
    project = metafile["project"]

    for platform in metafile['platforms']:
        for api in platform['apis']:

            yml = {}
            for editor in metafile['editors']:
                for test_platform in metafile['test_platforms']:

                    if test_platform["name"].lower() == 'standalone':
                        if api["name"].lower() != 'openglcore': # skip standalone for openglcore (osx and linux)
                            job = Project_StandaloneJob(project, editor, platform, api, test_platform)
                            yml[job.job_id] = job.yml
                            
                            if job.build_job is not None:
                                yml[job.build_job.job_id] = job.build_job.yml
                    
                    elif platform["name"].lower() != "android": # android only has standalone, so run this block only for != android
                        job = Project_NotStandaloneJob(project, editor, platform, api, test_platform)
                        yml[job.job_id] = job.yml
                    
            # store yml per [project]-[platform]-[api]
            yml_file = project_filepath_specific(project["name"], platform["name"], api["name"])
            dump_yml(yml_file, yml)



def create_project_all_jobs(metafile_name):

    metafile = load_yml(metafile_name)

    yml = {}
    for editor in metafile['editors']:
        job = Project_AllJob(metafile["project"]["name"], editor, metafile["all"]["dependencies"])
        yml[job.job_id] = job.yml

    yml_file = project_filepath_all(metafile["project"]["name"])
    dump_yml(yml_file, yml)



def create_editor_job(metafile_name):

    metafile = load_yml(metafile_name)

    yml = {}
    for platform in metafile["platforms"]:
        for editor in metafile["editors"]:
            job = Editor_PrimingJob(platform, editor, metafile["agent"])
            yml[job.job_id] = job.yml

    dump_yml(editor_filepath(), yml)


def create_package_jobs(metafile_name):
    metafile = load_yml(metafile_name)
    yml = {}

    for package in metafile["packages"]:
        job = Package_PackJob(package, metafile["agent_win"])
        yml[job.job_id] = job.yml

        job = Package_PublishJob(package, metafile["agent_win"], metafile["platforms"])
        yml[job.job_id] = job.yml

    for editor in metafile["editors"]:
        for platform in metafile["platforms"]:
            for package in metafile["packages"]:
                job = Package_TestJob(package, platform, editor)
                yml[job.job_id] = job.yml

                job = Package_TestDependenciesJob(package, platform, editor)
                yml[job.job_id] = job.yml

    for editor in metafile["editors"]:
        job = Package_AllPackageCiJob(metafile["packages"], metafile["agent_win"], metafile["platforms"], editor)
        yml[job.job_id] = job.yml
    
    job = Package_PublishAllJob(metafile["packages"], metafile["agent_ubuntu"])
    yml[job.job_id] = job.yml

    dump_yml(packages_filepath(), yml)


def create_abv_jobs(metafile_name):
    metafile = load_yml(metafile_name)
    yml = {}

    for editor in metafile["editors"]:
        for test_platform in metafile['test_platforms']:
            job = ABV_SmokeTestJob(editor, test_platform, metafile["smoke_test"])
            yml[job.job_id] = job.yml
        
        job = ABV_AllSmokeTestsJob(editor, metafile["test_platforms"])
        yml[job.job_id] = job.yml

        job = ABV_AllProjectCiJob(editor, metafile["projects"], metafile["abv_config"]["trigger_editors"])
        yml[job.job_id] = job.yml

        if editor["version"] in metafile["nightly_config"]["allowed_editors"]:
            job = ABV_AllProjectCiNightlyJob(editor, metafile["projects"], metafile["test_platforms"], metafile["nightly_config"])
            yml[job.job_id] = job.yml

        job = ABV_TrunkVerificationJob(editor, metafile["projects"], metafile["test_platforms"])
        yml[job.job_id] = job.yml

    dump_yml(abv_filepath(), yml)


def create_preview_publish_jobs(metafile_name):
    metafile = load_yml(metafile_name)
    yml = {}

    job = PreviewPublish_AutoVersionJob(metafile["agent_ubuntu"], metafile["packages"],  metafile["integration_branch"], metafile["publishing"]["auto_version"])
    yml[job.job_id] = job.yml

    for package in metafile["packages"]:
        if package["publish_source"] == True:
            job = PreviewPublish_PublishJob(metafile["agent_win"], package, metafile["integration_branch"], metafile["publishing"]["auto_publish"], metafile["editors"], metafile["platforms"])
            yml[job.job_id] = job.yml

            job = PreviewPublish_PromoteJob(metafile["agent_win"], package)
            yml[job.job_id] = job.yml

    dump_yml(pb_filepath(), yml)

def create_template_jobs(metafile_name):
    metafile = load_yml(metafile_name)
    yml = {}

    for template in metafile["templates"]:
        job = Template_PackJob(template, metafile["agent_win"])
        yml[job.job_id] = job.yml


    for editor in metafile["editors"]:
        for platform in metafile["platforms"]:
            for template in metafile["templates"]:
                job = Template_TestJob(template, platform, editor)
                yml[job.job_id] = job.yml

                job = Template_TestDependenciesJob(template, platform, editor)
                yml[job.job_id] = job.yml

    for editor in metafile["editors"]:
        job = Template_AllTemplateCiJob(metafile["templates"], metafile["agent_win"], metafile["platforms"], editor)
        yml[job.job_id] = job.yml
    

    dump_yml(templates_filepath(), yml)


if __name__== "__main__":

    # configure yaml
    yaml = ruamel.yaml.YAML()
    yaml.width = 4096
    yaml.indent(offset=2, mapping=4, sequence=5)


    # clear directory from existing yml files, not to have old duplicates etc
    print(save_dir)
    old_yml_files = glob.glob(f'{save_dir}/.yamato/**/*.yml', recursive=True)
    for f in old_yml_files:
        os.remove(f)

    # create editor
    print(f'Running: editor')
    create_editor_job('config/_editor.metafile')

    # create package jobs
    print(f'Running: packages')
    create_package_jobs('config/_packages.metafile')

    # create abv
    print(f'Running: abv')
    create_abv_jobs('config/_abv.metafile')

    # create preview publish
    print(f'Running: preview_publish')
    create_preview_publish_jobs('config/_preview_publish.metafile')

     # create template jobs
    print(f'Running: templates')
    create_template_jobs('config/_templates.metafile')

    # create yml jobs for each specified project
    for project_metafile in glob.glob('config/[!_]*.metafile'):
        print(f'Running: {project_metafile}')
        create_project_specific_jobs(project_metafile) # create jobs for testplatforms
        create_project_all_jobs(project_metafile) # create All_ job


