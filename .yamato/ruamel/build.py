import ruamel
from jobs import project_all as pa
from jobs import project_test as pt
from jobs import editor_priming as ep
from jobs.helpers.namer import file_path, file_path_all
import sys, glob

# TODO check for cleaner way
test_platform_jobs = {
    'playmode': pt.project_not_standalone,
    'playmode XR': pt.project_not_standalone, 
    'editmode': pt.project_not_standalone,
    'Standalone': pt.project_standalone,
}

def load_yml(filepath):
    with open(filepath) as f:
        return yaml.load(f)

def dump_yml(filepath, yml_dict):
    with open(filepath, 'w') as f:
        yaml.dump(yml_dict, f)

def create_yml_jobs(project_metafile):

    metafile = load_yml(project_metafile)
    project = metafile["project"]

    for platform in metafile['platforms']:
        for api in platform['apis']:

            yml = {}
            for editor in metafile['editors']:
                for test_platform in metafile['test_platforms']:

                    job_id = f'{project["name"]}_{platform["name"]}_{api["name"]}_{test_platform["name"]}_{editor["version"]}'
                    yml[job_id] = test_platform_jobs[test_platform["name"]](project, editor, platform, api, test_platform)

                    # create build player job for when standalone uses split build
                    if test_platform["name"].lower == 'standalone' and platform["standalone_split"]: # TODO check for better way to do it
                        job_id = f'Build_{project["name"]}_{platform["name"]}_{api["name"]}_Player_{editor["version"]}'
                        yml[job_id] = pt.project_standalone_build(project, editor, platform, api)

            # store yml per [project]-[platform]-[api]
            yml_file = file_path(project["name"], platform["name"], api["name"])
            dump_yml(yml_file, yml)



def create_yml_all(project_metafile):

    metafile = load_yml(project_metafile)
    project_name = metafile["project"]["name"]

    yml = {}
    for editor in metafile['editors']:
        job_id = f'All_{project_name}_{editor["version"]}'
        yml[job_id] = pa.project_all(project_name, editor, metafile["dependencies_in_all"])

    yml_file = file_path_all(project_name)
    dump_yml(yml_file, yml)



def create_yml_editor(editor_metafile):

    metafile = load_yml(editor_metafile)

    yml = {}
    for platform in metafile["platforms"]:
        for editor in metafile["editors"]:
            job_id = f'editor:priming:{editor["version"]}:{platform["os"]}'
            yml[job_id] = ep.editor(platform, editor)

    dump_yml('.yamato/z_editor.yml', yml)



# TODO clean up the code, make filenames more readable/reuse, split things appropriately (eg editor, files, etc), fix script arguments, fix testplatforms (xr), ...
if __name__== "__main__":

    # configure yaml
    yaml = ruamel.yaml.YAML()
    yaml.width = 4096
    yaml.indent(offset=2, mapping=4, sequence=5)


    # create editor
    create_yml_editor('config/z_editor.metafile')


    # create yml jobs for each specified project (universal, shadergraph, vfx_lwrp, ...)
    args = sys.argv
    projects = glob.glob('config/[!z_]*.metafile') if 'all' in args else [f'config/{project}.metafile' for project in args[1:]]   
    print(f'Running: {projects}')

    for project_metafile in projects:
        create_yml_jobs(project_metafile) # create jobs for testplatforms
        create_yml_all(project_metafile) # create All_ job



