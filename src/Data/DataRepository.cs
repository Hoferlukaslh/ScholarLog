/*
    Fichier      :  DataRepository.cs
    Projet       :  ScholarLog

    Description  :
        Assure la persistence des données entre l'application et la base SQLite.
        Implémente le pattern Repository pour centraliser les opérations CRUD 
        (Create, Read, Update, Delete) et l'initialisation du schéma de la base.

    Auteur       :  Lukas Hofer - TINF2
    Date         :  18.03.2026
*/


using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ScholarLog.Data;


/// <summary>
/// Dépôt de données (Repository) responsable de toutes les interactions avec la base de données SQLite.
/// Gère les opérations CRUD (Création, Lecture, Mise à jour, Suppression) via Entity Framework Core.
/// Implémente IDisposable pour garantir la libération correcte du contexte de base de données.
/// </summary>
public class DataRepository : IDisposable
{
    private readonly MonDbContext _context;

    /// <summary>
    /// Initialise une nouvelle instance du dépôt et s'assure que la base de données est prête à l'emploi.
    /// </summary>
    public DataRepository()
    {
        _context = new MonDbContext();
        InitialiserBaseDeDonnees();
    }

    /// <summary>
    /// Vérifie si la base de données et ses tables existent. 
    /// Si ce n'est pas le cas, génère le schéma nécessaire pour stocker les données.
    /// </summary>
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

    /// <summary>
    /// Récupère la liste complète des modules avec toutes leurs entités liées 
    /// (Branches, Notes, Journal de travail, Types de travail) en utilisant un chargement anticipé (Eager Loading).
    /// </summary>
    /// <returns>Une liste de modules contenant toute l'arborescence des données.</returns>
    public async Task<List<Module>> GetModulesAsync()
    {
        return await _context.Module
            .Include(m => m.Branches).ThenInclude(b => b.Notes)
            .Include(m => m.JournalDeTravail).ThenInclude(e => e.Type) 
            .Include(m => m.TypesDeTravail)
            .AsSplitQuery()
            .ToListAsync();
    }

    // --- CRÉATION ASYNCHRONE ---

    /// <summary>Ajoute un nouveau module à la base de données.</summary>
    public async Task AjouterModuleAsync(Module m) { _context.Module.Add(m); await _context.SaveChangesAsync(); }
    
    /// <summary>Ajoute une nouvelle entrée au journal de travail.</summary>
    public async Task AjouterEntreeAsync(Entree e) { _context.Entree.Add(e); await _context.SaveChangesAsync(); }
    
    /// <summary>Ajoute une nouvelle note associée à une branche.</summary>
    public async Task AjouterNoteAsync(Note n) { _context.Note.Add(n); await _context.SaveChangesAsync(); }
    
    /// <summary>Ajoute une nouvelle branche (cours) à un module.</summary>
    public async Task AjouterBrancheAsync(Branche b) { _context.Branche.Add(b); await _context.SaveChangesAsync(); }
    
    /// <summary>
    /// Ajoute un nouveau type de travail. Vérifie d'abord s'il existe déjà pour éviter les doublons.
    /// S'il existe déjà, met à jour l'ID de l'objet passé en paramètre avec celui de la base de données.
    /// </summary>
    /// <param name="t">Le type de travail à ajouter ou à vérifier.</param>
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

    // --- MODIFICATION ASYNCHRONE ---

    /// <summary>Met à jour les informations d'un module existant.</summary>
    public async Task ModifierModuleAsync(Module m) { _context.Module.Update(m); await _context.SaveChangesAsync(); }
    
    /// <summary>Met à jour les informations d'une branche existante.</summary>
    public async Task ModifierBrancheAsync(Branche b) { _context.Branche.Update(b); await _context.SaveChangesAsync(); }
    
    /// <summary>Met à jour le nom d'un type de travail existant.</summary>
    public async Task ModifierTypeTravailAsync(TypeTravail t) { _context.TypeTravail.Update(t); await _context.SaveChangesAsync(); }
    
    /// <summary>Met à jour les valeurs d'une note existante.</summary>
    public async Task ModifierNoteAsync(Note n) { _context.Note.Update(n); await _context.SaveChangesAsync(); }
    
    /// <summary>Met à jour les détails d'une entrée du journal de travail.</summary>
    public async Task ModifierEntreeAsync(Entree e) { _context.Entree.Update(e); await _context.SaveChangesAsync(); }

    // --- SUPPRESSION ASYNCHRONE ---

    /// <summary>Supprime un module et toutes ses dépendances (cascading delete géré par la BDD).</summary>
    public async Task SupprimerModuleAsync(Module m) { _context.Module.Remove(m); await _context.SaveChangesAsync(); }
    
    /// <summary>Supprime une branche de la base de données.</summary>
    public async Task SupprimerBrancheAsync(Branche b) { _context.Branche.Remove(b); await _context.SaveChangesAsync(); }
    
    /// <summary>Supprime un type de travail.</summary>
    public async Task SupprimerTypeTravailAsync(TypeTravail t) { _context.TypeTravail.Remove(t); await _context.SaveChangesAsync(); }
    
    /// <summary>Supprime une note de la base de données.</summary>
    public async Task SupprimerNoteAsync(Note n) { _context.Note.Remove(n); await _context.SaveChangesAsync(); }
    
    /// <summary>Supprime une entrée spécifique du journal de travail.</summary>
    public async Task SupprimerEntreeAsync(Entree e) { _context.Entree.Remove(e); await _context.SaveChangesAsync(); }

    /// <summary>
    /// Libère les ressources non managées et supprime le contexte de base de données.
    /// Appelée automatiquement si le Repository est instancié dans un bloc 'using'.
    /// </summary>
    public void Dispose()
    {
        _context?.Dispose();
    }
    
    

    /// <summary>
    /// Récupère l'archive CBZ d'une note spécifique. 
    /// Cette méthode est isolée pour ne charger le gros BLOB en RAM que lorsque c'est nécessaire (ex: ouverture du lecteur).
    /// </summary>
    /// <param name="noteId">Identifiant de la note</param>
    /// <returns>Blob de données</returns>
    public async Task<byte[]?> GetArchiveCbzPourNoteAsync(int noteId)
    {
        var archive = await _context.NoteArchive
            .AsNoTracking() // Optimisation : on ne track pas un BLOB
            .FirstOrDefaultAsync(a => a.NoteId == noteId);
        return archive?.Donnees;
    }
    
    
     /// <summary>
     /// Ajoute ou met à jour l'archive CBZ d'une note.
     /// </summary>
     /// <param name="noteId">Identifiant de la note</param>
     /// <param name="cbzData">Blob de données</param>
    public async Task SauvegarderArchiveCbzAsync(int noteId, byte[] cbzData)
    {
        var archiveExistante = await _context.NoteArchive.FirstOrDefaultAsync(a => a.NoteId == noteId);

        if (archiveExistante != null)
        {
            // Mise à jour si l'archive existe déjà
            archiveExistante.Donnees = cbzData;
            _context.NoteArchive.Update(archiveExistante);
        }
        else
        {
            // Création d'une nouvelle archive
            var nouvelleArchive = new NoteArchive 
            { 
                NoteId = noteId, 
                Donnees = cbzData 
            };
            _context.NoteArchive.Add(nouvelleArchive);
        }

        await _context.SaveChangesAsync();
    }
}