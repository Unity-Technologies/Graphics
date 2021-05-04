import argparse
import os
from os import path
from shutil import copyfile

from git import Repo, exc

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Takes the output from shadergraph's image tests and commits them "
                                                 "back to the source repo")
    parser.add_argument("--root")

    args = parser.parse_args()

    repo = Repo(".\\")

    repo.git.config(['--global', 'user.name', 'Jessica Thomson'])
    repo.git.config(['--global', 'user.email', 'jessica.thomson@unity3d.com'])

    branch_name = os.getenv("GIT_BRANCH")
    if branch_name is None:  # Local run, we can use the branch name
        branch_name = repo.active_branch
    repo.git.stash()
    new_branch_name = branch_name + "-ref-images"
    repo.create_head(new_branch_name)
    repo.git.checkout(new_branch_name)
    try:
        repo.git.stash("pop")
    except exc.GitCommandError:
        pass

    editor = ""
    update_tests_file_path = path.join(args.root, "Assets/Resources/UpdateTests.txt")
    if not path.exists(update_tests_file_path):
        print("No update file found, skipping recommit")
        print(os.getcwd())
        for file in os.listdir("C:\\build\\output\\Unity-Technologies\\Graphics\\TestProjects\\ShaderGraph"):
            print(file)
        exit(0)
    with open(update_tests_file_path) as f:
        while True:
            line = f.readline().strip()
            if line == "":
                break
            test_name, asset_path, should_update_image = line.split(",")
            _, _, colorspace, editor, platform, vr, testname, testasset = asset_path.split("/")

            if should_update_image == "True":
                actual_img_path = path.join(os.getcwd(), args.root, "Assets", "ActualImages",
                                            colorspace, editor, platform, vr, test_name + ".png")
                reference_img_path = path.join(os.getcwd(), args.root, "Assets", "ReferenceImages",
                                               colorspace, editor, platform, vr, test_name)
                copyfile(actual_img_path, reference_img_path + ".png")
                repo.git.add(reference_img_path + ".png")
                asset_meta_dir_path = path.join(args.root, asset_path.rsplit("/", 1)[0])
                repo.git.add(asset_meta_dir_path + ".meta")
                if path.exists(reference_img_path + ".png.meta"):
                    repo.index.add([reference_img_path + ".png.meta"])  # Doesn't seem to always exist, so we check

            repo.git.add(path.join(args.root, asset_path))
            repo.git.add(path.join(args.root, asset_path + ".meta"))
            print(asset_path + " Added")
            full_asset_path = path.join(os.getcwd(), args.root, asset_path)

    repo.git.commit("-m", "Generated reference images for " + editor)
    repo.remote(name="origin").push(["--set-upstream", new_branch_name])
