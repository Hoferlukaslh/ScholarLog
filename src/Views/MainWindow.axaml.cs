/*
    Fichier      :  MainWindow.axaml.cs
    Projet       :  ScholarLog

    Description  :
        Code-behind de la fenêtre principale. STRICTEMENT limité à la logique visuelle (UI).
        Écoute le ViewModel pour animer l'interface :
            - Déplacement aléatoire des lumières (Timer)
            - Animation du menu latéral et opacité des textes
            - Positionnement du curseur de navigation
            - Direction de l'animation de changement de page (Haut/Bas)
            - Mode overlay du menu si la fenêtre fait < 960px de large
*/

using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using ScholarLog.ViewModels;
using Avalonia.Media.Transformation;
using Avalonia.Reactive;

namespace ScholarLog.Views;

public partial class MainWindow : Window
{
    private DispatcherTimer? _lightTimer;
    private readonly Random _random = new Random();
    private int _lastPageIndex = 0;

    // Seuil en-dessous duquel le menu passe en mode overlay
    private const double OverlayThreshold = 960.0;

    // Référence au Border qui contient la sidebar (colonne 0 de la grille principale)
    private Border? _sidebarBorder;
    private Grid?   _mainGrid;      // la grille ColumnDefinitions="Auto, *"
    private bool    _isRetractedMode = false;

    public MainWindow()
    {
        InitializeComponent();

        this.Loaded += MainWindow_Loaded;
        this.DataContextChanged += MainWindow_DataContextChanged;

        // Écoute les changements de taille de la fenêtre
        this.SizeChanged += (_, e) => OnWindowResized(e.NewSize.Width);
    }

    private void MainWindow_DataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var vm = (MainWindowViewModel)DataContext!;

        if (e.PropertyName == nameof(MainWindowViewModel.CurrentPageIndex))
        {
            UpdatePageTransitionDirection(vm.CurrentPageIndex);
            MoveNavCursor(vm.CurrentPageIndex);
            _lastPageIndex = vm.CurrentPageIndex;

            // En mode overlay : refermer le menu après navigation
            if (_isRetractedMode && vm.IsSidebarOpen)
                vm.IsSidebarOpen = false;
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.IsSidebarOpen))
        {
            AnimateSidebar(vm.IsSidebarOpen);
        }
    }

    private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        // Récupère les références aux contrôles structurels
        _sidebarBorder = this.FindControl<Border>("SidebarBorder");  // à nommer dans le AXAML si besoin
        _mainGrid      = this.FindControl<Grid>("MainLayoutGrid");    // idem

        _lightTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(7.5) };
        _lightTimer.Tick += (s, ev) => MoveLights();
        _lightTimer.Start();
        MoveLights();

        MoveNavCursor(0);

        // Applique le mode initial selon la taille au démarrage
        OnWindowResized(this.Bounds.Width);

        if (DataContext is MainWindowViewModel vm)
            await vm.ChargerDonneesInitialesAsync();
    }

    private void OnWindowResized(double width)
    {
        var sidebar = this.FindControl<Grid>("Sidebar");
        var sidebarBorder = this.FindControl<Border>("SidebarBorder") ?? FindSidebarBorder();
        // Récupération du bouton de toggle
        var toggleButton = this.FindControl<Button>("ToggleButton"); 

        if (sidebar == null || sidebarBorder == null) return;

        bool shouldBeRetracted = width < OverlayThreshold;

        if (shouldBeRetracted == _isRetractedMode) return; 

        _isRetractedMode = shouldBeRetracted;

        if (_isRetractedMode)
        {
            if (toggleButton != null) toggleButton.IsVisible = false;

            var vm = DataContext as MainWindowViewModel;
            if (vm?.IsSidebarOpen == true)
                vm.IsSidebarOpen = false; 

            Grid.SetColumn(sidebarBorder, 0);
        }
        else
        {
            // afficher le bouton en mode normal (> 960px)
            if (toggleButton != null) toggleButton.IsVisible = true;
        }
    }



    /// <summary>
    /// Trouve le Border parent de la Sidebar par remontée de l'arbre visuel.
    /// Utilisé si le Border n'a pas de nom dans le AXAML.
    /// </summary>
    private Border? FindSidebarBorder()
    {
        var sidebar = this.FindControl<Grid>("Sidebar");
        return sidebar?.Parent as Border;
    }

    private void AnimateSidebar(bool isOpen)
    {
        var sidebar     = this.FindControl<Grid>("Sidebar");
        var toggleIcon  = this.FindControl<Label>("ToggleIcon");
        var sidebarBorder = FindSidebarBorder();

        if (sidebar == null || toggleIcon == null) return;

        if (isOpen)
        {
            sidebar.Width = 145;
            toggleIcon.Classes.Remove("rotated");
            SetMenuTextOpacity(1);

            // En mode overlay ouvert : élève le ZIndex pour passer au-dessus du contenu
            if (_isRetractedMode && sidebarBorder != null)
                sidebarBorder.ZIndex = 50;
        }
        else
        {
            sidebar.Width = 55;
            if (!toggleIcon.Classes.Contains("rotated"))
                toggleIcon.Classes.Add("rotated");
            SetMenuTextOpacity(0);
        }
    }

    private void SetMenuTextOpacity(double opacity)
    {
        string[] controlNames =
        {
            "TextLogo", "TextAccueil", "TextNotes",
            "TextJournaux", "TextCollapse", "TextSettings"
        };

        foreach (var name in controlNames)
        {
            if (this.FindControl<Control>(name) is { } control)
                control.Opacity = opacity;
        }
    }

    private void MoveNavCursor(int pageIndex)
    {
        var sidebar   = this.FindControl<Grid>("Sidebar");
        var navCursor = this.FindControl<Border>("NavCursor");

        Button? targetButton = pageIndex switch
        {
            0 => this.FindControl<Button>("ButtonAccueil"),
            1 => this.FindControl<Button>("ButtonNotes"),
            2 => this.FindControl<Button>("Buttonjournaux"),
            3 => this.FindControl<Button>("Setting"),
            _ => null
        };

        if (targetButton == null || navCursor == null || sidebar == null) return;

        var pointRelatif = targetButton.TranslatePoint(new Point(0, 0), sidebar);

        if (pointRelatif.HasValue)
        {
            double yPos    = pointRelatif.Value.Y;
            double decalage = targetButton.Bounds.Height > 0
                ? (targetButton.Bounds.Height - navCursor.Height) / 2
                : 6;

            Canvas.SetTop(navCursor, yPos + decalage);
        }
    }

    private void UpdatePageTransitionDirection(int newIndex)
    {
        var mainContentControler = this.FindControl<TransitioningContentControl>("MainContentControler");

        if (mainContentControler?.PageTransition is CompositePageTransition compositeTransition)
        {
            var slideTransition = compositeTransition.PageTransitions.OfType<MyPageSlide>().FirstOrDefault();
            if (slideTransition != null)
                slideTransition.SensManuel = newIndex > _lastPageIndex;
        }
    }

    private void MoveLights()
    {
        double windowWidth  = this.Bounds.Width  > 0 ? this.Bounds.Width  : 1280;
        double windowHeight = this.Bounds.Height > 0 ? this.Bounds.Height : 720;

        void SetRandomPosition(Ellipse light)
        {
            double newX = _random.NextDouble() * windowWidth  - (light.Width  / 2);
            double newY = _random.NextDouble() * windowHeight - (light.Height / 2);

            string xStr = newX.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string yStr = newY.ToString(System.Globalization.CultureInfo.InvariantCulture);

            light.RenderTransform = TransformOperations.Parse($"translate({xStr}px, {yStr}px)");
        }

        if (this.FindControl<Ellipse>("Light1") is { } l1) SetRandomPosition(l1);
        if (this.FindControl<Ellipse>("Light2") is { } l2) SetRandomPosition(l2);
    }
}

public class MyPageSlide : PageSlide
{
    private Easing _easing = new LinearEasing();
    public bool SensManuel { get; set; } = true;

    public Easing Easing
    {
        set
        {
            _easing = value;
            SlideInEasing = value;
            SlideOutEasing = value;
        }
    }

    public MyPageSlide()
    {
        SlideInEasing  = _easing;
        SlideOutEasing = _easing;
    }

    public override Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
        => base.Start(from, to, SensManuel, cancellationToken);
}