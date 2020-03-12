import ruamel
from models import job_builder as jb
from models.helpers import name_builder as nb

yaml = ruamel.yaml.YAML()
yaml.width = 4096
yaml.indent(offset=2, mapping=4, sequence=5)

with open('upm-ci-universal.metafile') as f:
    metafile = yaml.load(f)

for project in metafile['projects']:
    for platform in metafile['platforms']:
        for api in platform['apis']:
            
            yml = {}
            
            for test_platform in metafile['testplatforms']:
                for editor in metafile['editors']:
                    
                    # create test job for every testplatform
                    job_id_test = nb.get_job_id_test(project, editor, platform, test_platform, api)
                    yml[job_id_test] = jb.project_test(project, editor, platform, test_platform, api) 
                    
                    # create build player job for when standalone uses split build
                    if test_platform["name"].lower() == "standalone" and platform["standalone_split"]: # TODO check for better way to do it 
                        job_id_build = nb.get_job_id_build(project, editor, platform, test_platform, api)
                        yml[job_id_build] = jb.project_build(project, editor, platform, test_platform, api)
            
            # store yml per [project]-[platform]-[api]
            yml_file = nb.get_yml_name(project, platform, api)
            with open(f'.yamato/{yml_file}'.lower(), 'w') as f:
                yaml.dump(yml, f) 
