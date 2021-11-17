import os
from zipfile import ZipFile
import argparse
from os import path, getcwd, listdir

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--artifacts", help="The relative directory from root artifacts are stored")
    parser.add_argument("--root", help="The project root")
    parser.add_argument("--logs", help="Print extra logs during execution")

    args = parser.parse_args()

    extra_logs = parser.logs

    if extra_logs:
        print("Working directory of zip_to_artifact: " + getcwd() + "\n\n")
        print("Contents of Graphics dir: ")
        print(os.listdir(getcwd()))

    # Should live at the execution root
    update_tests_file_path = "UpdateTests.txt"
    if not path.exists(update_tests_file_path):
        print("No update file found, exiting zip script")
        exit(0)

    if extra_logs:
        with open(update_tests_file_path) as f:
            for line in f.readline():
                print(f)
    with ZipFile(path.join(args.artifacts, "UpdatedTestData.zip"), 'w') as z:
        z.write(update_tests_file_path)
        with open(update_tests_file_path) as f:
            while True:
                line = f.readline().strip()
                if line == "":
                    if extra_logs:
                        print("Hit end of UpdateTests.txt")
                    break

                # Strip curly braces for serialized write from player
                line.replace('{', '')
                line.replace('}', '')
                test_name, asset_path, should_update_image = line.split(",")
                _, _, colorspace, editor, test_platform, vr, _, test_asset = asset_path.split("/")

                if should_update_image == "True":
                    actual_img_path = path.join(getcwd(), args.root, "Assets", "ActualImages",
                                                colorspace, editor, test_platform, vr, test_name + ".png")
                    reference_img_path = path.join(getcwd(), args.root, "Assets", "ReferenceImages",
                                                   colorspace, editor, test_platform, vr, test_name + ".png")
                    test_asset_path = path.join(getcwd(), args.root, "Assets", "Testing", "IntegrationTests",
                                                "ShaderGraphTestAssets", test_name + ".asset")
                    if extra_logs:
                        print("Adding " + test_name)
                    z.write(actual_img_path, reference_img_path)  # Write ActualImage to zip
                    if path.exists(actual_img_path + ".meta"):
                        z.write(actual_img_path + ".meta", reference_img_path + ".meta")
                elif extra_logs:
                    print(test_name + " doesn't need to be updated")
