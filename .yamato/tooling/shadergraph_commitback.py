import platform
import os
import subprocess
from os import path
from shutil import copyfile
import sys
from git import Repo, exc, Head

if __name__ == "__main__":

    root = sys.argv[1]
    utr = sys.argv[2:]

    repo = Repo(".\\")

    repo.git.config(['--global', 'user.name', 'Jessica Thomson'])
    repo.git.config(['--global', 'user.email', 'jessica.thomson@unity3d.com'])

    branch_name = os.getenv("GIT_BRANCH")
    if branch_name is None:  # Local run, we can use the branch name
        branch_name = repo.active_branch
    repo.git.stash()
    if isinstance(branch_name, Head):
        new_branch_name = branch_name.name + "-ref-images"
    else:
        new_branch_name = branch_name + "-ref-images"
    repo.create_head(new_branch_name)
    repo.git.checkout(new_branch_name)
    try:
        repo.git.stash("pop")
    except exc.GitCommandError:
        pass

    editor = ""
    update_tests_file_path = path.join(root, "Assets/Resources/UpdateTests.txt")
    if not path.exists(update_tests_file_path):
        print("No update file found, skipping recommit")
        print(os.getcwd())
        for file in os.listdir("C:\\build\\output\\Unity-Technologies\\Graphics\\TestProjects\\ShaderGraph"):
            print(file)
        # Skip the additional run if there's nothing to re-commit
        exit(0)
    with open(update_tests_file_path) as f:
        while True:
            line = f.readline().strip()
            if line == "":
                break
            test_name, asset_path, should_update_image = line.split(",")
            _, _, colorspace, editor, test_platform, vr, testname, testasset = asset_path.split("/")

            if should_update_image == "True":
                actual_img_path = path.join(os.getcwd(), root, "Assets", "ActualImages",
                                            colorspace, editor, test_platform, vr, test_name + ".png")
                reference_img_path = path.join(os.getcwd(), root, "Assets", "ReferenceImages",
                                               colorspace, editor, test_platform, vr, test_name)
                copyfile(actual_img_path, reference_img_path + ".png")
                repo.git.add(reference_img_path + ".png")
                asset_meta_dir_path = path.join(root, asset_path.rsplit("/", 1)[0])
                repo.git.add(asset_meta_dir_path + ".meta")
                if path.exists(reference_img_path + ".png.meta"):
                    repo.index.add([reference_img_path + ".png.meta"])  # Doesn't seem to always exist, so we check

            repo.git.add(path.join(root, asset_path))
            repo.git.add(path.join(root, asset_path + ".meta"))
            print(asset_path + " Added")
            full_asset_path = path.join(os.getcwd(), root, asset_path)

    repo.git.commit("-m", "Generated reference images for " + editor)
    repo.remote(name="origin").push(["--set-upstream", new_branch_name])

    utr_file_name = ""
    if platform.system() == 'Windows':
        utr_file_name = "utr.bat"
    else:
        utr_file_name = "utr"
    utr.insert(0, utr_file_name)
    print("Calling utr with arguments: \n" + " ".join(utr))
    exit(subprocess.call(utr))
