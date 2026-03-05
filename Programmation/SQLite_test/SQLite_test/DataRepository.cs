namespace SQLite_test;

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq;
using System.Collections.Generic;

public class DataRepository : IDisposable
{
    private readonly MonDbContext _context;

    public DataRepository()
    {
        _context = new MonDbContext();

        // 1. On s'assure que le fichier existe et que les tables de base sont là
        // Si le fichier n'existe pas, il le crée. S'il existe, il ne fait rien.
        _context.Database.EnsureCreated();

        // 2. Sécurité spécifique pour SQLite : 
        // On vérifie si la table 'Module' existe vraiment dans le fichier
        using (var command = _context.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Module';";
            _context.Database.OpenConnection();
        
            var result = command.ExecuteScalar();
        
            if (result == null)
            {
                // La table n'existe pas (le fichier était vide ou vieux)
                // On utilise le "Generator" pour créer les tables manquantes sans supprimer le fichier
                var databaseCreator = (Microsoft.EntityFrameworkCore.Storage.RelationalDatabaseCreator)_context.Database.GetService<Microsoft.EntityFrameworkCore.Storage.IDatabaseCreator>();
                databaseCreator.CreateTables();
                Console.WriteLine("Tables créées dans la base existante.");
            }
        }
    }
    // Lecture 
    public List<Module> GetModules()
    {
        return _context.Module
            .Include(m => m.Branches).ThenInclude(b => b.Notes)
            .Include(m => m.Entrees).ThenInclude(e => e.Type)
            .Include(m => m.TypesTravail)
            .ToList();
    }

    // Création 
    public void AjouterModule(Module m) { _context.Module.Add(m); _context.SaveChanges(); }
    public void AjouterEntree(Entree e) { _context.Entree.Add(e); _context.SaveChanges(); }
    public void AjouterNote(Note n) { _context.Note.Add(n); _context.SaveChanges(); }
    public void AjouterBranche(Branche b) { _context.Branche.Add(b); _context.SaveChanges(); }
    
    public void AjouterTypeTravail(TypeTravail t)
    {
        if (!_context.TypeTravail.Any(type => type.Nom == t.Nom && type.ModuleId == t.ModuleId))
        {
            _context.TypeTravail.Add(t);
            _context.SaveChanges();
        }
    }

    // Modification 
    public void ModifierModule(Module m) { _context.Module.Update(m); _context.SaveChanges(); }
    public void ModifierBranche(Branche b) { _context.Branche.Update(b); _context.SaveChanges(); }
    public void ModifierTypeTravail(TypeTravail t) { _context.TypeTravail.Update(t); _context.SaveChanges(); }
    public void ModifierNote(Note n) { _context.Note.Update(n); _context.SaveChanges(); }
    public void ModifierEntree(Entree e) { _context.Entree.Update(e); _context.SaveChanges(); }

    // Suppression 
    public void SupprimerModule(Module m) { _context.Module.Remove(m); _context.SaveChanges(); }
    public void SupprimerBranche(Branche b) { _context.Branche.Remove(b); _context.SaveChanges(); }
    public void SupprimerTypeTravail(TypeTravail t) { _context.TypeTravail.Remove(t); _context.SaveChanges(); }
    public void SupprimerNote(Note n) { _context.Note.Remove(n); _context.SaveChanges(); }
    public void SupprimerEntree(Entree e) { _context.Entree.Remove(e); _context.SaveChanges(); }

    public void Dispose()
    {
        _context?.Dispose();
    }
}