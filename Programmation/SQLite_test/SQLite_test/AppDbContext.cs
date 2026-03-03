namespace SQLite_test;

using Microsoft.EntityFrameworkCore;

public class MonDbContext : DbContext
{

    // Tables de la base de donnée
    public DbSet<Note> Note { get; set; }
    public DbSet<Module> Module { get; set; }
    public DbSet<Branche> Branche { get; set; }
    public DbSet<Entree> Entree { get; set; }
    public DbSet<TypeTravail> TypeTravail { get; set; }
    


    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        // Récupère le dossier où se trouve l'exécutable (ton dossier bin/.../net10.0/)
        string dbPath = Path.Combine(AppContext.BaseDirectory, "app.db");
    
        Console.WriteLine($"Base de données située à : {dbPath}");
    
        options.UseSqlite($"Data Source={dbPath}");
    }
}