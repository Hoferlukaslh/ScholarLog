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

namespace ScholarLog.Views;

public partial class MainWindow : Window
{
    private DispatcherTimer? _lightTimer;
    private readonly Random _random = new Random();
    private int _lastPageIndex = 0; // Pour calculer le sens de l'animation des pages

    public MainWindow()
    {
        InitializeComponent();

        this.Loaded += MainWindow_Loaded;
        // On s'abonne au changement de DataContext pour écouter le ViewModel
        this.DataContextChanged += MainWindow_DataContextChanged;
    }

    private void MainWindow_DataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            // Dès qu'une propriété du ViewModel change, on vérifie si on doit animer l'UI
            vm.PropertyChanged += ViewModel_PropertyChanged;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var vm = (MainWindowViewModel)DataContext!;

        // Si la page a changé : on met à jour la direction de l'animation et le curseur
        if (e.PropertyName == nameof(MainWindowViewModel.CurrentPageIndex))
        {
            UpdatePageTransitionDirection(vm.CurrentPageIndex);
            MoveNavCursor(vm.CurrentPageIndex);
            _lastPageIndex = vm.CurrentPageIndex;
        }
        // Si l'état du menu a changé : on lance l'animation de la barre latérale
        else if (e.PropertyName == nameof(MainWindowViewModel.IsSidebarOpen))
        {
            AnimateSidebar(vm.IsSidebarOpen);
        }
    }

    private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        // Timer pour les lumières de fond animées
        _lightTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(7.5)
        };
        _lightTimer.Tick += (s, ev) => MoveLights();
        _lightTimer.Start();
        MoveLights();

        // Positionnement initial du curseur de navigation
        MoveNavCursor(0);

        // Déclenche le chargement depuis le ViewModel
        if (DataContext is MainWindowViewModel vm)
        {
            await vm.ChargerDonneesInitialesAsync();
        }
    }


    // logique purement visuelle (animations & geométrie) 
    private void MoveLights()
    {
        double windowWidth = this.Bounds.Width > 0 ? this.Bounds.Width : 1280;
        double windowHeight = this.Bounds.Height > 0 ? this.Bounds.Height : 720;

        void SetRandomPosition(Ellipse light)
        {
            double newX = _random.NextDouble() * windowWidth - (light.Width / 2);
            double newY = _random.NextDouble() * windowHeight - (light.Height / 2);

            // Formatage neutre obligatoire pour Avalonia (évite les virgules suisses/françaises "10,5px" qui font crasher le parser)
            string xStr = newX.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string yStr = newY.ToString(System.Globalization.CultureInfo.InvariantCulture);

            // Déplacement matériel ultra-léger
            light.RenderTransform = TransformOperations.Parse($"translate({xStr}px, {yStr}px)");
        }

        if (this.FindControl<Ellipse>("Light1") is { } l1) SetRandomPosition(l1);
        if (this.FindControl<Ellipse>("Light2") is { } l2) SetRandomPosition(l2);
    }

    private void AnimateSidebar(bool isOpen)
    {
        var sidebar = this.FindControl<Grid>("Sidebar");
        var toggleIcon = this.FindControl<Label>("ToggleIcon");

        if (sidebar == null || toggleIcon == null) return;

        if (isOpen)
        {
            sidebar.Width = 212;
            toggleIcon.Classes.Remove("rotated");
            SetMenuTextOpacity(1);
        }
        else
        {
            sidebar.Width = 60;
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
            {
                control.Opacity = opacity;
            }
        }
    }

    private void MoveNavCursor(int pageIndex)
    {
        var sidebar = this.FindControl<Grid>("Sidebar");
        var navCursor = this.FindControl<Border>("NavCursor");

        // On associe l'index à son bouton physique
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
            double yPos = pointRelatif.Value.Y;
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
            {
                // Si on descend dans le menu (index plus grand), l'animation glisse vers le bas, et inversement
                slideTransition.SensManuel = newIndex > _lastPageIndex;
            }
        }
    }
}

/// <summary>
/// Animation de transition entre les pages
/// </summary>
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
        SlideInEasing = _easing;
        SlideOutEasing = _easing;
    }

    public override Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        return base.Start(from, to, SensManuel, cancellationToken);
    }
}