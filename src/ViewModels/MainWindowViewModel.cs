/*
    Fichier      :  MainWindowViewModel.cs
    Projet       :  ScholarLog

    Description  :
        ViewModel principal agissant comme chef d'orchestre de l'application.
        Contrôle la navigation globale entre les pages, gère l'état du menu latéral
        et orchestre l'écran de chargement initial.

    Auteur       :  Lukas Hofer - TINF2
    Date         :  19.03.2026

    Remarques    :
        - Cache les instances des sous-ViewModels (Home, Notes, Journal, Settings) pour optimiser les performances.
        - Écoute les messages de navigation (ModuleNavigationMessage) émis par d'autres ViewModels.
        - Gère la progression asynchrone (ChargerDonneesInitialesAsync) au démarrage de l'application.
*/

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using ScholarLog.Data;

namespace ScholarLog.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    // Cache des ViewModels (instanciation unique)
    private readonly HomeViewModel     _home     = new();
    private readonly NotesViewModel    _notes    = new();
    private readonly JournalViewModel  _journal  = new();
    private readonly SettingsViewModel _settings = new();

    // État de l'interface
    [ObservableProperty] private ViewModelBase? _currentPage;
    [ObservableProperty] private int            _currentPageIndex = 0;
    [ObservableProperty] private bool           _isSidebarOpen    = true;

    // État du splash screen
    [ObservableProperty] private double _splashOpacity   = 1.0;
    [ObservableProperty] private bool   _isLoading       = true;
    [ObservableProperty] private int    _loadingProgress = 0;
    [ObservableProperty] private string _loadingText     = "Initialisation...";

    public MainWindowViewModel()
    {
        CurrentPage = _home;
        
        WeakReferenceMessenger.Default.Register<ModuleNavigationMessage>(
            this,
            (recipient, message) =>
            {
                _journal.SelectedModule = message.Module;
                CurrentPage      = _journal;
                CurrentPageIndex = 2;
            });
    }

    // Commandes

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarOpen = !IsSidebarOpen;

    [RelayCommand]
    private void Navigate(string destination)
    {
        (CurrentPage, CurrentPageIndex) = destination switch
        {
            "Accueil"  => (_home     as ViewModelBase, 0),
            "Notes"    => (_notes    as ViewModelBase, 1),
            "Journaux" => (_journal  as ViewModelBase, 2),
            "Settings" => (_settings as ViewModelBase, 3),
            _          => (CurrentPage,                CurrentPageIndex),
        };
    }

    // Logique de chargement initial

    public async Task ChargerDonneesInitialesAsync()
    {
        LoadingText = "Chargement des données globales...";

        Task chargementTask = AppDataService.Instance.ChargerDonneesGlobalesAsync();

        for (int i = 0; i <= 100; i += 2)
        {
            LoadingProgress = i;

            // On attend la fin réelle du chargement avant de dépasser 90 %
            if (i >= 90 && !chargementTask.IsCompleted)
                await chargementTask;

            await Task.Delay(chargementTask.IsCompleted ? 5 : 70);
        }

        LoadingProgress = 100;
        LoadingText     = "Terminé !";
        SplashOpacity   = 0;

        await Task.Delay(150); // laisse l'animation d'opacité se terminer
        IsLoading = false;
    }

    // Message de navigation inter-pages

    public sealed class ModuleNavigationMessage
    {
        public ModuleViewModel Module { get; }

        public ModuleNavigationMessage(ModuleViewModel module)
        {
            Module = module;
        }
    }
}