import ruamel
from models import job_builder as jb
import sys

def create_yml(metafile):
    project = metafile["project"]

    for platform in metafile['platforms']:
        for api in platform['apis']:
                
            yml = {}
            for editor in metafile['editors']:

                job_id_playmode = f'{project["name"]}_{platform["name"]}_{api["name"]}_playmode_{editor["version"]}'   
                yml[job_id_playmode] = jb.project_playmode(project, editor, platform, api)

                job_id_editmode = f'{project["name"]}_{platform["name"]}_{api["name"]}_editmode_{editor["version"]}'   
                yml[job_id_editmode] = jb.project_editmode(project, editor, platform, api)

                job_id_standalone = f'{project["name"]}_{platform["name"]}_{api["name"]}_Standalone_{editor["version"]}'    
                yml[job_id_standalone] = jb.project_standalone(project, editor, platform, api)
                        
                    # create build player job for when standalone uses split build
                if platform["standalone_split"]: # TODO check for better way to do it 
                    job_id_standalone_build = f'Build_{project["name"]}_{platform["name"]}_{api["name"]}_Player_{editor["version"]}'
                    yml[job_id_standalone_build] = jb.project_standalone_build(project, editor, platform, api)
                
            # store yml per [project]-[platform]-[api]
            yml_file = f'{project["name"]}/upm-ci-{project["name"]}-{platform["name"]}-{api["name"]}.yml'.lower()
            with open(f'.yamato/{yml_file}'.lower(), 'w') as f:
                yaml.dump(yml, f) 


if __name__== "__main__":
    
    yaml = ruamel.yaml.YAML()
    yaml.width = 4096
    yaml.indent(offset=2, mapping=4, sequence=5)
    
    project_names = sys.argv[1:]
    for project in project_names:
        with open(f'config/upm-ci-{project}.metafile') as f:
            metafile = yaml.load(f)
        create_yml(metafile)



