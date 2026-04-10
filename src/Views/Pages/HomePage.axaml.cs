/*
    Fichier      :  HomePage.axaml.cs
    Projet       :  ScholarLog

    Description  :
        Code-behind de la vue HomePage.
        Gère les animations d'ouverture/fermeture des panneaux droit et bas
        via MaxWidth/MaxHeight + Transitions Avalonia.

    Auteur       :  Lukas Hofer - TINF2
    Date         :  19.03.2026
*/

using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using ScholarLog.Data;
using ScholarLog.ViewModels;

namespace ScholarLog.Views.Pages;

public partial class HomePage : UserControl
{
    
    private const double JournalTargetHeight = 300.0;   // Hauteur cible du panneau journal
    private const double RightPanelTargetWidth = 250.0; // Largeur cible du panneau droit 

    private CancellationTokenSource? _closeCts;

    private Panel?  _rightPanel;
    private Border? _bottomPanel;

    private ModuleViewModel? _lastSelectedModule = null;

    public HomePage()
    {
        InitializeComponent();

        _rightPanel  = this.FindControl<Panel>("PanneauDroit");
        _bottomPanel = this.FindControl<Border>("PanneauJournal");
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HomeViewModel.SelectedModule) && this.DataContext is HomeViewModel vm)
        {
            var newVal = vm.SelectedModule;

            // Ouverture : on fixe DisplayedModule APRÈS avoir lancé la transition
            if (_lastSelectedModule == null && newVal != null)
            {
                _closeCts?.Cancel();

                // Mettre MaxWidth/MaxHeight à la valeur cible → déclenche la transition
                _rightPanel!.MaxWidth   = RightPanelTargetWidth;
                _bottomPanel!.MaxHeight = JournalTargetHeight;

                // Mettre DisplayedModule à jour sur le prochain tick UI, après que la transition ait démarré
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    vm.DisplayedModule = newVal;
                }, Avalonia.Threading.DispatcherPriority.Render);
            }
            // Fermeture
            else if (_lastSelectedModule != null && newVal == null)
            {
                _rightPanel!.MaxWidth   = 0;
                _bottomPanel!.MaxHeight = 0;

                _closeCts?.Cancel();
                _closeCts = new CancellationTokenSource();
                var token = _closeCts.Token;

                // Effacer DisplayedModule après la fin de l'animation (150ms)
                Task.Delay(200, token).ContinueWith(_ =>
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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Abonnement/désabonnement propre au DataContext (prévention des fuites mémoire)
        if (change.Property == DataContextProperty)
        {
            if (change.OldValue is HomeViewModel oldVm)
                oldVm.PropertyChanged -= Vm_PropertyChanged;

            if (change.NewValue is HomeViewModel newVm)
                newVm.PropertyChanged += Vm_PropertyChanged;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _closeCts?.Cancel();
        _closeCts?.Dispose();
        _closeCts = null;
    }
}