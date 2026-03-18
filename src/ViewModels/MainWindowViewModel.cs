using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using ScholarLog.Pages;
using ScholarLog.Data;

// fichier : MainWindowViewModel.cs

namespace ScholarLog.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    
    // cache des viewModel
    private HomeViewModel? _homeViewModel;
    private NotesViewModel? _notesViewModel;
    private JournalViewModel? _journalViewModel;
    private SettingsViewModel? _settingsViewModel;
    
    // etat de l'interface
    
    [ObservableProperty]
    private ViewModelBase? _currentPage;

    [ObservableProperty]
    private int _currentPageIndex = 0;

    [ObservableProperty]
    private bool _isSidebarOpen = true;
    

    [ObservableProperty]
    private double _splashOpacity = 1.0;

    // etat de chargement 
    
    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private int _loadingProgress = 0;

    [ObservableProperty]
    private string _loadingText = "Initialisation...";

    public MainWindowViewModel()
    {
        // Initialiser la page d'accueil par défaut
        _homeViewModel = new HomeViewModel();
        CurrentPage = _homeViewModel;
        
        WeakReferenceMessenger.Default.Register<ModuleNavigationMessage>(this, (recipient, message) =>
        {
            // 1. On prépare la page du journal
            _journalViewModel ??= new JournalViewModel();
            
            // 2. On lui donne le module qu'on a reçu dans le message
            _journalViewModel.SelectedModule = message.Module;
            
            // 3. On bascule l'affichage et on met à jour l'index du menu (2 = Journaux)
            CurrentPage = _journalViewModel;
            CurrentPageIndex = 2; 
        });
    }
    

    // commandes (Remplacent les événements Click)

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarOpen = !IsSidebarOpen;
    }

    [RelayCommand]
    private void Navigate(string destination)
    {
        switch (destination)
        {
            case "Accueil":
                _homeViewModel ??= new HomeViewModel();
                CurrentPage = _homeViewModel;
                CurrentPageIndex = 0;
                break;
            case "Notes":
                _notesViewModel ??= new NotesViewModel();
                CurrentPage = _notesViewModel;
                CurrentPageIndex = 1;
                break;
            case "Journaux":
                _journalViewModel ??= new JournalViewModel();
                CurrentPage = _journalViewModel;
                CurrentPageIndex = 2;
                break;
            case "Settings":
                _settingsViewModel ??= new SettingsViewModel();
                CurrentPage = _settingsViewModel;
                CurrentPageIndex = 3;
                break;
        }
    }

    // logique métier
    public async Task ChargerDonneesInitialesAsync()
    {
        LoadingText = "Chargement des données globales... ";
        
        Task chargementTask = AppDataService.Instance.ChargerDonneesGlobalesAsync();

        for (int i = 0; i <= 100; i += 2)
        {
            LoadingProgress = i;
            if (i >= 90 && !chargementTask.IsCompleted)
            {
                await chargementTask;
            }
            int delais = chargementTask.IsCompleted ? 5 : 70; 
            await Task.Delay(delais);
        }

        LoadingProgress = 100;
        LoadingText = "Terminé !";
        
        SplashOpacity = 0;
        
        await Task.Delay(150); // attente de la fin de l'animation
        IsLoading = false; // Le XAML réagira pour cacher le splash screen
    }
    
    public class ModuleNavigationMessage
    {
        public ModuleViewModel Module { get; }
    
        public ModuleNavigationMessage(ModuleViewModel module)
        {
            Module = module;
        }
    }
}