rem assume we start from Tools folder
cd..

rename %1 package
7z a -ttar %1-%2.tar package
7z a -tgzip %1-%2.tgz %1-%2.tar
del %1-%2.tar
rename package %1

move %1-%2.tgz .\Tools\%1-%2.tgz

cd Tools