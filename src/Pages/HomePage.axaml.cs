using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using ScholarLog.Data;
using ScholarLog.ViewModels;

namespace ScholarLog.Pages;

public partial class HomePage : UserControl
{
    // On conserve cet événement car MainWindow y est abonné
    public event EventHandler<ModuleViewModel>? NavigationVersJournalDemandee;

    // --- PROPRIÉTÉS D'ANIMATION (Purement UI) ---
    public static readonly StyledProperty<double> RightPanelWidthStarProperty =
        AvaloniaProperty.Register<HomePage, double>(nameof(RightPanelWidthStar), 0.0);

    public static readonly StyledProperty<double> BottomPanelHeightStarProperty =
        AvaloniaProperty.Register<HomePage, double>(nameof(BottomPanelHeightStar), 0.0);

    public double RightPanelWidthStar
    {
        get => GetValue(RightPanelWidthStarProperty);
        set => SetValue(RightPanelWidthStarProperty, value);
    }

    public double BottomPanelHeightStar
    {
        get => GetValue(BottomPanelHeightStarProperty);
        set => SetValue(BottomPanelHeightStarProperty, value);
    }

    private ModuleViewModel? _lastSelectedModule = null;

    public HomePage()
    {
        InitializeComponent();
        
        // On s'abonne au changement de contexte pour écouter le ViewModel
        this.DataContextChanged += HomePage_DataContextChanged;
    }

    private void HomePage_DataContextChanged(object? sender, EventArgs e)
    {
        if (this.DataContext is HomeViewModel vm)
        {
            // On écoute les changements de données pour déclencher les animations
            vm.PropertyChanged += Vm_PropertyChanged;
            
            // On relaye l'événement de navigation du ViewModel vers l'extérieur (MainWindow)
            vm.NavigationVersJournalDemandee += (s, module) => 
            {
                NavigationVersJournalDemandee?.Invoke(this, module);
            };
        }
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HomeViewModel.SelectedModule) && this.DataContext is HomeViewModel vm)
        {
            var newVal = vm.SelectedModule;

            // Déclenche l'animation d'ouverture
            if (_lastSelectedModule == null && newVal != null)       
            {
                RightPanelWidthStar = 3.0;
                BottomPanelHeightStar = 40.0;
            }
            // Déclenche l'animation de fermeture
            else if (_lastSelectedModule != null && newVal == null)  
            {
                RightPanelWidthStar = 0.0;
                BottomPanelHeightStar = 0.0;
                
                Task.Delay(350).ContinueWith(_ => 
                    Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => 
                    {
                        if (vm.SelectedModule == null) vm.DisplayedModule = null;
                    }));
            }
            
            _lastSelectedModule = newVal;
        }
    }

    // Applique les valeurs animées aux GridLength en temps réel
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == RightPanelWidthStarProperty)
        {
            double val = Math.Max(0, change.GetNewValue<double>());
            if (this.FindControl<Grid>("MJETBrancheGraph") is { } grid)
                grid.ColumnDefinitions[1].Width = new GridLength(val, GridUnitType.Star);
        }
        else if (change.Property == BottomPanelHeightStarProperty)
        {
            double val = Math.Max(0, change.GetNewValue<double>());
            if (this.FindControl<Grid>("ModuleEtJournal") is { } grid)
                grid.RowDefinitions[1].Height = new GridLength(val, GridUnitType.Star);
        }
    }
}