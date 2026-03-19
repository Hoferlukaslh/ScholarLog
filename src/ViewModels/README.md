# Documentation – ViewModels


## Table des matières
- [Le Principe : L'Architecture MVVM](#le-principe--larchitecture-mvvm)
- [Contenu du dossier](#contenu-du-dossier)
    - [ViewModelBase.cs](#viewmodelbasecs)
    - [MainWindowViewModel.cs](#mainwindowviewmodelcs)
    - [HomeViewModel.cs](#homeviewmodelcs)
    - [JournalViewModel.cs](#journalviewmodelcs)
    - [NotesViewModel.cs](#notesviewmodelcs)
    - [SettingsViewModel.cs](#settingsviewmodelcs)

---

## Le Principe : L'Architecture MVVM

    Les ViewModels reposent sur le pattern MVVM (Model-View-ViewModel). Le principe est simple : séparer strictement l'interface graphique (la Vue) de la logique métier et des données (le Modèle).

    Le ViewModel agit comme un chef d'orchestre intermédiaire :
    - Il prépare et formate les données du Modèle pour qu'elles soient affichables par la Vue.
    - Il intercepte les actions de l'utilisateur (clics, saisies) via des Commandes (ex: [RelayCommand]).
    - Il notifie automatiquement la Vue lorsque les données changent (grâce à [ObservableProperty] fourni par le CommunityToolkit.Mvvm).

    > [!TIP]
    > Cela permet d'avoir un code propre, testable, et où l'interface graphique n'est pas mélangée avec la logique de la base de données.
    


    ```mermaid
    graph TD
        %% Définition des nœuds
        View[Vue / Interface UI<br/>Avalonia UI]
        ViewModel[ViewModel<br/>Chef d'orchestre]
        Model[Modèle / Données<br/>Entity Framework Core]

        %% Définition des relations et flux
        View -- "1. Notifie les actions utilisateur<br/>(Commandes/Bindings)" --> ViewModel
        ViewModel -- "2. Manipule les données<br/>(Logique métier)" --> Model
        Model -- "3. Envoie les données brutes<br/>(Entités)" --> ViewModel
        ViewModel -- "4. Met à jour l'affichage<br/>(Notification de changement / Binding)" --> View

        %% Personnalisation des styles (facultatif, mais joli)
        style View fill:#f9f,stroke:#333,stroke-width:2px,rx:10,ry:10
        style ViewModel fill:#ccf,stroke:#333,stroke-width:2px,rx:10,ry:10
        style Model fill:#ff9,stroke:#333,stroke-width:2px,rx:10,ry:10
    ```

---

## Contenu du dossier

### ViewModelBase.cs
C'est la fondation. Cette classe hérite de ObservableObject et sert de parent à tous les autres ViewModels pour leur transmettre les capacités de notification de changement d'état.

### MainWindowViewModel.cs
Le chef d'orchestre principal. Il gère la navigation globale de l'application (bascule entre les différentes pages), contrôle l'ouverture du menu latéral, et orchestre la séquence de chargement initial de l'application.

### HomeViewModel.cs
Le tableau de bord (Accueil). Il agrège les données globales pour afficher des résumés : il calcule les moyennes par branche, prépare les données pour le graphique (Donut), détermine les tendances, et permet de naviguer vers d'autres pages via un système de messagerie (WeakReferenceMessenger).

### JournalViewModel.cs
Le moteur du journal de travail étudiant. Il gère toutes les opérations d'ajout, modification et suppression (CRUD) des entrées de journal et de leurs catégories. C'est aussi lui qui embarque la logique complexe de génération des rapports d'exportation (Markdown, CSV, JSON).

### NotesViewModel.cs
Le gestionnaire d'évaluations. Il s'occupe de la logique CRUD pour les notes, lie dynamiquement chaque note à sa branche/module parent, et synchronise ces opérations en direct avec la base de données SQLite.

### SettingsViewModel.cs
Le gestionnaire des paramètres. Il contrôle des éléments très spécifiques comme la validation et la sauvegarde du chemin de la base de données locale, ainsi que la bascule dynamique entre le thème clair et sombre.