/*
    Fichier      :  HomePage.axaml.cs
    Projet       :  ScholarLog

    Description  :
        Code-behind de la vue HomePage.
        Initialise la collection de modules affichés dans la page
        principale et fournit les données utilisées pour l'interface.

    Auteur       :  Lukas Hofer - TINF2
    Date         :  10.03.2026

    Remarques    :
        - /!\ Le code-behind est exclusivement du test, pas d'implémentation final actuellement. /!\
        - Les données sont actuellement générées aléatoirement pour les tests.
        - La collection Modules est liée à l'ItemsControl du XAML.
        - La classe Module représente un module scolaire avec ses statistiques.
*/

namespace ScholarLog.Pages;

using Avalonia.Controls;
using System;
using System.Collections.ObjectModel; 
using CommunityToolkit.Mvvm.ComponentModel;

public partial class HomePage : UserControl
{
    public ObservableCollection<Module> Modules { get; set; } = new ObservableCollection<Module>();
    
    public HomePage()
    {
        InitializeComponent();
        
        DataContext = this; 
        
        Random rng = new Random();
        
        string[] moduleNames = { "M0", "M1", "M2", "M3", "M4", "M5", "M6", "M7", "DIPL." };
        
        foreach (var name in moduleNames)
        {
            Modules.Add(new Module
            {
                Name = name,
                AvgPractice = Math.Round(rng.NextDouble() * 5 + 1, 1),
                AvgTheory = Math.Round(rng.NextDouble() * 5 + 1, 1),
                TheoryTrend = (Trend)rng.Next(0, 3),
                PracticeTrend = (Trend)rng.Next(0, 3)
            });
        }
    }
    
    
    
    public enum Trend
    {
        Up, Down, Stable
    }

    public class Module : ObservableObject
    {
        public string Name { get; set; } = string.Empty;
        public double AvgPractice { get; set; }
        public double AvgTheory { get; set; }
        public Trend TheoryTrend { get; set; }
        public Trend PracticeTrend { get; set; }

        public double GlobalAverage => (AvgPractice + AvgTheory) / 2;
    }
}