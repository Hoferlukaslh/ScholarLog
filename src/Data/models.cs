/*
    Fichier      :  models.cs
    Projet       :  ScholarLog

    Description  :
        Définit la structure des données et les modèles de vue (ViewModels).
        Contient les entités mappées à la base SQLite via Entity Framework Core
        ainsi que les extensions nécessaires à l'affichage dans Avalonia UI.

    Auteur       :  Lukas Hofer - TINF2
    Date         :  18.03.2026
*/

using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Module = ScholarLog.Data.Module; // Élimination de l'ambiguité avec système Module pour EF

namespace ScholarLog.Data;


/// <summary>
/// Définit le type d'une branche ou d'un cours (ex: M, TM, PM).
/// </summary>
public enum TypeCours { M, TM, PM } 

/// <summary>
/// Représente la tendance d'une moyenne pour l'affichage des indicateurs visuels (en hausse, en baisse ou stable).
/// </summary>
public enum Trend {Up, Down, Stable}


// ==========================================
#region Modèles de données (Entités)
// Ces classes représentent le modèle orienté objet (domaine métier) de l'application.
// Elles sont mappées aux tables de la base de données relationnelle locale SQLite 
// via le framework Entity Framework Core (ORM).
// ==========================================

/// <summary>
/// Entité représentant un module. 
/// Un module regroupe de manière hiérarchique ses propres types de travaux, ses branches et son journal de travail.
/// </summary>
[Table("module")]
public class Module : ObservableObject
{
    [Key][Column("mod_id")]
    public int Id { get; set; }
    
    [Column("mod_nom")]
    public string Nom { get; set; }
    
    // un module possède ses propres types de travaux, ses branches et son journal
    public List<Branche> Branches { get; set; } = new List<Branche>();
    public List<Entree> JournalDeTravail { get; set; } = new List<Entree>();
    public List<TypeTravail> TypesDeTravail { get; set; } = new List<TypeTravail>();
}

/// <summary>
/// Entité représentant une catégorie de travail pour le journal (ex: Documentation, Programmation, Recherche).
/// La liste des types de travail disponibles est unique et spécifique à chaque module pour éviter l'affichage de catégories non pertinentes.
/// </summary>
[Table("type_travail")]
public class TypeTravail : ObservableObject
{
    [Key][Column("typ_id")]
    public int Id { get; set; }

    [Column("typ_nom")]
    public string Nom { get; set; }
    
    [Column("mod_id")]
    public int ModuleId { get; set; }

    [ForeignKey("ModuleId")]
    public ScholarLog.Data.Module Module { get; set; }
}


/// <summary>
/// Entité représentant une entrée journalisée pour le suivi du temps de travail de l'étudiant.
/// Elle documente le temps passé sur une tâche, incluant la date, la durée, le type de travail et une description.
/// </summary>
[Table("entree")]
public class Entree : ObservableObject
{
    [Key][Column("ent_id")]
    public int Id { get; set; }

    [Column("ent_duree")]
    public double Duree { get; set; }

    [Column("ent_date")]
    public DateTime Date { get; set; }

    [Column("ent_description")]
    public string Description { get; set; }

    [Column("mod_id")]
    public int ModuleId { get; set; }
    
    [ForeignKey("ModuleId")]
    public ScholarLog.Data.Module Module { get; set; }

    [Column("typ_id")]
    public int TypeTravailId { get; set; }
    
    [ForeignKey("TypeTravailId")]
    public TypeTravail Type { get; set; }
    
    [NotMapped]
    private bool _isDeletePending;

    [NotMapped]
    public bool IsDeletePending
    {
        get => _isDeletePending;
        set => SetProperty(ref _isDeletePending, value); 
        // SetProperty notifiera l'interface que la valeur a changé (grâce à ObservableObject)
    }
}

/// <summary>
/// Entité représentant une subdivision d'un module, comme une matière spécifique (ex: Mathématique).
/// Elle regroupe les différentes évaluations (notes) de l'étudiant pour cette matière.
/// </summary>
[Table("branche")]
public class Branche : ObservableObject
{
    [Key][Column("bra_id")]
    public int Id { get; set; }
    
    [Column("bra_nom")]
    public string Nom { get; set; }
    
    [Column("bra_type")]
    public TypeCours Type { get; set; }

    [Column("mod_id")]
    public int ModuleId { get; set; }
    
    [ForeignKey("ModuleId")]
    public ScholarLog.Data.Module Module { get; set; }
    
    public List<Note> Notes { get; set; } = new List<Note>();
    
    /// <summary>
    /// Calcule la moyenne des notes associées à cette branche.
    /// </summary>
    /// <returns>La moyenne arrondie au demi-point (0.5) supérieur ou inférieur.</returns>
    public double CalculerMoyenne()
    {
        // vérifie que la liste existe et n'est pas vide
        if (Notes == null || Notes.Count == 0)
            return 0;

        // LINQ calcule la moyenne exacte en interne (plus rapide et plus sûr)
        double moyenneExacte = Notes.Average(n => n.Valeur);

        // ARRONDI : la moyenne de la branche est arrondie au 0.5
        return Math.Round(moyenneExacte * 2.0, MidpointRounding.AwayFromZero) / 2.0;
    }
}

/// <summary>
/// Entité représentant un résultat scolaire ou une évaluation.
/// Elle est associée à une branche existante et contient la
/// valeur entre 1 et 6 (par incrément de 0.5), le titre et la date de l'épreuve.
/// </summary>
[Table("note")]
public class Note : ObservableObject
{
    [Key][Column("not_id")]
    public int Id { get; set; }
    [Column("not_valeur")]
    public double Valeur { get; set; }
    
    [Column("not_date")]
    public DateTime Date { get; set; }
    
    [Column("not_titre")]
    public string titre { get; set; }

    [Column("bra_id")]
    public int BrancheId { get; set; }
    
    [ForeignKey("BrancheId")]
    public Branche Branche { get; set; }
    
    [NotMapped]
    private bool _isDeletePending;
    
    [NotMapped]
    private bool _hasDocument;

    [NotMapped]
    public bool HasDocument
    {
        get => _hasDocument;
        set => SetProperty(ref _hasDocument, value); 
    }
    
    public NoteArchive? ArchiveCbz { get; set; }

    [NotMapped]
    public bool IsDeletePending
    {
        get => _isDeletePending;
        set => SetProperty(ref _isDeletePending, value); 
    }
}

/// <summary>
/// Entité séparée pour stocker l'archive CBZ associée à une note.
/// L'isolation du BLOB permet de ne le charger en RAM qu'à la demande.
/// </summary>
[Table("note_archive")]
public class NoteArchive : ObservableObject
{
    [Key][Column("arc_id")]
    public int Id { get; set; }

    [Column("arc_donnees")]
    public byte[] Donnees { get; set; } // Le fichier CBZ brut

    [Column("not_id")]
    public int NoteId { get; set; }

    [ForeignKey("NoteId")]
    public Note Note { get; set; }
}
#endregion

// ==========================================
#region Affichage (MVVM)
// Modèles de vue (ViewModels du pattern MVVM)
// Ces classes sont dédiées au fonctionnement de l'interface Avalonia UI et étendent le domaine métier.
// ==========================================

/// <summary>
/// Modèle de vue (ViewModel) pour l'affichage détaillé d'une Branche dans l'interface
/// Intègre des propriétés supplémentaires pour l'affichage dynamique de la moyenne et de la tendance.
/// </summary>
public class BrancheViewModel : Branche
{
    public double Moyenne { get; set; }
    public Trend BrancheTrend { get; set; }
}

/// <summary>
/// Modèle de vue (ViewModel) pour l'affichage d'un Type de Travail dans l'interface
/// Ajoute le cumul des heures travaillées pour cette catégorie.
/// </summary>
public class TypeTravailViewModel() : TypeTravail
{
    public double Somme { get; set; }
}

/// <summary>
/// Modèle de vue (ViewModel) pour l'affichage synthétique d'un Module dans l'interface
/// Centralise les calculs globaux nécessaires au tableau de bord (moyennes théoriques, cumul du journal)
/// </summary>
public class ModuleViewModel : ScholarLog.Data.Module
{

    public string ShortName => Nom.Length <= 3 ? Nom : Nom.Substring(0, 3).ToUpper();
    public double AvgTheory { get; set; }
    public double TravailModule { get; set; }
    public Trend TheoryTrend { get; set; }

    /// <summary>
    /// Calcule la moyenne pondérée générale du module
    /// </summary>
    public double GlobalAverage
    {
        get
        {
            double moyenne = 0;
        
            // trouve directement la branche M et sa première note, sans faire de boucle
            var brancheM = base.Branches?.FirstOrDefault(b => b.Type == TypeCours.M);
            double noteProjetModule = brancheM?.Notes?.FirstOrDefault()?.Valeur ?? 0;
        
            if (noteProjetModule > 0 && AvgTheory > 0)
                moyenne = (noteProjetModule + AvgTheory) / 2.0;
            else 
                moyenne = noteProjetModule + AvgTheory; 
        
            return Math.Round(moyenne * 2.0, MidpointRounding.AwayFromZero) / 2.0;
        }
    }
}

/// <summary>
/// Extension de la classe Note pour l'affichage dans l'interface utilisateur.
/// Permet de lier une évaluation à ses métadonnées de contexte (Nom du module et de la branche)
/// afin de faciliter l'affichage dans des listes globales ou des tableaux de bord.
/// </summary>
public class NoteViewModel : Note
{
    public Note NoteData { get; set; } = new();
    public string BrancheNom { get; set; } = string.Empty;
    public string ModuleNom { get; set; } = string.Empty;
}
# endregion