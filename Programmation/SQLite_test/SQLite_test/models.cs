namespace SQLite_test;

using System;
using System.Collections.Generic;
using System.Linq;



public class TypeTravail
{
    public int Id { get; set; }
    public string Nom { get; set; }
}

public class Entree
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public float Duree { get; set; } // Float comme dans l'UML
    public string Description { get; set; }
    
    public int TypeTravailId { get; set; }
    public TypeTravail Type { get; set; }
    
    public int ModuleId { get; set; }
}


public class Note
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; }
    public float Valeur { get; set; }

    public int BrancheId { get; set; }
}


public class Branche
{
    public int Id { get; set; }
    public string Nom { get; set; }
    public int ModuleId { get; set; }

    // Relation "contient *"
    public List<Note> Notes { get; set; } = new List<Note>();
    
    public float CalculerMoyenne()
    {
        if (Notes == null || !Notes.Any()) return 0f;
        return (float)Notes.Average(n => n.Valeur);
    }
}


public class Module
{
    public int Id { get; set; }
    public string Nom { get; set; }
    

    public List<Entree> Entrees { get; set; } = new List<Entree>();
    public List<Branche> Branches { get; set; } = new List<Branche>();



    public void AjouterEntree(Entree entree)
    {
        Entrees.Add(entree);
    }

    public void RetirerEntree(Entree entree)
    {
        Entrees.Remove(entree);
    }

    

    public float CalculerTotalHeures()
    {
        if (Entrees == null) return 0f;
        return Entrees.Sum(e => e.Duree);
    }


    public float CalculerTotalHeures(TypeTravail type)
    {
        if (Entrees == null) return 0f;
        return Entrees
            .Where(e => e.TypeTravailId == type.Id)
            .Sum(e => e.Duree);
    }


    // Moyenne des branches (pas définitif)
    public float CalculerMoyenne()
    {
        if (Branches == null || !Branches.Any()) return 0f;
        
        var moyennesBranches = Branches.Select(b => b.CalculerMoyenne());
        
        return (float)moyennesBranches.Average();
    }
}