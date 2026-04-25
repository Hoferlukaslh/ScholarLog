/*
    Fichier      :  HomePage.axaml.cs
    Projet       :  ScholarLog
*/

using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Animation;
using ScholarLog.Data;
using ScholarLog.ViewModels;

namespace ScholarLog.Views.Pages;

public partial class HomePage : UserControl
{
    private const double JournalHeightPercentage = 0.35;   
    private const double RightPanelWidthPercentage = 0.30; 

    private CancellationTokenSource? _closeCts;

    private Panel?  _rightPanel;
    private Border? _bottomPanel;
    
    private Grid? _mainGrid;
    private Grid? _journalGrid;
    
    private Transitions? _rightPanelTransitions;
    private Transitions? _bottomPanelTransitions;

    private ModuleViewModel? _lastSelectedModule = null;

    public static readonly StyledProperty<int> GridColumnsProperty =
        AvaloniaProperty.Register<HomePage, int>(nameof(GridColumns), 3);

    public int GridColumns
    {
        get => GetValue(GridColumnsProperty);
        set => SetValue(GridColumnsProperty, value);
    }

    public HomePage()
    {
        InitializeComponent();

        _rightPanel  = this.FindControl<Panel>("PanneauDroit");
        _bottomPanel = this.FindControl<Border>("PanneauJournal");
        
        _mainGrid = this.FindControl<Grid>("MJETBrancheGraph");
        _journalGrid = this.FindControl<Grid>("ModuleEtJournal");

        _rightPanelTransitions = _rightPanel!.Transitions;
        _bottomPanelTransitions = _bottomPanel!.Transitions;
    }

    private void ModulesListBox_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        double width = e.NewSize.Width;
        
        if (width < 300) GridColumns = 1;
        else if (width < 500) GridColumns = 2;
        else if (width < 1200) GridColumns = 3;
        else GridColumns = 3; 
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HomeViewModel.SelectedModule) && this.DataContext is HomeViewModel vm)
        {
            var newVal = vm.SelectedModule;
            
            // OUVERTURE
            if (_lastSelectedModule == null && newVal != null) 
            {
                _closeCts?.Cancel();
                
                _rightPanel!.Transitions = _rightPanelTransitions;
                _bottomPanel!.Transitions = _bottomPanelTransitions;

                double targetWidth = this.Bounds.Width * RightPanelWidthPercentage;
                double targetHeight = this.Bounds.Height * JournalHeightPercentage;

                _mainGrid!.ColumnDefinitions[2].Width = GridLength.Auto;
                _journalGrid!.RowDefinitions[2].Height = GridLength.Auto;

                _rightPanel.Width = targetWidth;
                _bottomPanel.Height = targetHeight;

                _rightPanel.MaxWidth = targetWidth;
                _bottomPanel.MaxHeight = targetHeight;

                Avalonia.Threading.Dispatcher.UIThread.Post(() => vm.DisplayedModule = newVal);

                Task.Delay(200).ContinueWith(_ => Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
                    if (vm.SelectedModule != null) {
                        _rightPanel.Transitions = null;
                        _bottomPanel.Transitions = null;

                        _mainGrid.ColumnDefinitions[2].Width = new GridLength(_rightPanel.Bounds.Width, GridUnitType.Pixel);
                        _journalGrid.RowDefinitions[2].Height = new GridLength(_bottomPanel.Bounds.Height, GridUnitType.Pixel);

                        _rightPanel.Width = double.NaN;
                        _bottomPanel.Height = double.NaN;

                        _rightPanel.MaxWidth = double.PositiveInfinity;
                        _bottomPanel.MaxHeight = double.PositiveInfinity;
                    }
                }));
            }
            // FERMETURE
            else if (_lastSelectedModule != null && newVal == null) 
            {
                _closeCts?.Cancel();
                _closeCts = new CancellationTokenSource();
                var token = _closeCts.Token;

                _rightPanel!.Transitions = null;
                _bottomPanel!.Transitions = null;

                _rightPanel.Width = _rightPanel.Bounds.Width;
                _bottomPanel.Height = _bottomPanel.Bounds.Height;
                
                _rightPanel.MaxWidth = _rightPanel.Bounds.Width;
                _bottomPanel.MaxHeight = _bottomPanel.Bounds.Height;

                _mainGrid!.ColumnDefinitions[2].Width = GridLength.Auto;
                _journalGrid!.RowDefinitions[2].Height = GridLength.Auto;

                Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                    
                    _rightPanel.Transitions = _rightPanelTransitions;
                    _bottomPanel.Transitions = _bottomPanelTransitions;

                    _rightPanel.MaxWidth = 0;
                    _bottomPanel.MaxHeight = 0;
                    
                }, Avalonia.Threading.DispatcherPriority.Render);

                Task.Delay(200, token).ContinueWith(_ =>
                        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (vm.SelectedModule == null) vm.DisplayedModule = null;
                        }),
                    token,
                    TaskContinuationOptions.OnlyOnRanToCompletion,
                    TaskScheduler.Default);
            }
            // CHANGEMENT DE MODULE ALORS QUE LES PANNEAUX SONT DÉJÀ OUVERTS
            else if (_lastSelectedModule != null && newVal != null)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => vm.DisplayedModule = newVal);
            }
            
            _lastSelectedModule = newVal;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == BoundsProperty && _lastSelectedModule != null)
        {
            if (_rightPanel!.Transitions == null)
            {
                var newBounds = (Rect)change.NewValue!;
                double newWidth = newBounds.Width * RightPanelWidthPercentage;
                double newHeight = newBounds.Height * JournalHeightPercentage;

                _mainGrid!.ColumnDefinitions[2].Width = new GridLength(newWidth, GridUnitType.Pixel);
                _journalGrid!.RowDefinitions[2].Height = new GridLength(newHeight, GridUnitType.Pixel);
            }
        }

        if (change.Property == DataContextProperty)
        {
            if (change.OldValue is HomeViewModel oldVm)
                oldVm.PropertyChanged -= Vm_PropertyChanged;

            if (change.NewValue is HomeViewModel newVm)
            {
                // Quand on revient sur la page, on force l'oubli du module pour se calquer sur le visuel
                newVm.SelectedModule = null;
                _lastSelectedModule = null;

                newVm.PropertyChanged += Vm_PropertyChanged;
            }
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _closeCts?.Cancel();
        _closeCts?.Dispose();
        _closeCts = null;

        // Prévention des fuites de mémoire
        if (this.DataContext is HomeViewModel vm)
        {
            vm.PropertyChanged -= Vm_PropertyChanged;
        }
    }
}