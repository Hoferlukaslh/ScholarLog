/*
    Fichier      :  AppDbContext.cs
    Projet       :  ScholarLog

    Description  :
        Définit le contexte de base de données Entity Framework Core.
        Configure la connexion à la base SQLite, définit les tables (DbSet)
        et optimise les performances via l'utilisation de modèles compilés.

    Auteur       :  Lukas Hofer - TINF2
    Date         :  18.03.2026
*/

using System;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace ScholarLog.Data;

/// <summary>
/// Contexte de base de données principal de l'application.
/// Assure la liaison entre les classes C# (Models) et les tables SQLite.
/// </summary>
public class MonDbContext : DbContext
{
    /// <summary> Table stockant les notes individuelles. </summary>
    public DbSet<Note> Note { get; set; }

    /// <summary> Table stockant les modules d'enseignement. </summary>
    public DbSet<Module> Module { get; set; }

    /// <summary> Table stockant les branches (cours) liées aux modules. </summary>
    public DbSet<Branche> Branche { get; set; }

    /// <summary> Table stockant les entrées du journal de travail (suivi du temps). </summary>
    public DbSet<Entree> Entree { get; set; }

    /// <summary> Table stockant les catégories de travaux personnalisées. </summary>
    public DbSet<TypeTravail> TypeTravail { get; set; }

    /// <summary> Table stockant les archives CBZ (BLOB) liées aux notes. </summary>
    public DbSet<NoteArchive> NoteArchive { get; set; }

    /// <summary>
    /// Configure le moteur de base de données et le chemin du fichier de stockage.
    /// </summary>
    /// <param name="options">Constructeur d'options pour le contexte.</param>
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        // Utilise le chemin choisi par l'utilisateur, ou le défaut dans AppData
        string dbPath = AppSettingsService.Instance.EffectiveDatabasePath;

        Console.WriteLine($"Base de données située à : {dbPath}");

        // S'assure que le dossier parent existe (cas d'un premier lancement)
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        options
            .UseSqlite($"Data Source={dbPath}")
            .UseModel(ScholarLog.Data.CompiledModels.MonDbContextModel.Instance);

        /* Note de maintenance :
           En cas de modification du fichier models.cs, il faut régénérer
           les CompiledModels avec la commande suivante dans le terminal :

           dotnet ef dbcontext optimize -c MonDbContext -o CompiledModels --namespace ScholarLog.Data.CompiledModels
        */
    }
}