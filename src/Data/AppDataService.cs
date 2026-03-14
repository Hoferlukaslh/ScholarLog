using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ScholarLog.Data;

public class AppDataService
{
    // Instance unique (Singleton)
    private static AppDataService? _instance;
    public static AppDataService Instance => _instance ??= new AppDataService();

    // Collection globale partagée dans toute l'app
    public ObservableCollection<ModuleViewModel> Modules { get; } = new ObservableCollection<ModuleViewModel>();
    
    public bool IsLoaded { get; private set; }

    private AppDataService() { }

    public async Task ChargerDonneesGlobalesAsync()
    {
        if (IsLoaded) return; // Sécurité pour ne jamais charger 2 fois

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
                        JournalDeTravail = mod.JournalDeTravail.ToList()
                    });
                }
            }
        });

        foreach (var mod in nouveauxModules) 
            Modules.Add(mod);
            
        IsLoaded = true;
    }

    private async Task CreerModulesParDefautAsync(DataRepository repo)
    {
        string[] moduleNames = { "M0", "M1", "M2", "M3", "M4", "M5", "M6", "M7", "DIPL." };
        foreach (var name in moduleNames)
            await repo.AjouterModuleAsync(new ScholarLog.Data.Module { Nom = name });
    }

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