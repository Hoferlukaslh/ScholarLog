using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ScholarLog.Data;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public enum TypeCours { M, TM, PM } 
public enum Trend {Up, Down, Stable}



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
    public Module Module { get; set; }
}


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
    public Module Module { get; set; }

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
    public Module Module { get; set; }
    
    public List<Note> Notes { get; set; } = new List<Note>();
    
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
    public bool IsDeletePending
    {
        get => _isDeletePending;
        set => SetProperty(ref _isDeletePending, value); 
    }
}




/// 
/// AFFICHAGE
/// 


public class BrancheViewModel : Branche
{
    public double Moyenne { get; set; }
    public Trend BrancheTrend { get; set; }
}


public class TypeTravailViewModel() : TypeTravail
{
    public double Somme { get; set; }
}

public class ModuleViewModel : Module
{

    public string ShortName => Nom.Length <= 3 ? Nom : Nom.Substring(0, 3).ToUpper();
    public double AvgTheory { get; set; }
    public double TravailModule { get; set; }
    public Trend TheoryTrend { get; set; }

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