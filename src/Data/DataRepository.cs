namespace ScholarLog.Data;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

public class DataRepository : IDisposable
{
    private readonly MonDbContext _context;

    public DataRepository()
    {
        _context = new MonDbContext();
        InitialiserBaseDeDonnees();
    }

    private void InitialiserBaseDeDonnees()
    {
        _context.Database.EnsureCreated();

        using (var command = _context.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='module';";
            _context.Database.OpenConnection();
        
            var result = command.ExecuteScalar();
        
            if (result == null)
            {
                var databaseCreator = (Microsoft.EntityFrameworkCore.Storage.RelationalDatabaseCreator)_context.Database.GetService<Microsoft.EntityFrameworkCore.Storage.IDatabaseCreator>();
                databaseCreator.CreateTables();
                Console.WriteLine("Tables créées dans la base existante.");
            }
        }
    }

    //  Lecture Asynchrone 
    public async Task<List<Module>> GetModulesAsync()
    {
        return await _context.Module
            .Include(m => m.Branches).ThenInclude(b => b.Notes)
            .Include(m => m.JournalDeTravail).ThenInclude(e => e.Type) 
            .Include(m => m.TypesDeTravail)
            .ToListAsync();
    }

    //  Création Asynchrone 
    public async Task AjouterModuleAsync(Module m) { _context.Module.Add(m); await _context.SaveChangesAsync(); }
    public async Task AjouterEntreeAsync(Entree e) { _context.Entree.Add(e); await _context.SaveChangesAsync(); }
    public async Task AjouterNoteAsync(Note n) { _context.Note.Add(n); await _context.SaveChangesAsync(); }
    public async Task AjouterBrancheAsync(Branche b) { _context.Branche.Add(b); await _context.SaveChangesAsync(); }
    
    public async Task AjouterTypeTravailAsync(TypeTravail t)
    {
        // On cherche si le type existe déjà en base
        var existant = await _context.TypeTravail
            .FirstOrDefaultAsync(type => type.Nom == t.Nom && type.ModuleId == t.ModuleId);
        
        if (existant == null)
        {
            // Il n'existe pas, on l'ajoute (EF Core va mettre à jour t.Id automatiquement)
            _context.TypeTravail.Add(t);
            await _context.SaveChangesAsync();
        }
        else
        {
            // CRITIQUE : Il existe déjà, on doit assigner le vrai ID de la base à notre objet !
            t.Id = existant.Id;
        }
    }

    //  Modification Asynchrone 
    public async Task ModifierModuleAsync(Module m) { _context.Module.Update(m); await _context.SaveChangesAsync(); }
    public async Task ModifierBrancheAsync(Branche b) { _context.Branche.Update(b); await _context.SaveChangesAsync(); }
    public async Task ModifierTypeTravailAsync(TypeTravail t) { _context.TypeTravail.Update(t); await _context.SaveChangesAsync(); }
    public async Task ModifierNoteAsync(Note n) { _context.Note.Update(n); await _context.SaveChangesAsync(); }
    public async Task ModifierEntreeAsync(Entree e) { _context.Entree.Update(e); await _context.SaveChangesAsync(); }

    //  Suppression Asynchrone 
    public async Task SupprimerModuleAsync(Module m) { _context.Module.Remove(m); await _context.SaveChangesAsync(); }
    public async Task SupprimerBrancheAsync(Branche b) { _context.Branche.Remove(b); await _context.SaveChangesAsync(); }
    public async Task SupprimerTypeTravailAsync(TypeTravail t) { _context.TypeTravail.Remove(t); await _context.SaveChangesAsync(); }
    public async Task SupprimerNoteAsync(Note n) { _context.Note.Remove(n); await _context.SaveChangesAsync(); }
    public async Task SupprimerEntreeAsync(Entree e) { _context.Entree.Remove(e); await _context.SaveChangesAsync(); }

    public void Dispose()
    {
        _context?.Dispose();
    }
}