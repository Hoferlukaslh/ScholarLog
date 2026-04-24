/*
    Fichier      :  HomePage.axaml.cs
    Projet       :  ScholarLog

    Description  :
        Code-behind de la vue HomePage.
        Gère les animations d'ouverture/fermeture des panneaux droit et bas
        via MaxWidth/MaxHeight + Transitions Avalonia.
        Les dimensions s'adaptent dynamiquement en pourcentage de la fenêtre sans lag.

    Auteur       :  Lukas Hofer - TINF2
    Date         :  25.04.2026
*/

using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Animation; // Ajout nécessaire pour la classe Transitions
using ScholarLog.Data;
using ScholarLog.ViewModels;

namespace ScholarLog.Views.Pages;

public partial class HomePage : UserControl
{
    private const double JournalHeightPercentage = 0.35;   // 35% de la hauteur totale
    private const double RightPanelWidthPercentage = 0.30; // 30% de la largeur totale

    private CancellationTokenSource? _closeCts;

    private Panel?  _rightPanel;
    private Border? _bottomPanel;
    
    // On sauvegarde les transitions définies dans le XAML
    private Transitions? _rightPanelTransitions;
    private Transitions? _bottomPanelTransitions;

    private ModuleViewModel? _lastSelectedModule = null;

    public HomePage()
    {
        InitializeComponent();

        _rightPanel  = this.FindControl<Panel>("PanneauDroit");
        _bottomPanel = this.FindControl<Border>("PanneauJournal");

        // Récupération des transitions XAML à l'initialisation
        _rightPanelTransitions = _rightPanel!.Transitions;
        _bottomPanelTransitions = _bottomPanel!.Transitions;
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HomeViewModel.SelectedModule) && this.DataContext is HomeViewModel vm)
        {
            var newVal = vm.SelectedModule;

            // Ouverture
            if (_lastSelectedModule == null && newVal != null)
            {
                _closeCts?.Cancel();

                double targetWidth = this.Bounds.Width * RightPanelWidthPercentage;
                double targetHeight = this.Bounds.Height * JournalHeightPercentage;

                _rightPanel!.Width = targetWidth;
                _bottomPanel!.Height = targetHeight;

                _rightPanel!.MaxWidth = targetWidth;
                _bottomPanel!.MaxHeight = targetHeight;

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

        // --- GESTION DU REDIMENSIONNEMENT DE LA FENÊTRE ---
        if (change.Property == BoundsProperty && _lastSelectedModule != null)
        {
            var newBounds = (Rect)change.NewValue!;
            double newWidth = newBounds.Width * RightPanelWidthPercentage;
            double newHeight = newBounds.Height * JournalHeightPercentage;

            // 1. On coupe temporairement les animations pour éviter l'effet "élastique"
            _rightPanel!.Transitions = null;
            _bottomPanel!.Transitions = null;

            // 2. On redimensionne instantanément
            _rightPanel.Width = newWidth;
            _rightPanel.MaxWidth = newWidth;

            _bottomPanel.Height = newHeight;
            _bottomPanel.MaxHeight = newHeight;

            // 3. On remet les animations actives pour la prochaine fermeture (via Post pour attendre la fin du rendu en cours)
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (_rightPanel != null) _rightPanel.Transitions = _rightPanelTransitions;
                if (_bottomPanel != null) _bottomPanel.Transitions = _bottomPanelTransitions;
            }, Avalonia.Threading.DispatcherPriority.Render);
        }

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