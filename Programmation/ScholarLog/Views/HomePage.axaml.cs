using Avalonia.Controls;
using System;
using System.Collections.ObjectModel; // Changement ici !
using CommunityToolkit.Mvvm.ComponentModel;

namespace ScholarLog.Views;

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