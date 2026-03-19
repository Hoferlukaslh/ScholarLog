# Documentation – Couche Données (ScholarLog)

## Table des matières

- [Fichiers concernés](#fichiers-concernés)
    - [models.cs](#modelscs)
    - [AppDbContext.cs](#appdbcontextcs)
    - [DataRepository.cs](#datarepositorycs)
    - [AppDataService.cs](#appdataservicecs)

- [1. Modèles de données](#1-modèles-de-données)
    - [Énumérations](#énumérations)
    - [Entités](#entités)
        - [Module](#module)
        - [Branche](#branche)
        - [Note](#note)
        - [Entree](#entree)
        - [TypeTravail](#typetravail)

- [ViewModels (Modèles de vue)](#viewmodels-modèles-de-vue)

- [DataRepository (Opérations CRUD)](#datarepository-opérations-crud)

- [AppDbContext](#appdbcontext)

- [AppDataService](#appdataservice)

- [Diagrammes](#diagrammes)
    - [Diagramme d'architecture](#diagramme-darchitecture)
    - [Diagramme des entités (Relations)](#diagramme-des-entités-relations)

- [Maintenance et Déploiement](#maintenance-et-déploiement)
    - [Optimisation des requêtes (Compiled Models)](#optimisation-des-requêtes-compiled-models)
    - [ATTENTION](#attention)

--- 

## Fichiers concernés
| Fichier           | Rôle                                                            |
|-------------------|-----------------------------------------------------------------|
| models.cs         | Entités du domaine, Énumérations + ViewModels (UI).             |
| DataRepository.cs | Accès aux données (Opérations CRUD) et initialisation.          |
| AppDbContext.cs   | Configuration d'Entity Framework Core (Liaison BDD).            |
| AppDataService.cs | Service global (Singleton) + logique métier et état en mémoire. |

### models.cs
Contient la définition des entités métiers et des énumérations, mappées à la base SQLite :

- Entités de base : Module, Branche, Note, Entree (Journal de travail) et TypeTravail. Elles héritent toutes d'ObservableObject pour la notification de l'interface utilisateur.
- Énumérations : TypeCours (M, TM, PM) et Trend (Tendance : Up, Down, Stable).
- ViewModels : Étendent les entités pour l'affichage (ex : ModuleViewModel, BrancheViewModel) en y ajoutant des propriétés dynamiques comme les moyennes calculées (AvgTheory, GlobalAverage).


### AppDbContext.cs
Définit la classe MonDbContext qui assure la liaison entre les objets C# (DbSet) et les tables SQLite.

- Configure la création du fichier BDD.db dans le répertoire de l'exécutable (OnConfiguring).
- Utilise des modèles compilés (CompiledModels) pour des performances accrues au lancement de l'application.


### DataRepository.cs
Centralise les opérations CRUD (Create, Read, Update, Delete) de manière asynchrone avec Entity Framework Core.

- Gère la création automatique de la base de données et vérifie l'existence des tables au premier lancement (InitialiserBaseDeDonnees).
- Utilise l'Eager Loading (Include, ThenInclude) et l'optimisation AsSplitQuery pour récupérer toute l'arborescence d'un module en une seule requête (GetModulesAsync).


### AppDataService.cs
Service implémenté sous forme de Singleton (AppDataService.Instance) agissant comme source de vérité (State Management) pour toute l'application.

- Charge et maintient la liste des modules en mémoire via une ObservableRangeCollection.
- Crée l'arborescence par défaut (M0 à DIPL.) si la base est vide.
- Contient la logique métier complexe : calcul des moyennes arithmétiques (arrondies au 0.5 le plus proche) et détermination des tendances de notes.



---

# 1. Modèles de données

## Énumérations

| Nom       | Valeurs          | Description            |
|-----------|------------------|------------------------|
| TypeCours | M, TM, PM        | Type de branche        |
| Trend     | Up, Down, Stable | Indicateur de tendance |


## Entités

### Module
| Propriété        | Type              | Description                           |
|:-----------------|:------------------|:--------------------------------------|
| Id               | int               | Clé primaire (mod_id).                |
| Nom              | string            | Nom du module.                        |
| Branches         | List<Branche>     | Liste des sous-cours.                 |
| JournalDeTravail | List<Entree>      | Suivi du temps de travail.            |
| TypesDeTravail   | List<TypeTravail> | Catégories de travail personnalisées. |

### Branche
| Propriété | Type       | Description                          |
|:----------|:-----------|:-------------------------------------|
| Id        | int        | Clé primaire (bra_id).               |
| Nom       | string     | Nom de la branche.                   |
| Type      | TypeCours  | Type de cours (M, TM, PM).           |
| ModuleId  | int        | Clé étrangère vers le module parent. |
| Notes     | List<Note> | Évaluations associées.               |

### Note
| Propriété       | Type     | Description                              |
|:----------------|:---------|:-----------------------------------------|
| Id              | int      | Clé primaire (not_id).                   |
| Valeur          | double   | Note obtenue (1–6).                      |
| Date            | DateTime | Date de l'évaluation.                    |
| titre           | string   | Titre de l'épreuve.                      |
| BrancheId       | int      | Clé étrangère vers la branche parente.   |
| IsDeletePending | bool     | État local (non mappé en BDD) pour l'UI. |

### Entree
| Propriété       | Type     | Description                                 |
|:----------------|:---------|:--------------------------------------------|
| Id              | int      | Clé primaire (ent_id).                      |
| Duree           | double   | Temps travaillé.                            |
| Date            | DateTime | Date du travail.                            |
| Description     | string   | Description de la tâche accomplie.          |
| ModuleId        | int      | Clé étrangère vers le module.               |
| TypeTravailId   | int      | Clé étrangère vers la catégorie de travail. |
| IsDeletePending | bool     | État local (non mappé en BDD) pour l'UI.    |

### TypeTravail
| Propriété | Type   | Description                          |
|:----------|:-------|:-------------------------------------|
| Id        | int    | Clé primaire (typ_id).               |
| Nom       | string | Nom de la catégorie (ex: Recherche). |
| ModuleId  | int    | Clé étrangère vers le module.        |

---

## ViewModels (Modèles de vue)
| Classe                 | Propriétés Ajoutées                                             | Rôle                                                         |
|------------------------|-----------------------------------------------------------------|--------------------------------------------------------------|
| BrancheViewModel       | Moyenne, BrancheTrend                                           | Affichage dynamique d'une branche dans l'UI                  |
| TypeTravailViewModel   | Somme                                                           | Cumul des heures pour un type spécifique                     |
| ModuleViewModel        | ShortName, AvgTheory, TravailModule, TheoryTrend, GlobalAverage | Synthèse et calculs globaux pour le tableau de bord          |

## DataRepository (Opérations CRUD)
| Action   | Méthodes Implémentées                                                                                                        |
|----------|------------------------------------------------------------------------------------------------------------------------------|
| Create   | AjouterModuleAsync, AjouterEntreeAsync, AjouterNoteAsync, AjouterBrancheAsync, AjouterTypeTravailAsync (gère les doublons)   |
| Read     | GetModulesAsync (Eager Loading complet)                                                                                      |
| Update   | ModifierModuleAsync, ModifierBrancheAsync, ModifierTypeTravailAsync, ModifierNoteAsync, ModifierEntreeAsync                  |
| Delete   | SupprimerModuleAsync (Cascading), SupprimerBrancheAsync, SupprimerTypeTravailAsync, SupprimerNoteAsync, SupprimerEntreeAsync |

## AppDbContext
| Élément  | Description                                                |
|----------|------------------------------------------------------------|
| Moteur   | SQLite                                                     |
| Fichier  | BDD.db (situé dans le répertoire AppContext.BaseDirectory) |
| ORM      | Entity Framework Core (optimisé avec CompiledModels)       |

## AppDataService
| Élément              | Rôle                                                            |
|----------------------|-----------------------------------------------------------------|
| Instance (Singleton) | Point d'accès unique (AppDataService.Instance)                  |
| Modules              | ObservableRangeCollection partagée par toute l'application      |
| ChargerDonnees       | Initialisation asynchrone depuis la BDD ou création par défaut  |
| ObtenirMoyenne       | Calcule la moyenne des branches avec arrondi arithmétique (0.5) |
| DeterminerTendance   | Compare la dernière note à la moyenne globale (marge de 0.2)    |


## Diagrammes

### Diagramme d'architecture

``` mermaid
flowchart TD
    DB[(SQLite : BDD.db)]
    CTX[MonDbContext]
    REPO[DataRepository]
    SERVICE[AppDataService]
    VM[ViewModels]
    UI[Avalonia UI]

    DB --> CTX
    CTX --> REPO
    REPO --> SERVICE
    SERVICE --> VM
    VM --> UI
```

### Diagramme d'accès aux données et Services
``` mermaid
classDiagram
    %% Interfaces et classes de base du framework
    class DbContext {
        <<Entity Framework Core>>
    }
    class IDisposable {
        <<Interface>>
    }

    %% Contexte de base de données
    class MonDbContext {
        +DbSet~Note~ Note
        +DbSet~Module~ Module
        +DbSet~Branche~ Branche
        +DbSet~Entree~ Entree
        +DbSet~TypeTravail~ TypeTravail
        #OnConfiguring(DbContextOptionsBuilder options)
    }

    %% Pattern Repository
    class DataRepository {
        -MonDbContext _context
        +DataRepository()
        -InitialiserBaseDeDonnees()
        +GetModulesAsync() Task~List~Module~~
        
        %% Méthodes d'ajout
        +AjouterModuleAsync(Module m) Task
        +AjouterEntreeAsync(Entree e) Task
        +AjouterNoteAsync(Note n) Task
        +AjouterBrancheAsync(Branche b) Task
        +AjouterTypeTravailAsync(TypeTravail t) Task
        
        %% Méthodes de modification
        +ModifierModuleAsync(Module m) Task
        +ModifierBrancheAsync(Branche b) Task
        +ModifierTypeTravailAsync(TypeTravail t) Task
        +ModifierNoteAsync(Note n) Task
        +ModifierEntreeAsync(Entree e) Task
        
        %% Méthodes de suppression
        +SupprimerModuleAsync(Module m) Task
        +SupprimerBrancheAsync(Branche b) Task
        +SupprimerTypeTravailAsync(TypeTravail t) Task
        +SupprimerNoteAsync(Note n) Task
        +SupprimerEntreeAsync(Entree e) Task
        
        +Dispose()
    }

    %% Pattern Singleton & Logique Métier
    class AppDataService {
        <<Singleton>>
        -AppDataService _instance$
        +AppDataService Instance$
        +ObservableRangeCollection~ModuleViewModel~ Modules
        +bool IsLoaded
        -AppDataService()
        +ChargerDonneesGlobalesAsync() Task
        -CreerModulesParDefautAsync(DataRepository repo) Task
        +ObtenirMoyenne(List~Branche~ liste) double
        +DeterminerTendance(List~Branche~ branches, double moyenneActuelle) Trend
    }

    %% Relations d'héritage et d'implémentation
    MonDbContext --|> DbContext : Étend
    DataRepository ..|> IDisposable : Implémente

    %% Relations de dépendance et de composition
    DataRepository "1" *-- "1" MonDbContext : Contient et gère
    AppDataService ..> DataRepository : Instancie (via using)
```

Explication de l'architecture modélisée :
- Couche ORM (MonDbContext) : Elle fait le pont entre les entités C# et SQLite en héritant du DbContext natif d'Entity Framework.
- Couche d'Accès aux Données (DataRepository) : Elle encapsule complètement MonDbContext. L'interface IDisposable, assures que la connexion à la base est bien fermée après chaque opération.
- Couche Service/Métier (AppDataService) : C'est le chef d'orchestre. Le $ dans le diagramme Mermaid (à côté de _instance et Instance) indique des membres statiques, ce qui représente bien le pattern Singleton. Le service instancie temporairement le repository pour charger les entités, puis les convertit en ModuleViewModel pour l'interface Avalonia.

### Diagramme des entités (Relations)

``` mermaid
classDiagram
    %% Énumérations
    class TypeCours {
        <<enumeration>>
        M
        TM
        PM
    }
    class Trend {
        <<enumeration>>
        Up
        Down
        Stable
    }

    %% Modèles de données (Entités EF Core)
    class Module {
        +int Id
        +string Nom
    }
    class Branche {
        +int Id
        +string Nom
        +TypeCours Type
        +CalculerMoyenne() double
    }
    class TypeTravail {
        +int Id
        +string Nom
    }
    class Entree {
        +int Id
        +double Duree
        +DateTime Date
        +string Description
        +bool IsDeletePending
    }
    class Note {
        +int Id
        +double Valeur
        +DateTime Date
        +string titre
        +bool IsDeletePending
    }

    %% Modèles de vue (MVVM pour Avalonia)
    class ModuleViewModel {
        +string ShortName
        +double AvgTheory
        +double TravailModule
        +Trend TheoryTrend
        +double GlobalAverage
    }
    class BrancheViewModel {
        +double Moyenne
        +Trend BrancheTrend
    }
    class TypeTravailViewModel {
        +double Somme
    }
    class NoteViewModel {
        +string BrancheNom
        +string ModuleNom
        +Note NoteData
    }

    %% Relations de composition (1 à plusieurs)
    Module "1" *-- "*" Branche : Possède
    Module "1" *-- "*" Entree : Journalise
    Module "1" *-- "*" TypeTravail : Définit
    Branche "1" *-- "*" Note : Contient

    %% Associations simples
    Entree "*" --> "1" TypeTravail : Est classé par

    %% Relations d'héritage (ViewModels étendant les Entités)
    Module <|-- ModuleViewModel : Étend
    Branche <|-- BrancheViewModel : Étend
    TypeTravail <|-- TypeTravailViewModel : Étend
    Note <|-- NoteViewModel : Étend
    
    %% Liens vers les énumérations
    Branche --> TypeCours
    BrancheViewModel --> Trend
    ModuleViewModel --> Trend
```

## Maintenance et Déploiement

### Optimisation des requêtes (Compiled Models)

Le DbContext utilise un modèle compilé pour accélérer le démarrage :   
```.UseModel(ScholarLog.Data.CompiledModels.MonDbContextModel.Instance)```

### ATTENTION

Si vous modifiez la structure des entités dans `models.cs` (ajout de
table, modification de colonne), vous devez impérativement régénérer les
modèles compilés en exécutant la commande suivante :

``` bash
dotnet ef dbcontext optimize -c MonDbContext -o CompiledModels --namespace ScholarLog.D
```


