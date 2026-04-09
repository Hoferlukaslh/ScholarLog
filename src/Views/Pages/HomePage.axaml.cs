/*
    Fichier      :  HomePage.axaml.cs
    Projet       :  ScholarLog

    Description  :
        Code-behind de la vue HomePage.
        Initialise la collection de modules affichés dans la page
        principale en récupérant les données depuis la base SQLite de manière asynchrone.

    Auteur       :  Lukas Hofer - TINF2
    Date         :  19.03.2026
*/


using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using ScholarLog.Data;
using ScholarLog.ViewModels;
using ScholarLog.Views.Pages;

namespace ScholarLog.Views.Pages;

public partial class HomePage : UserControl
{
    // évenement de navigation 
    public event EventHandler<ModuleViewModel>? NavigationVersJournalDemandee;
    
    private CancellationTokenSource? _closeCts;

    //  propriété d'animation UI

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


    private Grid? _mjetGrid;
    private Grid? _moduleGrid;

    public HomePage()
    {
        InitializeComponent();

        _mjetGrid = this.FindControl<Grid>("MJETBrancheGraph");
        _moduleGrid = this.FindControl<Grid>("ModuleEtJournal");
    }

    

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HomeViewModel.SelectedModule) && this.DataContext is HomeViewModel vm)
        {
            var newVal = vm.SelectedModule;

            // déclenche l'animation d'ouverture
            if (_lastSelectedModule == null && newVal != null)
            {
                _closeCts?.Cancel();
                RightPanelWidthStar = 3.0;
                BottomPanelHeightStar = 40.0;
            }
            // déclenche l'animation de fermeture
            else if (_lastSelectedModule != null && newVal == null)
            {
                RightPanelWidthStar = 0.0;
                BottomPanelHeightStar = 0.0;

                _closeCts?.Cancel();
                _closeCts = new CancellationTokenSource();
                var token = _closeCts.Token;

                Task.Delay(350, token).ContinueWith(_ =>
                        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (vm.SelectedModule == null) vm.DisplayedModule = null;
                        }),
                    token,
                    TaskContinuationOptions.OnlyOnRanToCompletion,
                    TaskScheduler.Default);
            }

            _lastSelectedModule = newVal;
        }
    }


    // applique les valeurs animées aux GridLength en temps réel
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Gestion du DataContext (Prévention des fuites de mémoire)
        if (change.Property == DataContextProperty)
        {
            if (change.OldValue is HomeViewModel oldVm)
            {
                oldVm.PropertyChanged -= Vm_PropertyChanged;
                oldVm.NavigationVersJournalDemandee -= Vm_NavigationVersJournalDemandee;
            }

            if (change.NewValue is HomeViewModel newVm)
            {
                newVm.PropertyChanged += Vm_PropertyChanged;
                newVm.NavigationVersJournalDemandee += Vm_NavigationVersJournalDemandee;
            }
        }
        // Panneau de droite
        else if (change.Property == RightPanelWidthStarProperty)
        {
            double val = Math.Max(0, change.GetNewValue<double>());

            _mjetGrid.ColumnDefinitions[1].Width = new GridLength(val, GridUnitType.Star);
        }
        // Panneau du bas (Journal)
        else if (change.Property == BottomPanelHeightStarProperty)
        {
            double val = Math.Max(0, change.GetNewValue<double>());

            _moduleGrid.RowDefinitions[1].Height = new GridLength(val, GridUnitType.Star);
        }
    }

    private void Vm_NavigationVersJournalDemandee(object? sender, ModuleViewModel module)
    {
        NavigationVersJournalDemandee?.Invoke(this, module);
    }
}