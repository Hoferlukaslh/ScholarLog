namespace ScholarLog.Data;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

[Table("module")]
public class Module
{
    [Key][Column("mod_id")]
    public int Id { get; set; }

    [Column("mod_nom")]
    public string Nom { get; set; }
    
    public List<Entree> Entrees { get; set; } = new List<Entree>();
    public List<Branche> Branches { get; set; } = new List<Branche>();
    public List<TypeTravail> TypesTravail { get; set; } = new List<TypeTravail>();
    
    public double CalculerTotalHeures() => Entrees?.Sum(e => e.Duree) ?? 0;

    public double CalculerTotalHeures(TypeTravail type) 
        => Entrees?.Where(e => e.TypeTravailId == type.Id).Sum(e => e.Duree) ?? 0;

    public double CalculerMoyenne()
    {
        if (Branches == null || !Branches.Any()) return 0;
        var moyennes = Branches.Select(b => b.CalculerMoyenne()).Where(m => m > 0).ToList();
        return moyennes.Any() ? moyennes.Average() : 0;
    }
}

[Table("branche")]
public class Branche
{
    [Key][Column("bra_id")]
    public int Id { get; set; }

    [Column("bra_nom")]
    public string Nom { get; set; }

    [Column("mod_id")]
    public int ModuleId { get; set; }

    [ForeignKey("ModuleId")]
    public Module Module { get; set; }
    
    public List<Note> Notes { get; set; } = new List<Note>();
    
    public double CalculerMoyenne()
    {
        if (Notes == null || !Notes.Any()) return 0;
        return Notes.Average(n => n.Valeur);
    }
}

[Table("type_travail")]
public class TypeTravail
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
public class Entree
{
    [Key][Column("ent_id")]
    public int Id { get; set; }

    [Column("ent_duree")]
    public double Duree { get; set; }

    [Column("ent_description")]
    public string Description { get; set; }

    [Column("ent_date")]
    public DateTime Date { get; set; }

    [Column("mod_id")]
    public int ModuleId { get; set; }

    [Column("typ_id")]
    public int TypeTravailId { get; set; } 

    [ForeignKey("ModuleId")]
    public Module Module { get; set; }

    [ForeignKey("TypeTravailId")]
    public TypeTravail Type { get; set; }
}

[Table("note")]
public class Note
{
    [Key][Column("not_id")]
    public int Id { get; set; }

    [Column("not_date")]
    public DateTime Date { get; set; }

    [Column("not_description")]
    public string Description { get; set; }

    [Column("not_valeur")]
    public double Valeur { get; set; }

    [Column("bra_id")]
    public int BrancheId { get; set; }

    [ForeignKey("BrancheId")]
    public Branche Branche { get; set; }
}