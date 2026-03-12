namespace ScholarLog.Data;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public enum TypeCours { M, TM, PM } 

[Table("module")]
public class Module
{
    [Key][Column("mod_id")]
    public int Id { get; set; }
    
    [Column("mod_nom")]
    public string Nom { get; set; }
    
    // Un module possède ses propres types de travaux, ses branches et son journal
    public List<Branche> Branches { get; set; } = new List<Branche>();
    public List<Entree> JournalDeTravail { get; set; } = new List<Entree>();
    public List<TypeTravail> TypesDeTravail { get; set; } = new List<TypeTravail>();

    public double CalculerNoteFinale()
    {
        double sommeMoyennesTM = 0;
        int compteurTM = 0;

        double sommeMoyennesPM = 0;
        int compteurPM = 0;

        // parcours de toutes les branches du module
        foreach (Branche branche in Branches)
        {
            // traitement seulement  s'il y a des notes
            if (branche.Notes != null && branche.Notes.Count > 0)
            {
                double moyenneBranche = branche.CalculerMoyenne();

                // trie par type de cours (TM ou PM)
                if (branche.Type == TypeCours.TM)
                {
                    sommeMoyennesTM += moyenneBranche;
                    compteurTM++;
                }
                else if (branche.Type == TypeCours.PM)
                {
                    sommeMoyennesPM += moyenneBranche;
                    compteurPM++;
                }
            }
        }

        // Calcul des moyennes intermédiaires pour chaque groupe
        double noteFinaleTM = (compteurTM > 0) ? (sommeMoyennesTM / compteurTM) : 0;
        double noteFinalePM = (compteurPM > 0) ? (sommeMoyennesPM / compteurPM) : 0;

        // calcul de la note finale du module
        // Si les deux types existent, on fait la moyenne des deux
        if (compteurTM > 0 && compteurPM > 0)
            return (noteFinaleTM + noteFinalePM) / 2.0;

        // Sinon, on retourne l'une ou l'autre (si l'une est à 0, l'addition fonctionne)
        return noteFinaleTM + noteFinalePM;
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
}

[Table("branche")]
public class Branche
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
        double sommeTotale = 0;
        int nombreDeNotes = 0;

        // vérification de sécurité : si la liste de notes est vide ou nulle
        if (Notes == null || Notes.Count == 0)
            return 0;

        // parcours de chaque note pour faire la somme
        foreach (var note in Notes)
        {
            sommeTotale += note.Valeur;
            nombreDeNotes++;
        }

        // Somme divisée par le nombre d'éléments
        return sommeTotale / nombreDeNotes;
    }
}

[Table("note")]
public class Note
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
}