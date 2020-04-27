import os, glob
from distutils.dir_util import copy_tree

if __name__== "__main__":

    # Copies all directory contents of source into destination, while preserving the original structure of the source folder.
    # Used to get the jobs to show up in Yamato (because it only sees yml files in the root of .yamato/)
    
    # TODO Once Yamato sees .yml files under subdirectories, we should move all files belonging to one project under a subfolder for this project. 
    # (e.g. move .yamato/shadergraph-all.yml under .yamato/shadergraph/shadergraph-all.yml etc)
    # This can be done by modifying project_filepath_specific() and project_filepath_all() under jobs.shared.namer
    # Ideally, then we dont need this copy-script anymore too.

    
    source_dir = '.yamato' # the .yamato folder into which 
    destination_dir = os.path.dirname(os.getcwd()) # returns parent directory of this file (the root .yamato/)


    # clear directory from existing yml files, not to have old duplicates etc
    old_yml_files = glob.glob(f'{destination_dir}/*.yml')
    for f in old_yml_files:
        os.remove(f)


    # copy fresh yml files for Yamato
    copy_tree(source_dir, destination_dir)