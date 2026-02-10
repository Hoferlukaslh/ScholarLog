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
        // Créera un fichier "app.db" à côté de ton .exec
        options.UseSqlite("Data Source=/home/lukas/ES/Cours/TINF2/M5 - Génie Logiciel - Projet/Test/SQLite_test/SQLite_test/app.db");
    }
}