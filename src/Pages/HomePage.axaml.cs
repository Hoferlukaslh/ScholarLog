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
using ScholarLog.Data;

namespace ScholarLog.Pages;

public partial class HomePage : UserControl
{
    public static readonly StyledProperty<ModuleViewModel?> SelectedModuleProperty =
        AvaloniaProperty.Register<HomePage, ModuleViewModel?>(nameof(SelectedModule));
    
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
    
    
    public ModuleViewModel? SelectedModule
    {
        get => GetValue(SelectedModuleProperty);
        set => SetValue(SelectedModuleProperty, value);
    }
    
    
    
    public ObservableCollection<ModuleViewModel> Modules { get; set; } = new ObservableCollection<ModuleViewModel>();
    public ObservableCollection<BrancheViewModel> BranchesTM { get; set; } = new ObservableCollection<BrancheViewModel>();
    public ObservableCollection<BrancheViewModel> BranchesM { get; set; } = new ObservableCollection<BrancheViewModel>();
    public ObservableCollection<TypeTravailViewModel> TypesTravail { get; set; } = new ObservableCollection<TypeTravailViewModel>();
    public ObservableCollection<Entree> Journal { get; set; } = new ObservableCollection<Entree>();
    
    
    
    
    // Clique sur un model de module (carte)
    private void OnModuleSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (SelectedModule == null) return; 

        // nettoyage 
        BranchesTM.Clear();
        BranchesM.Clear();
        Journal.Clear();
        TypesTravail.Clear();
        
        var typestravail = new List<TypeTravailViewModel>();
        
        // parcours de chaque entrée du journal
        foreach (var entree in SelectedModule.JournalDeTravail)
        {
            bool existeDeja = false;

            // Recherche manuelle par identifiant unique
            for (int i = 0; i < typestravail.Count; i++)
            {
                if (typestravail[i].Id == entree.Type.Id) 
                    existeDeja = true;
            }

            // insertion uniquement si l'élément est absent
            if (!existeDeja)
            {
                TypeTravailViewModel wm = new TypeTravailViewModel
                {
                    Id = entree.Type.Id,
                    Nom = entree.Type.Nom,
                    ModuleId = entree.Type.ModuleId,
                    Somme = 0.00
                };
        
                typestravail.Add(wm);
            }
        }
        
        // pour chaque entrée du journal
        foreach (var entree in SelectedModule.JournalDeTravail)
        {
            Journal.Add(entree);

            for (int i = 0; i < typestravail.Count; i++)
            {
                if (entree.TypeTravailId == typestravail[i].Id)
                    typestravail[i].Somme += entree.Duree;
            }
        }

        foreach (var type in typestravail)
            TypesTravail.Add(type);
        
        DessinerGraphiqueDonut(TypesTravail);

        // dispatching
        foreach (var branche in SelectedModule.Branches)
        {
            double moy = branche.CalculerMoyenne();
        
            var vm = new BrancheViewModel
            {
                Nom = branche.Nom,
                Moyenne = Math.Round(moy, 1),
                Type = branche.Type,
                Notes = branche.Notes.ToList(),
                BrancheTrend = DeterminerTendance(new List<Branche> { branche }, moy)
            };

            // ajout dans la bonne collection selon le type
            switch (branche.Type)
            {
                case TypeCours.TM:
                    BranchesTM.Add(vm);
                    break;
                
                case TypeCours.M:
                    BranchesM.Add(vm);
                    break;
            }
        }
        
        
    }


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
        int durationMs = 150; // Durée de l'animation (300ms)
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
   
    
    
    private async Task ChargerDonneesAsync()
    {
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
                    // listes branches 
                    var branchesTM = new List<Branche>(); // théorique
                    var module = new Branche();
                    
                    foreach (var branche in mod.Branches) //Pour chaque branche du module
                    {
                        // Si Théorique -> ajout dans liste branche théorique
                        if (branche.Type == TypeCours.TM)
                            branchesTM.Add(branche);
                        
                        // Si Module -> ajoute travail module
                        else if (branche.Type == TypeCours.M)
                            module = branche;
                    }
                    
                    double avgTM = ObtenirMoyenne(branchesTM);
                    double noteModule = 0;
                    
                    if (module.Notes.Count == 1)
                        noteModule =module.Notes[0].Valeur;
                    

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

        // nettoyage et actualisation
        Modules.Clear();
        foreach (var mod in nouveauxModules) Modules.Add(mod);
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

        double moyenne = 0;

        if (nombreDeBranchesValides != 0)
            moyenne = sommeDesMoyennes / nombreDeBranchesValides;
        
        return Math.Round(moyenne * 2.0, MidpointRounding.AwayFromZero) / 2.0;
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
    

    private void OnDonutCanvasSizeChanged(object? sender, Avalonia.Controls.SizeChangedEventArgs e)
    {
        if (TypesTravail != null && TypesTravail.Any())
            DessinerGraphiqueDonut(TypesTravail);
    }

    /// <summary>
    /// Dessine le graphique en anneau (Donut).
    /// Fonction généré par IA GEMINI,
    /// code à revoir + à documenter.
    /// </summary>
    private void DessinerGraphiqueDonut(IEnumerable<TypeTravailViewModel> donnees)
    {
        DonutCanvas.Children.Clear();
        LegendPanel.Children.Clear();

        // 1. Dimensions allouées dynamiquement
        double largeur = DonutCanvas.Bounds.Width;
        double hauteur = DonutCanvas.Bounds.Height;

        if (largeur <= 0 || hauteur <= 0) return;

        double total = donnees.Sum(d => d.Somme);
        if (total <= 0) return;

        // 2. Centrage mathématique
        double centerX = largeur / 2;
        double centerY = hauteur / 2;

        // On prend la plus petite dimension pour le diamètre afin que le cercle entre parfaitement
        double dimensionMinimale = Math.Min(largeur, hauteur);
        
        // Marge de 5 pixels pour ne pas toucher les bords
        double outerRadius = (dimensionMinimale / 2) - 5; 
        if (outerRadius <= 0) return; // Sécurité si l'espace est trop petit
        
        double innerRadius = outerRadius * 0.6; 

        double currentAngle = -Math.PI / 2; 
        string[] colorPalette = { "#CC4A90E2", "#CC50E3C2", "#CCF5A623", "#CCD0021B", "#CCBD10E0", "#CCB8E986", "#CC8B572A" };
        int colorIndex = 0;

        foreach (var item in donnees)
        {
            if (item.Somme <= 0) continue;

            // 3. Calcul de l'angle
            double proportion = item.Somme / total;
            double angleProportion = proportion * 2 * Math.PI;

            if (angleProportion >= 2 * Math.PI)
                angleProportion = 2 * Math.PI - 0.001; 

            double nextAngle = currentAngle + angleProportion;
            int isLargeArc = angleProportion > Math.PI ? 1 : 0; 

            // 4. Trigonométrie SVG
            double startX_Outer = centerX + outerRadius * Math.Cos(currentAngle);
            double startY_Outer = centerY + outerRadius * Math.Sin(currentAngle);
            
            double endX_Outer = centerX + outerRadius * Math.Cos(nextAngle);
            double endY_Outer = centerY + outerRadius * Math.Sin(nextAngle);
            
            double endX_Inner = centerX + innerRadius * Math.Cos(nextAngle);
            double endY_Inner = centerY + innerRadius * Math.Sin(nextAngle);
            
            double startX_Inner = centerX + innerRadius * Math.Cos(currentAngle);
            double startY_Inner = centerY + innerRadius * Math.Sin(currentAngle);

            string pathData = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "M {0},{1} A {2},{2} 0 {3} 1 {4},{5} L {6},{7} A {8},{8} 0 {3} 0 {9},{10} Z",
                startX_Outer, startY_Outer, 
                outerRadius, 
                isLargeArc, 
                endX_Outer, endY_Outer,
                endX_Inner, endY_Inner,
                innerRadius,
                startX_Inner, startY_Inner);

            var sliceColor = Avalonia.Media.SolidColorBrush.Parse(colorPalette[colorIndex % colorPalette.Length]);

            var path = new Avalonia.Controls.Shapes.Path
            {
                Data = Avalonia.Media.StreamGeometry.Parse(pathData),
                Fill = sliceColor,
                Stroke = Avalonia.Media.Brushes.Transparent, 
                StrokeThickness = 1
            };
            
            Avalonia.Controls.ToolTip.SetTip(path, $"{item.Nom} : {item.Somme:0.##}h");
            DonutCanvas.Children.Add(path);

            // 5. Légende dynamique pour le WrapPanel
            var legendItem = new Avalonia.Controls.StackPanel 
            { 
                Orientation = Avalonia.Layout.Orientation.Horizontal, 
                Spacing = 5,
                Margin = new Avalonia.Thickness(10, 2), // Espacement pour le WrapPanel
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            var colorBox = new Avalonia.Controls.Border 
            { 
                Width = 10, Height = 10, 
                CornerRadius = new Avalonia.CornerRadius(5), 
                Background = sliceColor,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            var label = new Avalonia.Controls.TextBlock 
            { 
                Text = $"{item.Nom} : {proportion:P0}", 
                FontSize = 12,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            if (this.TryFindResource("PrimaryForeground", out var fgRes) && fgRes is Avalonia.Media.IBrush fgBrush)
                label.Foreground = fgBrush;

            legendItem.Children.Add(colorBox);
            legendItem.Children.Add(label);
            LegendPanel.Children.Add(legendItem);

            currentAngle = nextAngle;
            colorIndex++;
        }
    }
}