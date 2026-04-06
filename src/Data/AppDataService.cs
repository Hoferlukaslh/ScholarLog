/*
    Fichier      :  AppDataService.cs
    Projet       :  ScholarLog

    Description  :
        Service de gestion de données centralisé (Pattern Singleton).
        Assure le chargement initial, le stockage en mémoire vive des modules
        et contient la logique métier pour les calculs de moyennes et de tendances.

    Auteur       :  Lukas Hofer - TINF2
    Date         :  18.03.2026
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScholarLog.Data;

/// <summary>
/// Service gérant l'état global des données de l'application.
/// Fait le lien entre le DataRepository et les ViewModels de l'interface.
/// </summary>
public class AppDataService
{
    // Instance unique (Singleton)
    private static AppDataService? _instance;
    public static AppDataService Instance => _instance ??= new AppDataService();

    /// <summary> Collection des modules chargés, partagée par toute l'application. </summary>
    public ObservableRangeCollection<ModuleViewModel> Modules { get; } =
        new ObservableRangeCollection<ModuleViewModel>();

    /// <summary> Indique si le chargement initial a déjà été effectué. </summary>
    public bool IsLoaded { get; private set; }

    private AppDataService()
    {
    }

    /// <summary>
    /// Charge les données depuis la base SQLite de manière asynchrone.
    /// Initialise les modules par défaut si la base est vide.
    /// </summary>
    public async Task ChargerDonneesGlobalesAsync()
    {
        if (IsLoaded) return;

        var nouveauxModules = new List<ModuleViewModel>();

        await Task.Run(async () =>
        {
            using (var repo = new DataRepository())
            {
                var rawModules = await repo.GetModulesAsync();

                if (!rawModules.Any())
                {
                    await CreerModulesParDefautAsync(repo);
                    rawModules = await repo.GetModulesAsync();
                }

                foreach (var mod in rawModules)
                {
                    // Filtrage utilisant l'énumération TypeCours
                    var branchesTM = mod.Branches.Where(b => b.Type == TypeCours.TM).ToList();
                    var module = mod.Branches.FirstOrDefault(b => b.Type == TypeCours.M) ?? new Branche();

                    double avgTM = ObtenirMoyenne(branchesTM);
                    double noteModule = module.Notes.Count == 1 ? module.Notes[0].Valeur : 0;

                    nouveauxModules.Add(new ModuleViewModel
                    {
                        Id = mod.Id,
                        Nom = mod.Nom,
                        AvgTheory = Math.Round(avgTM, 1),
                        TravailModule = noteModule,
                        TheoryTrend = DeterminerTendance(branchesTM, avgTM),
                        Branches = mod.Branches.ToList(),
                        JournalDeTravail = mod.JournalDeTravail.ToList(),
                        TypesDeTravail = mod.TypesDeTravail.ToList()
                    });
                }
            }
        });

        foreach (var mod in nouveauxModules)
            Modules.Add(mod);

        IsLoaded = true;
    }

    /// <summary>
    /// Crée la structure de base (M0 à DIPL.) si aucune donnée n'existe.
    /// </summary>
    private async Task CreerModulesParDefautAsync(DataRepository repo)
    {
        string[] moduleNames = { "M0", "M1", "M2", "M3", "M4", "M5", "M6", "M7", "DIPL." };
        foreach (var name in moduleNames)
            await repo.AjouterModuleAsync(new Module { Nom = name });
    }

    /// <summary>
    /// Calcule la moyenne arithmétique d'une liste de branches, arrondie au 0.5 le plus proche.
    /// </summary>
    public double ObtenirMoyenne(List<Branche> liste)
    {
        double sommeDesMoyennes = 0;
        int nombreDeBranchesValides = 0;

        foreach (Branche b in liste)
        {
            if (b.Notes != null && b.Notes.Count > 0)
            {
                sommeDesMoyennes += b.CalculerMoyenne();
                nombreDeBranchesValides++;
            }
        }

        if (nombreDeBranchesValides == 0) return 0;
        return Math.Round((sommeDesMoyennes / nombreDeBranchesValides) * 2.0, MidpointRounding.AwayFromZero) / 2.0;
    }

    /// <summary>
    /// Analyse la dernière note reçue par rapport à la moyenne globale pour définir la tendance.
    /// </summary>
    public Trend DeterminerTendance(List<Branche> branches, double moyenneActuelle)
    {
        var toutesLesNotes = branches.SelectMany(b => b.Notes).OrderByDescending(n => n.Date).ToList();
        if (toutesLesNotes.Count < 2) return Trend.Stable;

        var derniereNote = toutesLesNotes.First();
        double marge = 0.2;

        if (derniereNote.Valeur > moyenneActuelle + marge) return Trend.Up;
        if (derniereNote.Valeur < moyenneActuelle - marge) return Trend.Down;
        return Trend.Stable;
    }
}