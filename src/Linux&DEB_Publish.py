#!/usr/bin/env python3
# ==============================================================================
# Script de Build et de Packaging DEBIAN pour ScholarLog sous Linux
# ==============================================================================

import os
import shutil
import subprocess
from pathlib import Path

# --- Configuration ---
APP_NAME = "ScholarLog"
VERSION = "1.0.0"
MAINTAINER = "Hofer Lukas <lukas.hofer.ju@gmail.com>"

# Définition des chemins
USER_HOME = str(Path.home()) # Équivalent à /home/$USER/
BUILD_DIR = os.path.join(USER_HOME, "Build_ScholarLog")
SRC_DIR = os.getcwd() # Suppose que le script est lancé depuis la racine du projet
IMAGES_DIR = os.path.join(SRC_DIR, "Assets", "Images")

# Architectures cibles pour Linux
ARCHITECTURES = [
    {"rid": "linux-x64", "deb_arch": "amd64"},
    {"rid": "linux-arm64", "deb_arch": "arm64"}
]

def run_command(cmd, cwd=None):
    """Exécute une commande shell et gère les erreurs."""
    print(f"\033[93mExécution :\033[0m {' '.join(cmd)}")
    try:
        subprocess.run(cmd, check=True, cwd=cwd)
    except subprocess.CalledProcessError as e:
        print(f"\033[91mErreur lors de l'exécution de la commande : {e}\033[0m")
        exit(1)

def write_file(path, content):
    """Écrit du texte dans un fichier en UTF-8."""
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)

def main():
    print(f"\033[96m==================================================\033[0m")
    print(f"\033[96m DÉPLOIEMENT LINUX DEB POUR {APP_NAME} \033[0m")
    print(f"\033[96m==================================================\033[0m")
    print(f"Dossier de build : {BUILD_DIR}\n")

    # Création du dossier de build principal
    os.makedirs(BUILD_DIR, exist_ok=True)

    for arch in ARCHITECTURES:
        rid = arch["rid"]
        deb_arch = arch["deb_arch"]
        out_path = os.path.join(BUILD_DIR, f"dotnet_{rid}")

        print(f"\033[96m\n--- 1. COMPILATION .NET POUR {rid} ---\033[0m")
        dotnet_cmd = [
            "dotnet", "publish", "-c", "Release", "-f", "net10.0",
            "-r", rid, "--self-contained", "true",
            "-p:PublishSingleFile=true",
            "-p:PublishTrimmed=true",
            "-p:PublishReadyToRun=true",
            "-p:IncludeNativeLibrariesForSelfExtract=true",
            "-p:IncludeAllContentForSelfExtract=true",
            "-o", out_path
        ]
        run_command(dotnet_cmd)

        print(f"\033[96m\n--- 2. CRÉATION DU PAQUET DEBIAN ({deb_arch}) ---\033[0m")
        deb_dir_name = f"scholarlog-{deb_arch}"
        deb_dir = os.path.join(BUILD_DIR, deb_dir_name)

        # Nettoyage de l'ancien dossier s'il existe
        if os.path.exists(deb_dir):
            shutil.rmtree(deb_dir)

        # Création de l'arborescence DEB
        dirs_to_create = [
            os.path.join(deb_dir, "usr", "bin"),
            os.path.join(deb_dir, "usr", "share", "applications"),
            os.path.join(deb_dir, "usr", "share", "pixmaps"),
            os.path.join(deb_dir, "DEBIAN")
        ]
        for d in dirs_to_create:
            os.makedirs(d, exist_ok=True)

        # Copie et droits de l'exécutable
        binary_src = os.path.join(out_path, APP_NAME)
        binary_dest = os.path.join(deb_dir, "usr", "bin", APP_NAME)
        shutil.copy2(binary_src, binary_dest)
        os.chmod(binary_dest, 0o755)

        # Copie des images (avec vérification pour éviter un crash si absentes)
        img_png = os.path.join(IMAGES_DIR, "ScholarLog.png")
        img_xpm = os.path.join(IMAGES_DIR, "ScholarLog.xpm")
        if os.path.exists(img_png):
            shutil.copy2(img_png, os.path.join(deb_dir, "usr", "share", "pixmaps", "ScholarLog.png"))
        if os.path.exists(img_xpm):
            shutil.copy2(img_xpm, os.path.join(deb_dir, "usr", "share", "pixmaps", "ScholarLog.xpm"))
        else:
            print("\033[93mAttention : Les images .png ou .xpm n'ont pas été trouvées dans Assets/Images.\033[0m")

        # Fichier Control
        control_content = (
            f"Package: scholarlog\n"
            f"Version: {VERSION}\n"
            f"Section: utils\n"
            f"Priority: optional\n"
            f"Architecture: {deb_arch}\n"
            f"Maintainer: {MAINTAINER}\n"
            f"Description: Logiciel GUI (mono-utilisateur) d'inscription de notes et de journal de travail.\n"
        )
        write_file(os.path.join(deb_dir, "DEBIAN", "control"), control_content)

        # Fichier Desktop
        desktop_content = (
            "[Desktop Entry]\n"
            "Type=Application\n"
            "Name=ScholarLog\n"
            "Comment=Gestionnaire de logs scolaires\n"
            "Exec=ScholarLog\n"
            "Icon=ScholarLog\n"
            "Terminal=false\n"
            "Categories=Education;Utility;\n"
        )
        write_file(os.path.join(deb_dir, "usr", "share", "applications", "scholarlog.desktop"), desktop_content)

        # Permissions strictes requises par dpkg-deb
        os.chmod(os.path.join(deb_dir, "DEBIAN"), 0o755)
        os.chmod(os.path.join(deb_dir, "DEBIAN", "control"), 0o644)

        # Génération du paquet .deb
        dpkg_cmd = ["dpkg-deb", "--root-owner-group", "--build", deb_dir_name]
        run_command(dpkg_cmd, cwd=BUILD_DIR)

        print(f"\033[92m-> Paquet généré : {os.path.join(BUILD_DIR, f'{deb_dir_name}.deb')}\033[0m")

    print(f"\033[92m\n==================================================")
    print(f" DÉPLOIEMENT TERMINÉ AVEC SUCCÈS !")
    print(f" Les fichiers se trouvent dans : {BUILD_DIR}")
    print(f"==================================================\033[0m")

if __name__ == "__main__":
    main()
