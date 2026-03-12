/*
    Fichier      :  HomePage.axaml.cs
    Projet       :  ScholarLog

    Description  :
        Code-behind de la vue HomePage.
        Initialise la collection de modules affichés dans la page
        principale en récupérant les données depuis la base SQLite de manière asynchrone.

    Auteur       :  Lukas Hofer - TINF2
    Date         :  11.03.2026
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel; 
using System.Linq;
using System.Threading.Tasks; 
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity; 
using CommunityToolkit.Mvvm.ComponentModel;
using ScholarLog.Data;

namespace ScholarLog.Pages;

public partial class HomePage : UserControl
{
    public static readonly StyledProperty<ModuleViewModel?> SelectedModuleProperty =
        AvaloniaProperty.Register<HomePage, ModuleViewModel?>(nameof(SelectedModule));

    public ModuleViewModel? SelectedModule
    {
        get => GetValue(SelectedModuleProperty);
        set => SetValue(SelectedModuleProperty, value);
    }
    
    public enum Trend {Up, Down, Stable}
    
    public ObservableCollection<ModuleViewModel> Modules { get; set; } = new ObservableCollection<ModuleViewModel>();
    
    
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // On vérifie si la propriété modifiée est notre module sélectionné
        if (change.Property == SelectedModuleProperty)
        {
            var oldVal = change.GetOldValue<ModuleViewModel?>();
            var newVal = change.GetNewValue<ModuleViewModel?>();

            // Si on passe de "rien" à un "module" -> on ouvre les panneaux
            if (oldVal == null && newVal != null)
            {
                AnimateGridsAsync(true);
            }
            // Si l'utilisateur désélectionne le module -> on referme les panneaux
            else if (oldVal != null && newVal == null)
            {
                AnimateGridsAsync(false);
            }
        }
    }
    private async void AnimateGridsAsync(bool open)
    {
        // Valeurs cibles demandées dans tes commentaires XAML
        double targetCol = open ? 3.0 : 0.0;
        double targetRow = open ? 40.0 : 0.0;

        // Valeurs actuelles de départ
        double startCol = MJETBrancheGraph.ColumnDefinitions[1].Width.Value;
        double startRow = ModuleEtJournal.RowDefinitions[1].Height.Value;

        // Configuration de l'animation
        int durationMs = 300; // Durée de l'animation (300ms)
        int fps = 60;
        int steps = durationMs * fps / 1000;
        int delay = 1000 / fps;

        for (int i = 1; i <= steps; i++)
        {
            double progress = (double)i / steps;
        
            // Fonction d'assouplissement "Quadratic Ease Out" pour un mouvement naturel (ralentit à la fin)
            double ease = 1 - (1 - progress) * (1 - progress);
        
            double currentCol = startCol + (targetCol - startCol) * ease;
            double currentRow = startRow + (targetRow - startRow) * ease;

            // Application aux définitions de la grille avec l'unité "Star" (*)
            MJETBrancheGraph.ColumnDefinitions[1].Width = new GridLength(currentCol, GridUnitType.Star);
            ModuleEtJournal.RowDefinitions[1].Height = new GridLength(currentRow, GridUnitType.Star);

            // Pause asynchrone n'impactant pas le thread UI
            await Task.Delay(delay);
        }
    
        // Par sécurité, on force les valeurs exactes de fin pour éviter les erreurs d'arrondis
        MJETBrancheGraph.ColumnDefinitions[1].Width = new GridLength(targetCol, GridUnitType.Star);
        ModuleEtJournal.RowDefinitions[1].Height = new GridLength(targetRow, GridUnitType.Star);
    }
   
    
    public HomePage()
    {
        InitializeComponent();
        DataContext = this; 
        
        this.Loaded += HomePage_Loaded;
    }
    
    private async void HomePage_Loaded(object? sender, RoutedEventArgs e)
    {
        this.Loaded -= HomePage_Loaded; 
        await ChargerDonneesAsync();
    }
    
    private async Task ChargerDonneesAsync()
    {
        var nouveauxModules = new List<ModuleViewModel>();

        // ajout "async" ici pour pouvoir utiliser "await" à l'intérieur du Task.Run
        await Task.Run(async () => 
        {
            using (var repo = new DataRepository())
            {
                var modulesDb = await repo.GetModulesAsync(); 

                if (!modulesDb.Any())
                {
                    await CreerModulesParDefautAsync(repo); 
                    modulesDb = await repo.GetModulesAsync(); 
                }

                foreach (var mod in modulesDb)
                {
                    // listes branches 
                    var branchesTM = new List<Branche>(); // théorique
                    var branchesPM = new List<Branche>(); // pratique
                    
                    foreach (var branche in mod.Branches) //Pour chaque branche du module
                    {
                        // Si Théorique -> ajout dans liste branche théorique
                        if (branche.Type == TypeCours.TM)
                            branchesTM.Add(branche);
                        
                        // Si Pratique -> ajout dans liste branche pratique
                        else if (branche.Type == TypeCours.PM) 
                            branchesPM.Add(branche);
                    }
                    
                    double avgTM = ObtenirMoyenne(branchesTM);
                    double avgPM = ObtenirMoyenne(branchesPM);


                    nouveauxModules.Add(new ModuleViewModel
                    {
                        Name = mod.Nom,
                        AvgTheory = Math.Round(avgTM, 1),
                        AvgPractice = Math.Round(avgPM, 1),
                        TheoryTrend = DeterminerTendance(branchesTM, avgTM),
                        PracticeTrend = DeterminerTendance(branchesPM, avgPM)
                    });
                }
            }
        });

        // nettoyage et actualisation
        Modules.Clear();
        foreach (var mod in nouveauxModules) Modules.Add(mod);
    }
    
    public double ObtenirMoyenne(List<Branche> liste)
    {
        double sommeDesMoyennes = 0;
        int nombreDeBranchesValides = 0;

        // 2. Parcours de la liste avec une boucle foreach
        foreach (Branche b in liste)
        {
            // vérification branche contient des notes pour éviter de fausser la moyenne
            if (b.Notes != null && b.Notes.Count > 0)
            {
                // On ajoute la moyenne de cette branche à la somme totale
                sommeDesMoyennes += b.CalculerMoyenne(); 

                nombreDeBranchesValides++;
            }
        }

        double moyenne = 0;

        // évite division par 0
        if (nombreDeBranchesValides != 0)
            moyenne = sommeDesMoyennes / nombreDeBranchesValides;

        return moyenne;
    }
    
    private Trend DeterminerTendance(List<Branche> branches, double moyenneActuelle)
    {
        var toutesLesNotes = branches
            .SelectMany(b => b.Notes)
            .OrderByDescending(n => n.Date)
            .ToList();

        if (toutesLesNotes.Count < 2)
            return Trend.Stable;

        var derniereNote = toutesLesNotes.First();
        double marge = 0.2;

        if (derniereNote.Valeur > moyenneActuelle + marge)
            return Trend.Up;
        
        else if (derniereNote.Valeur < moyenneActuelle - marge)
            return Trend.Down;
        
        else
            return Trend.Stable;
    }
    
    private async Task CreerModulesParDefautAsync(DataRepository repo)
    {
        string[] moduleNames = { "M0", "M1", "M2", "M3", "M4", "M5", "M6", "M7", "DIPL." };
        
        foreach (var name in moduleNames)
            await repo.AjouterModuleAsync(new ScholarLog.Data.Module { Nom = name });
    }


    public class ModuleViewModel : ObservableObject
    {
        
        public string Name { get; set; } = string.Empty;
        public string ShortName => Name.Length <= 3 ? Name : Name.Substring(0, 3).ToUpper();
        public double AvgPractice { get; set; }
        public double AvgTheory { get; set; }
        public Trend TheoryTrend { get; set; }
        public Trend PracticeTrend { get; set; }

        public double GlobalAverage
        {
            get
            {
                double moyenne;
                
                // Si les deux moyennes sont présentes, on fait la moyenne des deux
                if (AvgPractice > 0 && AvgTheory > 0)
                    moyenne =  (AvgPractice + AvgTheory) / 2.0;
                else // Sinon, on retourne celle qui n'est pas à zéro (ou 0 si aucune n'a de note)
                    moyenne =  AvgPractice + AvgTheory; 
                
                return moyenne;
            }
        }
    }

    private void OnModuleSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        Console.WriteLine($"module : {SelectedModule.Name}");
    }
}