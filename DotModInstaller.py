# Compile with python -m nuitka --onefile --windows-icon-from-ico=DMEXE.ico DotModInstaller.py

# Imports
# File path checking
import os

# File tree copying and removal
import shutil

# Config (This installer is meant to be universal, for both the main mod and addons)
import configparser

# Parsing Steam's libraryfolders.vdf to find games on secondary drives
import re


# Steam always stores the game in a folder named "Cold Waters"
GAME_FOLDER = "Cold Waters"


def steam_library_roots():
    """Return likely Steam install roots across platforms.

    Covers Windows drives, native Linux Steam (including the Flatpak and the
    Debian/Ubuntu installation layouts) and macOS. Also reads each root's
    libraryfolders.vdf so games installed on secondary drives are picked up.
    Forward slashes are used throughout; Windows accepts them as well.
    """
    home = os.path.expanduser("~")
    candidates = [
        # Windows
        "C:/Program Files (x86)/Steam",
        "D:/Steam",
        "D:/Games/Steam",
        "D:/SteamLibrary",
        "E:/SteamLibrary",
        # Linux (native, Flatpak and distro-specific layouts)
        f"{home}/.steam/steam",
        f"{home}/.steam/root",
        f"{home}/.steam/debian-installation",
        f"{home}/.local/share/Steam",
        f"{home}/.var/app/com.valvesoftware.Steam/.local/share/Steam",
        # macOS
        f"{home}/Library/Application Support/Steam",
    ]

    roots = list(candidates)
    for root in candidates:
        for vdf in (
            f"{root}/steamapps/libraryfolders.vdf",
            f"{root}/config/libraryfolders.vdf",
        ):
            if os.path.exists(vdf):
                try:
                    with open(vdf, "r", encoding="utf-8", errors="ignore") as handle:
                        for match in re.findall(r'"path"\s+"([^"]+)"', handle.read()):
                            # VDF stores Windows paths with escaped backslashes
                            roots.append(match.replace("\\\\", "/").replace("\\", "/"))
                except OSError:
                    pass

    return roots


def find_install_directory():
    """Auto-locate the Cold Waters install across Windows, Linux and macOS."""
    seen = set()
    for root in steam_library_roots():
        path = f"{root}/steamapps/common/{GAME_FOLDER}"
        if path in seen:
            continue
        seen.add(path)
        if os.path.isdir(path):
            return path
    return None


# Good programming practices
def main():
    config = configparser.ConfigParser()
    config.read("%s/Installer.ini" % (os.getcwd()))
    mod_name = config["Settings"]["mod_name"]
    mod = "%s/ColdWaters_Data" % (os.getcwd())

    # Auto-locate; checks likely Steam locations on Windows, Linux and macOS
    install_directory = find_install_directory()

    if install_directory is None:
        print("Auto-locate failed.")
        install_directory = input(
            "Please input the directory of your Cold Waters install: "
        )

    else:
        print(f"Found Cold Waters at {install_directory}.")

    # Make sure everything's correct:
    if (
        input(
            f"This will install {mod_name} to {install_directory}. \nIs that correct? Y/N: "
        ).lower()
        != "y"
    ):
        install_directory = input(
            "Please input the directory of your Cold Waters install: "
        )

    mod_target = install_directory + f"/MODS/{mod_name}/ColdWaters_Data"

    if mod_name == "DotMod":
        # Welcome/Install instructions for the main mod
        print(
            "\nReminder: Only download DotMod from the GitHub page. Any other downloads are *unauthorized* and cannot "
            "be verified."
        )
        print(
            "If you did not download the mod's files from the GitHub, close this program at once and run a malware "
            "scan."
        )
        input("Press Return to continue. ")
        print(
            "\nThank you for choosing DotMod! This program will help you automatically install/update the mod."
        )
        print(
            "If you have JSGME installed, disable all mods currently enabled, and make sure the game is closed."
        )

        # Install JSGME
        JSGME = "%s/JSGME" % (os.getcwd())
        if os.path.exists(f"{install_directory}/JSGME.ini"):
            print("JSGME already installed.")

        else:
            shutil.copytree(JSGME, install_directory, dirs_exist_ok=True)
            print(
                "JSGME installed. Please read through JSGME Help.txt and JoneSoft.txt."
            )

        # Remove the old files
        if os.path.exists(f"{install_directory}/MODS/{mod_name}"):
            shutil.rmtree(f"{install_directory}/MODS/{mod_name}", ignore_errors=True)
            print("Previous version of the mod removed.")

    # Install the mod
    print("Working, please wait...\nThis may take some time.")
    shutil.copytree(mod, mod_target, dirs_exist_ok=True)
    print("Mod installed.")

    # And we're done!
    if mod_name == "DotMod":
        print(
            f"\nThis program is completed. Now, navigate to your Cold Waters directory, at {install_directory}, and "
            f"run JSGME.exe (if you used Epic Mod, this may be called Epic Mod Install) and enable the main mod "
            f"there."
        )
        print("Enable the main mod first, and then any addons you want over it.")
        print("Thank you for using DotMod.")
        input("Press Return to exit.")
    else:
        print(
            f"\nThis program is completed. Now, navigate to your Cold Waters directory, at {install_directory}, and "
            f"run JSGME.exe (if you used Epic Mod, this may be called Epic Mod Install) and enable this addon there."
        )
        print(
            "The installation of this addon is complete. If you experience any crashes, please remove all addons and "
            "re-enable them."
        )
        print("Please contact the addon's creator if the issue persists.")
        input("Press Return to exit.")


if __name__ == "__main__":
    main()
