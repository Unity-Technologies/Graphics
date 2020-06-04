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
from jobs.preview_publish.pb_publish_all_preview import PreviewPublish_PublishAllPreviewJob
from jobs.preview_publish.pb_wait_for_nightly import PreviewPublish_WaitForNightlyJob
from jobs.templates.template_pack import Template_PackJob
from jobs.templates.template_test import Template_TestJob
from jobs.templates.template_test_dependencies import Template_TestDependenciesJob
from jobs.templates.test_all import Template_AllTemplateCiJob

save_dir = os.path.dirname(os.path.dirname(os.getcwd()))
shared_editors = []
shared_platforms = []
shared_test_platforms = []
shared_agents = []
target_branch = ''

def load_yml(filepath):
    with open(filepath) as f:
        return yaml.load(f)

def dump_yml(filepath, yml_dict):
    with open(os.path.join(save_dir,filepath), 'w') as f:
        yaml.dump(yml_dict, f)

def get_editors(metafile):
    override_editors = metafile.get("override_editors", None)
    return override_editors if override_editors is not None else shared_editors

def get_platform(platform, api=""):
    if platform.get("agent_default") is not None:
        return platform
    else:
        return shared_platforms.get(f'{platform["name"]}_{api}', shared_platforms.get(platform["name"]))

def get_test_platforms(metafile_testplatforms):
    test_platforms = []
    for test_platform_name in metafile_testplatforms:
        test_platforms.append({
            "name": test_platform_name,
            "args": shared_test_platforms[test_platform_name]
        })
    return test_platforms

def get_agent(agent_name):
    return shared_agents[agent_name]

def create_project_specific_jobs(metafile_name):

    metafile = load_yml(metafile_name)
    project = metafile["project"]

    for platform_meta in metafile['platforms']:
        for api in platform_meta['apis'] or [""]:
            platform = get_platform(platform_meta, api)
            yml = {}
            for editor in get_editors(metafile):
                for test_platform in get_test_platforms(metafile['test_platforms']):
                    
                    if test_platform["name"].lower() == 'standalone':
                        if api.lower() != 'openglcore': # skip standalone for openglcore (osx and linux)
                            job = Project_StandaloneJob(project, editor, platform, api, test_platform)
                            yml[job.job_id] = job.yml
                            
                            if job.build_job is not None:
                                yml[job.build_job.job_id] = job.build_job.yml
                    
                    elif platform["name"].lower() not in ["iphone","android"]: # mobile only has standalone
                        job = Project_NotStandaloneJob(project, editor, platform, api, test_platform)
                        yml[job.job_id] = job.yml
                    
            # store yml per [project]-[platform]-[api]
            yml_file = project_filepath_specific(project["name"], platform["name"], api)
            dump_yml(yml_file, yml)



def create_project_all_jobs(metafile_name):

    metafile = load_yml(metafile_name)

    yml = {}
    for editor in get_editors(metafile):
        job = Project_AllJob(metafile["project"]["name"], editor, metafile["all"]["dependencies"])
        yml[job.job_id] = job.yml

    yml_file = project_filepath_all(metafile["project"]["name"])
    dump_yml(yml_file, yml)



def create_editor_job(metafile_name):

    metafile = load_yml(metafile_name)

    yml = {}
    for platform in metafile["platforms"]:
        for editor in get_editors(metafile):
            job = Editor_PrimingJob(platform, editor, get_agent(metafile["agent"]))
            yml[job.job_id] = job.yml

    dump_yml(editor_filepath(), yml)


def create_package_jobs(metafile_name):
    metafile = load_yml(metafile_name)
    yml = {}

    for package in metafile["packages"]:
        job = Package_PackJob(package, get_agent(metafile["agent_pack"]))
        yml[job.job_id] = job.yml

        job = Package_PublishJob(package, get_agent(metafile["agent_publish"]), metafile["platforms"])
        yml[job.job_id] = job.yml

    for editor in get_editors(metafile):
        for plat in metafile["platforms"]:
            platform = plat.copy()
            platform["agent_default"] = get_agent(platform["agent_default"])
            for package in metafile["packages"]:
                job = Package_TestJob(package, platform, editor)
                yml[job.job_id] = job.yml

                job = Package_TestDependenciesJob(package, platform, editor)
                yml[job.job_id] = job.yml

    for editor in get_editors(metafile):
        job = Package_AllPackageCiJob(metafile["packages"], get_agent(metafile["agent_publish"]), metafile["platforms"], editor)
        yml[job.job_id] = job.yml
    
    job = Package_PublishAllJob(metafile["packages"], get_agent(metafile["agent_publish_all"]))
    yml[job.job_id] = job.yml

    dump_yml(packages_filepath(), yml)


def create_abv_jobs(metafile_name):
    metafile = load_yml(metafile_name)
    yml = {}

    metafile["smoke_test"]["agent"] = get_agent(metafile["smoke_test"]["agent"])
    metafile["smoke_test"]["agent_gpu"] = get_agent(metafile["smoke_test"]["agent_gpu"])
    
    for editor in get_editors(metafile):
        for test_platform in get_test_platforms(metafile["smoke_test"]["test_platforms"]):
            job = ABV_SmokeTestJob(editor, test_platform, metafile["smoke_test"])
            yml[job.job_id] = job.yml
        
        job = ABV_AllSmokeTestsJob(editor, get_test_platforms(metafile["smoke_test"]["test_platforms"]))
        yml[job.job_id] = job.yml

        job = ABV_AllProjectCiJob(editor, metafile["abv"]["projects"], metafile["abv"]["trigger_editors"], target_branch)
        yml[job.job_id] = job.yml

        if editor["version"] in metafile["nightly"]["allowed_editors"]:
            job = ABV_AllProjectCiNightlyJob(editor, metafile["abv"]["projects"], get_test_platforms(metafile["smoke_test"]["test_platforms"]), metafile["nightly"], target_branch)
            yml[job.job_id] = job.yml

        job = ABV_TrunkVerificationJob(editor, metafile["trunk_verification"]["dependencies"])
        yml[job.job_id] = job.yml

    dump_yml(abv_filepath(), yml)


def create_preview_publish_jobs(metafile_name):
    metafile = load_yml(metafile_name)
    yml = {}

    job = PreviewPublish_AutoVersionJob(get_agent(metafile["agent_auto_version"]), metafile["packages"], target_branch, metafile["publishing"]["auto_version"])
    yml[job.job_id] = job.yml

    job = PreviewPublish_PublishAllPreviewJob(metafile["packages"], target_branch, metafile["publishing"]["auto_publish"])
    yml[job.job_id] = job.yml

    job = PreviewPublish_WaitForNightlyJob(metafile["packages"],  get_editors(metafile), metafile["platforms"])
    yml[job.job_id] = job.yml

    for package in metafile["packages"]:

        if package["publish_source"] == True:
            job = PreviewPublish_PublishJob(get_agent(metafile["agent_publish"]), package, get_editors(metafile), metafile["platforms"])
            yml[job.job_id] = job.yml

            job = PreviewPublish_PromoteJob(get_agent(metafile["agent_promote"]), package)
            yml[job.job_id] = job.yml

    dump_yml(pb_filepath(), yml)

def create_template_jobs(metafile_name):
    metafile = load_yml(metafile_name)
    yml = {}

    for template in metafile["templates"]:
        job = Template_PackJob(template, get_agent(metafile["agent_pack"]))
        yml[job.job_id] = job.yml


    for editor in get_editors(metafile):
        for plat in metafile["platforms"]:
            platform = plat.copy()
            platform["agent_default"] = get_agent(platform["agent_default"])
            for template in metafile["templates"]:
                job = Template_TestJob(template, platform, editor)
                yml[job.job_id] = job.yml

                job = Template_TestDependenciesJob(template, platform, editor)
                yml[job.job_id] = job.yml

    for editor in get_editors(metafile):
        job = Template_AllTemplateCiJob(metafile["templates"], get_agent(metafile["agent_all_ci"]), metafile["platforms"], editor)
        yml[job.job_id] = job.yml
    

    dump_yml(templates_filepath(), yml)


if __name__== "__main__":

    # configure yaml
    yaml = ruamel.yaml.YAML()
    yaml.width = 4096
    yaml.indent(offset=2, mapping=4, sequence=5)


    # parse shared file
    shared = load_yml('config/__shared.metafile')
    shared_editors = shared['editors']
    shared_platforms = shared['project_platforms']
    shared_test_platforms = shared['test_platforms']
    target_branch = shared['target_branch']
    shared_agents = shared['non_project_agents']

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



