from shutil import copy
from sys import argv
from winreg import HKEY_CURRENT_USER, CloseKey, OpenKey, QueryValueEx

dll_file = argv[1]
key = OpenKey(HKEY_CURRENT_USER, r"Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders")
my_documents, _ = QueryValueEx(key, "Personal")
CloseKey(key)
effects_folder = my_documents + "\\paint.net App Files\\Effects"
palette_folder = my_documents + "\\paint.net App Files\\Palettes"
copy(dll_file, effects_folder)
copy("./Basic EGA.txt", palette_folder)
copy("./Full EGA.txt", palette_folder)

print()
print(f"Successfully copied {dll_file} to {effects_folder} and EGA palettes to {palette_folder}")
print()
