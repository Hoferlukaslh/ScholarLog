1. choisir architecture : amd64, arm64, armhf
2. Renomer le dossier en mettant l'architecture à la fin. EX : scholarlog-x64, scholarlog-ARM64, scholarlog-ARM.
3. Ajouter l'executable dans le dossier scholarlog-arch/usr/bin/.
4. Editer le fichier /DEBIAN/control -> modifier avec l'architecture choisi.
5. Compiler : dpkg-deb --build scholarlog-arch

