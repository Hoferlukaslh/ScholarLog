# Documentation – Interfaces Utilisateur (Dossier Pages)

## Table des matières
- [1. Accueil (HomePage)](#1-accueil-homepage)
- [2. Journal de Travail (JournalPage)](#2-journal-de-travail-journalpage)
- [3. Résultats et Notes (NotesPage)](#3-résultats-et-notes-notespage)
- [4. Paramètres (SettingsPage)](#4-paramètres-settingspage)

---

## Structure Technique : `.axaml` vs `.cs`
Chaque page de l'application fonctionne avec deux fichiers complémentaires :

- Le fichier .axaml (Le Visuel) : C'est le "dessin" de la page. Il contient les boutons, les textes, les couleurs et la disposition des éléments.
- Le fichier .cs (Le Cerveau) : C'est le code qui gère les actions spéciales (ouvrir un explorateur de fichiers, lancer un compte à rebours de sécurité ou gérer les animations).   


---

## 1. Accueil (HomePage)

### Vue d’ensemble
Tableau de bord affichant les modules sous forme de cartes interactives.  
Un clic ouvre un panneau avec statistiques détaillées et visualisations.

### Fonctionnalités

- **Cartes dynamiques**
    - Nom du module (abrégé)
    - Moyenne théorique
    - Note de travail
    - Évolution des notes   
  

- **Indicateurs visuels**
    - Vert : hausse
    - Orange : stable
    - Rouge : baisse   


- **Panneaux d’analyse**
    - Journaux récents
    - Moyennes par branche
    - Graphique en anneau (Donut)   
  

---

## 2. Journal de Travail (JournalPage)

### Vue d’ensemble
Interface de gestion des sessions de travail avec suivi détaillé et export des données.

### Fonctionnalités

- **Suivi des entrées**
    - Date, durée, catégorie, description

- **Gestion**
    - Ajout / modification via fenêtres modales
    - Organisation des catégories
    - Visualisation graphique   
  

- **Export**
    - Formats : Markdown, CSV, JSON
    - Prévisualisation, copie, sauvegarde   
  
  
- **Sécurité**
    - Confirmation avant suppression (délai 3s ou double action)
    - Raccourcis clavier pour suppression rapide   
  
  
---

## 3. Résultats et Notes (NotesPage)

### Vue d’ensemble
Gestion des notes avec calcul des moyennes pondérées.

### Fonctionnalités

- **Affichage**
    - Liste globale
    - Vue détaillée par module   
  

- **Ajout de notes**
    - Module, branche, titre, date
    - Note (1 à 6)   
  

- **Sécurité**
    - Confirmation différée avant suppression (3 secondes)   
  

---

## 4. Paramètres (SettingsPage)

### Vue d’ensemble
Configuration des aspects techniques et visuels de l’application.

### Fonctionnalités

- **Base de données**
    - Sélection du fichier SQLite (.db)   
  

- **Gestion des erreurs**
    - Notifications dynamiques (Flyout)
    - Fichier introuvable / modifications non enregistrées   
  

- **Interface**
    - Mode clair / sombre   
  