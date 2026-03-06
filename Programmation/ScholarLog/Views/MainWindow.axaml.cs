using Avalonia.Media;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using ScholarLog.Component;

namespace ScholarLog.Views
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer? _lightTimer;
        private readonly Random _random = new Random();

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            if (OperatingSystem.IsLinux())
            {
                this.Classes.Add("linux");
            }
            
            // Le timer s'active toutes les 7.5 secondes
            _lightTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(7.5)
            };
            _lightTimer.Tick += (s, ev) => MoveLights();
            _lightTimer.Start();

            // On lance le premier mouvement immédiatement
            MoveLights();
        }

        private void MoveLights()
        {
            // On récupère la taille. Si elle est de 0 (fenêtre en train de charger), on donne une valeur par défaut
            double windowWidth = this.Bounds.Width > 0 ? this.Bounds.Width : 1280;
            double windowHeight = this.Bounds.Height > 0 ? this.Bounds.Height : 720;

            void SetRandomPosition(Ellipse light)
            {
                // Calcule une nouvelle position aléatoire
                double newX = _random.NextDouble() * windowWidth - (light.Width / 2);
                double newY = _random.NextDouble() * windowHeight - (light.Height / 2);

                Canvas.SetLeft(light, newX);
                Canvas.SetTop(light, newY);
            }

            if (Light1 != null) SetRandomPosition(Light1);
            if (Light2 != null) SetRandomPosition(Light2);
        }
        
        private void ToggleSidebar_Click(object? sender, RoutedEventArgs e)
        {
            Grid sidebar = this.FindControl<Grid>("Sidebar");
            Label toggleIcon = this.FindControl<Label>("ToggleIcon");
 
            if (sidebar == null || toggleIcon == null) return;
 
            if (sidebar.Width <= 60)
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
            string[] controlNames = { 
                "TextLogo", 
                "TextAccueil", 
                "TextNotes", 
                "TextJournaux", 
                "TextCollapse", 
                "TextSettings" 
            };
 
            foreach (var name in controlNames)
            {
                Control control = this.FindControl<Control>(name);
                if (control != null)
                {
                    control.Opacity = opacity;
                }
            }
        }

        private void Setting_OnClick(object? sender, RoutedEventArgs e)
        {
            MainContentControler.Content = new SettingsPage();
        }

        private void Buttonjournaux_OnClick(object? sender, RoutedEventArgs e)
        {
            MainContentControler.Content = new JournalPage();
        }

        private void ButtonNotes_OnClick(object? sender, RoutedEventArgs e)
        {
            MainContentControler.Content = new NotesPage();
        }

        private void ButtonAccueil_OnClick(object? sender, RoutedEventArgs e)
        {
            MainContentControler.Content = new HomePage();
        }

        private async void ButtonExemple_OnClick(object? sender, RoutedEventArgs e)
        {
                MainContentControler.Content = new ExemplePage();
        }
    }
}