1. Installez flatpak et flatpak-builder : sudo apt update && sudo apt install -y flatpak flatpak-builder
2. Ajoutez le dépôt Flathub et les environnements de base : flatpak remote-add --user --if-not-exists flathub https://flathub.org/repo/flathub.flatpakrepo && flatpak install --user flathub org.freedesktop.Platform//25.08 org.freedesktop.Sdk//25.08
3. Ajoutez exécutable dans le dossier ./ScholarLog
4. Lancez la compilation : flatpak-builder build-dir com.HoferLukas.ScholarLog.yml --force-clean
5. Lancez l'export vers un dépôt local : flatpak build-export repo build-dir
6. Créez le bundle (fichier d'installation unique) : flatpak build-bundle repo ScholarLog-x64.flatpak com.HoferLukas.ScholarLog
7. Installez l'application : : flatpak install --user ScholarLog-arch.flatpak
