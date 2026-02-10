using System.ComponentModel.DataAnnotations;

namespace SQLite_test;

using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;

public class DataRepository
{
    private readonly MonDbContext _context;

    public DataRepository()
    {
        _context = new MonDbContext();
        _context.Database.EnsureCreated(); // Crée la BDD si elle n'existe pas
    }

    // --- Gestion des Modules ---
    public List<Module> GetModules()
    {
        // On charge tout d'un coup (Modules + Branches + Notes) pour que tes calculs marchent
        return _context.Module
            .Include(m => m.Branches)
            .ThenInclude(b => b.Notes)
            .Include(m => m.Entrees)
            .ToList();
    }

    public void AjouterModule(Module m)
    {
        _context.Module.Add(m);
        _context.SaveChanges(); // On sauvegarde direct
    }

    // --- Gestion des Entrées (Travail) ---
    public void AjouterEntree(Entree e)
    {
        _context.Entree.Add(e);
        _context.SaveChanges();
    }

    // --- Gestion des Notes ---
    public void AjouterNote(Note n)
    {
        _context.Note.Add(n);
        _context.SaveChanges();
    }
    
    // --- Gestion des Notes ---
    public void AjouterBranche(Branche b)
    {
        _context.Branche.Add(b);
        _context.SaveChanges();
    }

    public void AjouterTypeTravail(TypeTravail t)
    {
        bool existant = false;
        int idTrouvé = -1;
        
        foreach (TypeTravail typeTravail in _context.TypeTravail)
        {
            if (typeTravail.Nom == t.Nom)
            {
                existant = true;
                idTrouvé = typeTravail.Id;
            }
        }

        if (!existant)
        {
            _context.TypeTravail.Add(t);
            _context.SaveChanges();
        }
        /*else
        {
            Console.WriteLine($"Contient déjà  : {idTrouvé}  - {t.Nom}");
        }*/
    }
    
    public List<TypeTravail> GetTypeTravail()
    {
        // Pas besoin de Include, car TypeTravail ne contient pas de listes complexes
        return _context.TypeTravail.ToList();
    }
}