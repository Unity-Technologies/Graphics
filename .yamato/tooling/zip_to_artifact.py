from zipfile import ZipFile
import argparse
from os import path, getcwd

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--artifacts", help="The relative directory from root artifacts are stored")
    parser.add_argument("--root", help="The project root")

    args = parser.parse_args()

    print("Working directory of zip_to_artifact: " + getcwd())

    update_tests_file_path = path.join(args.root, "Assets/Resources/UpdateTests.txt")
    if not path.exists(update_tests_file_path):
        print("No update file found")
        exit(1)
    with ZipFile(path.join(args.artifacts, "UpdatedTestData.zip"), 'w') as z:
        with open(update_tests_file_path) as f:
            while True:
                line = f.readline().strip()
                if line == "":
                    print("Finished adding files")
                    break
                test_name, asset_path, should_update_image = line.split(",")
                _, _, colorspace, editor, test_platform, vr, _, test_asset = asset_path.split("/")

                if should_update_image == "True":
                    actual_img_path = path.join(getcwd(), args.root, "Assets", "ActualImages",
                                                colorspace, editor, test_platform, vr, test_name + ".png")
                    reference_img_path = path.join(getcwd(), args.root, "Assets", "ReferenceImages",
                                                   colorspace, editor, test_platform, vr, test_name)
                    z.write(actual_img_path, reference_img_path)
                    if path.exists(actual_img_path + ".meta"):
                        z.write(actual_img_path + ".meta", reference_img_path + ".meta")

