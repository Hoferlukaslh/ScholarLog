 <!-- Shields (badges) -->
<div align="center">
  <!-- https://github.com/Ileriayo/markdown-badges - MIT License - 2020 Ileriayo Adebiyi -->
  <a href="https://www.gnu.org/licenses/gpl-3.0.html"><img src="Images/Logos_Icones/Badges/License-GPLv3.svg" height="25" alt="GPLv3" /></a>
  <a href="https://learn.microsoft.com/fr-fr/dotnet/csharp/"><img src="Images/Logos_Icones/Badges/cs.svg" height="25" alt="C#" /></a>
  <a href="https://dotnet.microsoft.com/fr-fr/"><img src="Images/Logos_Icones/Badges/DotNET.svg" height="25" alt=".NET" /></a>
  <a href="https://avaloniaui.net/"><img src="Images/Logos_Icones/Badges/AvaloniaUI.svg" height="25" alt="Avalonia UI/" /></a>
  <a href="https://www.sqlite.org/index.html"><img src="Images/Logos_Icones/Badges/sqlite.svg" height="25" alt="SQLite" /></a>
  <a href="https://www.microsoft.com/fr-ch/windows/"><img src="Images/Logos_Icones/Badges/Windows.svg" height="25" alt="Windows" /></a>
  <a href="https://www.apple.com/chfr/macos/"><img src="Images/Logos_Icones/Badges/macOS.svg" height="25" alt="macOS" /></a>
  <a href="https://www.debian.org/index.fr.html"><img src="Images/Logos_Icones/Badges/Debian.svg" height="25" alt="Debian" /></a>
  <a href="https://penpot.app/"><img src="Images/Logos_Icones/Badges/penpot.svg" height="25" alt="Penpot" /></a>
</div>

## Table des matières

- [ScholarLog](#scholarlog)
- [Objectif](#objectif)
- [Technologies](#technologies)
- [Utilisateurs cibles](#utilisateurs-cibles)
- [Fonctionnalités principales](#fonctionnalités-principales)
- [Ressources utilisées](#ressources-utilisées) 
  - [Logiciels](#logiciels)
  - [Police d'écriture](#police-décriture)


# ScholarLog
Logiciel GUI (mono-utilisateur) d'inscription de notes et de journal de travail

Voir interface -> [README Interface](./Interface)     
Lien vers maquette interractif -> [Penpot/Maquette](https://penpot.kreativcam.ch/#/view?file-id=255dee3d-9464-811c-8007-7cee81528364&page-id=255dee3d-9464-811c-8007-7cee81528365&section=interactions&index=0&share-id=6f19317a-c44f-8084-8007-84b8385845b2) 

## Objectif
Développer une application de bureau permettant à un élève de gérer et suivre ses résultats scolaires de manière locale et autonome.

## Technologies

| Catégorie        | Technologie    | Détails                   | Version   |
|---               |---             |---                        |---        |
| Langage          | C#             | Développement principal   | 14.0      |
| Framework UI     | Avalonia UI    | Interface multiplateforme | 12.0      |
| Base de données  | EF core        | Stockage local de la BDD  | 10.0.5    |

## Utilisateurs cibles
- Usage individuel
- Élève en formation ES

## fonctionnalités principales

### Gestion des résultats (Notes)
* **Structure hiérarchique :** Organisation logique par Modules et Branches.
* **Calcul automatisé  :** Moyennes des branches calculées dynamiquement et arrondies au 0.5 le plus proche.
* **Aperçu PDF intégré :** Possibilité de joindre et de lire directement les documents PDF (ex: évaluations scannées) liés à une note grâce à une visionneuse intégrée.

### Journal de travail (Time Tracking)
* **Suivi journalisé :** Enregistrement des entrées documentant le temps passé par module, incluant la date, la durée et une description.
* **Catégorisation dynamique :** Création et gestion de catégories de travail (ex: Programmation, Recherche, Documentation) personnalisables pour chaque module.
* **Visualisation :** Tableau de bord interactif avec un diagramme en secteurs (Donut Chart) illustrant la répartition des heures par type de travail.

### Exportation Universelle
* **Formats supportés :**
    * **Markdown (MD) :** Tableaux parfaitement alignés et résumés structurés des moyennes.
    * **CSV :** Format brut optimisé pour l'importation sur Excel ou d'autres tableurs.
    * **JSON :** Structure hiérarchique propre.
* Possibilité de copier les données directement dans le presse-papier ou de les sauvegarder sur le disque local.

### Interface & Technique
* **Tableau de bord :** Vue d'ensemble des modules avec indicateurs de tendance visuels (en hausse, en baisse, stable) pour voir la tendance des notes.
* **Thème et Ergonomie :** Interface moderne avec thème sombre réduisant la fatigue visuelle. 
* **Données 100% Locales :** Base de données SQLite embarquée. Possibilité de configurer l'emplacement du fichier de sauvegarde depuis les paramètres.
* **Multi-plateforme :** Développé avec le framework Avalonia UI en C#, garantissant un fonctionnement natif sur Windows, macOS et Linux.

# Ressources utilisées
## Logiciels
| Logo | Application | Version | Utilité | Licence |
| :---: | :--- | :--- | :---: | :--- |
| <img src="Images/Logos_Icones/LazyGit.svg" width="48"/> | [**LazyGit**](https://github.com/jesseduffield/lazygit) | 0.58.1 | Interface graphique en terminal pour la gestion des dépôts Git (commits, branches, merges). | MIT |
| <img src="Images/Logos_Icones/OnlyOffice.svg" width="48"/> | [**OnlyOffice**](https://www.onlyoffice.com) | 9.3.1 | Suite bureautique utilisée pour la rédaction et l’édition des documents du projet (DOCX, XLSX). | AGPL-3.0 |
| <img src="Images/Logos_Icones/PenpotLogo.svg" width="48"/> | [**Penpot**](https://penpot.app) | 2.13.0 | Outil de conception d’interfaces (UI/UX) utilisé pour la création des maquettes. | AGPL-3.0 |
| <img src="Images/Logos_Icones/DrawIo.svg" width="48"/> | [**Draw.io**](https://github.com/jgraph/drawio) | 29.3.6 | Outil de conception de diagrammes. Utilisé pour la conception UML. | Apache 2.0 |
| <img src="Images/Logos_Icones/Looping.svg" width="48"/> | [**Looping**](https://www.looping-mcd.fr/) | 4.1 | Logiciel de modélisation conceptuelle de données (MCD). | Freeware |
| <img src="Images/Logos_Icones/JetBrainRider.svg" width="48"/> | [**JetBrains Rider**](https://www.jetbrains.com/rider/) | 2026.1 | Environnement de développement (IDE) utilisé pour le développement et le débogage des applications .NET. | Propriétaire |

## Police d'écriture
| Logo      | Nom       | Lien                                      | Utilité               | 
| ---       | ---       | ---                                       | ---                   |
| <img src="Images/Logos_Icones/Helvetica.svg" width="48"/> | Helvetica | https://font.download/font/helvetica-255  | Textes                |
| <img src="Images/Logos_Icones/Phosphor.svg" width="48"/>       | Phosphor  | https://phosphoricons.com/                | Affichage des icones  |
