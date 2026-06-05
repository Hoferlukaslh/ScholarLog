1. choissez architecture : amd64, arm64, armhf
2. Renomez le dossier en mettant l'architecture à la fin. EX : scholarlog-x64, scholarlog-ARM64, scholarlog-ARM.
3. Ajoutez l'executable dans le dossier scholarlog-arch/usr/bin/.
4. Editez le fichier /DEBIAN/control -> modifier avec l'architecture choisi.
5. Compilez : dpkg-deb --build scholarlog-arch