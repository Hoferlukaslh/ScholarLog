namespace ScholarLog.Data;

using System;
using System.IO;
using Microsoft.EntityFrameworkCore;

public class MonDbContext : DbContext
{
    // Tables de la base de données
    public DbSet<Note> Note { get; set; }
    public DbSet<Module> Module { get; set; }
    public DbSet<Branche> Branche { get; set; }
    public DbSet<Entree> Entree { get; set; }
    public DbSet<TypeTravail> TypeTravail { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        // Récupère le dossier où se trouve l'exécutable
        string dbPath = Path.Combine(AppContext.BaseDirectory, "BDD.db");
    
        Console.WriteLine($"Base de données située à : {dbPath}");
    
        options.UseSqlite($"Data Source={dbPath}");
    }
}