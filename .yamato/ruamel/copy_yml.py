import os
from distutils.dir_util import copy_tree

if __name__== "__main__":

    # Copies all directory contents of source into destination, while preserving the original structure of the source folder.
    # Used to get the jobs to show up in Yamato (because it only sees yml files in the root of .yamato/)
    
    # TODO Once Yamato sees .yml files under subdirectories, we should move all files belonging to one project under a subfolder for this project. 
    # (e.g. move .yamato/shadergraph-all.yml under .yamato/shadergraph/shadergraph-all.yml etc)
    # This can be done by modifying project_filepath_specific() and project_filepath_all() under jobs.shared.namer
    # Ideally, then we dont need this copy-script anymore too.
    source_dir = '.yamato'
    destination_dir = os.path.dirname(os.getcwd())
    copy_tree(source_dir, destination_dir)