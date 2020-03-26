import ruamel
from models import project_all as pa
from models import project_test as pt
from models.helpers.namer import file_path, file_path_all
import sys, glob

# TODO generate job names/ids

def create_yml_jobs(metafile):
    project = metafile["project"]

    for platform in metafile['platforms']:
        for api in platform['apis']:
            yml = {}
            for editor in metafile['editors']:

                job_id_playmode = f'{project["name"]}_{platform["name"]}_{api["name"]}_playmode_{editor["version"]}'   
                yml[job_id_playmode] = pt.project_playmode(project, editor, platform, api)

                job_id_editmode = f'{project["name"]}_{platform["name"]}_{api["name"]}_editmode_{editor["version"]}'   
                yml[job_id_editmode] = pt.project_editmode(project, editor, platform, api)

                job_id_standalone = f'{project["name"]}_{platform["name"]}_{api["name"]}_Standalone_{editor["version"]}'    
                yml[job_id_standalone] = pt.project_standalone(project, editor, platform, api)
                        
                    # create build player job for when standalone uses split build
                if platform["standalone_split"]: # TODO check for better way to do it 
                    job_id_standalone_build = f'Build_{project["name"]}_{platform["name"]}_{api["name"]}_Player_{editor["version"]}'
                    yml[job_id_standalone_build] = pt.project_standalone_build(project, editor, platform, api)
                
            # store yml per [project]-[platform]-[api]
            yml_file= file_path(project["name"], platform["name"], api["name"])
            with open(yml_file, 'w') as f:
                yaml.dump(yml, f) 



def create_yml_all(metafile):
    
    project_name = metafile["project"]["name"]
    dependencies_in_all = metafile["dependencies_in_all"]

    yml = {}
    for editor in metafile['editors']:
        
        job_id = f'All_{project_name}_{editor["version"]}'
        
        yml[job_id] = pa.project_all(project_name, editor, dependencies_in_all)
    

    yml_file = file_path_all(project_name)
    with open(yml_file, 'w') as f:
        yaml.dump(yml, f) 



if __name__== "__main__":
    
    # configure yaml
    yaml = ruamel.yaml.YAML()
    yaml.width = 4096
    yaml.indent(offset=2, mapping=4, sequence=5)
    
    # create yml for each specified project (universal, shadergraph, vfx_lwrp, ...)
    args = sys.argv
    if 'all' in args:
        project_config_files = glob.glob('config/*.metafile')
    else:
        project_config_files = [f'config/{project}.metafile' for project in args[1:]]

    print(f'Running: {project_config_files}')
    for project in project_config_files:
        
        with open(project) as f:
            metafile = yaml.load(f)
        
        create_yml_jobs(metafile) # create jobs for testplatforms
        create_yml_all(metafile) # create All_ job



