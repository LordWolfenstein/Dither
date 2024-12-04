from shutil import copy
from sys import argv
from winreg import HKEY_CURRENT_USER, CloseKey, OpenKey, QueryValueEx

dll_file = argv[1]
key = OpenKey(HKEY_CURRENT_USER, r"Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders")
my_documents, _ = QueryValueEx(key, "Personal")
CloseKey(key)
detination_folder = my_documents + "\\paint.net App Files\\Effects"
copy(dll_file, detination_folder)

print()
print(f"Successfully copied {dll_file} to {detination_folder}")
print()
