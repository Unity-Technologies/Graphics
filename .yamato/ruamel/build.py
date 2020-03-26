import ruamel
from models import project_all as pa
from models import project_test as pt
import sys

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
            yml_file = f'{project["name"]}/upm-ci-{project["name"]}-{platform["name"]}-{api["name"]}.yml'.lower()
            with open(f'.yamato/{yml_file}'.lower(), 'w') as f:
                yaml.dump(yml, f) 



def create_yml_all(metafile):
    
    project_name = metafile["project"]["name"]
    dependencies_in_all = metafile["dependencies_in_all"]

    yml = {}
    for editor in metafile['editors']:
        
        job_id = f'All_{project_name}_{editor["version"]}'
        
        yml[job_id] = pa.project_all(project_name, editor, dependencies_in_all)
    

    yml_file = f'{project_name}/upm-ci-{project_name}-all.yml'.lower()
    with open(f'.yamato/{yml_file}'.lower(), 'w') as f:
        yaml.dump(yml, f) 



if __name__== "__main__":
    
    # configure yaml
    yaml = ruamel.yaml.YAML()
    yaml.width = 4096
    yaml.indent(offset=2, mapping=4, sequence=5)
    
    # create yml for each specified project (universal, shadergraph, vfx_lwrp, ...)
    project_names = sys.argv[1:]
    for project in project_names:
        
        with open(f'config/upm-ci-{project}.metafile') as f:
            metafile = yaml.load(f)
        
        create_yml_jobs(metafile) # create jobs for testplatforms
        create_yml_all(metafile) # create All_ job



