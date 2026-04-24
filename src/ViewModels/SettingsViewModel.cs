/*
    Fichier      :  SettingsViewModel.cs
    Projet       :  ScholarLog

    Description  :
        ViewModel gérant les paramètres de l'application.
        Contrôle la sélection et la sauvegarde du chemin de la base de données
        SQLite locale ainsi que la bascule du thème (Clair/Sombre).

    Auteur       :  Lukas Hofer - TINF2
    Date         :  19.03.2026

    Remarques    :
        - Utilise OnIsDarkModeEnabledChanged pour appliquer dynamiquement le thème à l'application.
        - Communique les erreurs de validation de chemin à la vue via la propriété ShowPathError.
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using ScholarLog.Data;
using ScholarLog.Views;

namespace ScholarLog.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    // -------------------------------------------------------------------------
    //  Champs privés (propriétés manuelles — PAS de [ObservableProperty] ici)
    // -------------------------------------------------------------------------
    private string? _pathToBDD;
    private bool    _showPathError;
    private string  _statusMessage = string.Empty;

    // -------------------------------------------------------------------------
    //  Propriétés générées par le Toolkit (aucun doublon avec les champs ci-dessus)
    // -------------------------------------------------------------------------
    [ObservableProperty] private bool _isDarkModeEnabled;

    [ObservableProperty] private bool              _isEditModuleModalOpen = false;
    [ObservableProperty] private ModuleViewModel?  _editingModule;
    [ObservableProperty] private string            _nouvelleBrancheNom = string.Empty;
    [ObservableProperty] private ObservableCollection<Branche> _modalBranches = new();

    [ObservableProperty] private ObservableRangeCollection<ModuleViewModel> _modules = new();

    // Dialogue de confirmation
    [ObservableProperty] private bool   _isConfirmDialogOpen  = false;
    [ObservableProperty] private string _confirmDialogMessage  = string.Empty;
    
    [ObservableProperty] private bool _isReloading = false;
    
    

    // Stockage temporaire pour l'action de suppression en attente
    private object?        _itemToDelete;
    private Action<object>? _deleteAction;
    
    private bool _disableEffects;

    public bool DisableEffects
    {
        get => _disableEffects;
        set
        {
            if (SetProperty(ref _disableEffects, value))
            {
                AppSettingsService.Instance.Current.DisableEffects = value;
                AppSettingsService.Instance.Save();

                // Application à chaud via la MainWindow
                if (Avalonia.Application.Current?.ApplicationLifetime 
                    is IClassicDesktopStyleApplicationLifetime { MainWindow: MainWindow win })
                {
                    win.AppliquerEffets(!value);
                }
            }
        }
    }

    // -------------------------------------------------------------------------
    //  Propriétés manuelles (logique métier dans le setter)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Chemin vers la base de données choisi par l'utilisateur.
    /// Valide et persiste le chemin dès qu'il est modifié.
    /// </summary>
    public string? PathToBDD
    {
        get => _pathToBDD;
        // Le setter fait uniquement son travail : stocker la valeur
        set => SetProperty(ref _pathToBDD, value);
    }
    
    [RelayCommand]
    public async Task UpdateDatabasePathAsync(string newPath)
    {
        if (!AppSettingsService.IsValidDatabasePath(newPath))
        {
            ShowPathError = true;
            return;
        }

        PathToBDD = newPath;
        AppSettingsService.Instance.Current.DatabasePath = newPath;
        AppSettingsService.Instance.Save();

        // Ici, nous sommes dans une méthode Async, on peut utiliser "await" en toute sécurité !
        await RechargerDonneesAsync(); 
    }
    
    private async Task RechargerDonneesAsync()
    {
        IsReloading   = true;
        StatusMessage = "⏳ Chargement de la nouvelle base de données...";

        try
        {
            AppDataService.Instance.Reset();
            await AppDataService.Instance.ChargerDonneesGlobalesAsync();

            // Repointe la liste locale sur la collection rechargée
            Modules       = AppDataService.Instance.Modules;
            StatusMessage = "Base de données chargée avec succès.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erreur : {ex.Message}";
            Console.WriteLine($"[Reload] {ex}");
        }
        finally
        {
            IsReloading = false;
        }
    }
    
    

    /// <summary> Déclencheur pour signaler à la Vue d'afficher le Flyout d'erreur. </summary>
    public bool ShowPathError
    {
        get => _showPathError;
        set => SetProperty(ref _showPathError, value);
    }

    /// <summary> Message de statut affiché sous le champ de chemin. </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary> Chemin effectif affiché dans l'UI (personnalisé ou défaut dans AppData). </summary>
    public string DisplayedPath => AppSettingsService.Instance.EffectiveDatabasePath;

    // -------------------------------------------------------------------------
    //  Constructeur
    // -------------------------------------------------------------------------

    public SettingsViewModel()
    {
        if (Application.Current != null)
        {
            //IsDarkModeEnabled = Application.Current.ActualThemeVariant == ThemeVariant.Dark;
            IsDarkModeEnabled = AppSettingsService.Instance.Current.IsDarkMode;
            Modules           = AppDataService.Instance.Modules;
        }

        _pathToBDD      = AppSettingsService.Instance.Current.DatabasePath;
        _disableEffects = AppSettingsService.Instance.Current.DisableEffects; 
    }

    // -------------------------------------------------------------------------
    //  Callbacks du Toolkit
    // -------------------------------------------------------------------------

    /// <summary> Appelée automatiquement quand IsDarkModeEnabled change. </summary>
    partial void OnIsDarkModeEnabledChanged(bool value)
    {
        if (Application.Current != null)
        {
            //Application.Current.RequestedThemeVariant = value ? ThemeVariant.Dark : ThemeVariant.Light;
            
            Application.Current.RequestedThemeVariant = value ? ThemeVariant.Dark : ThemeVariant.Light;
            AppSettingsService.Instance.Current.IsDarkMode = value;
            AppSettingsService.Instance.Save();
        }
           
    }

    // -------------------------------------------------------------------------
    //  Commandes — Chemin de la BDD
    // -------------------------------------------------------------------------

    /// <summary>
    /// Sauvegarde le chemin saisi manuellement dans le champ texte.
    /// Appelée par le bouton "Enregistrer".
    /// </summary>
    [RelayCommand]
    private void SavePath()
    {
        string cleanedPath = PathToBDD?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(cleanedPath))
        {
            ResetToDefault();
            return;
        }

        // La validation + persistance se font dans le setter de PathToBDD
        PathToBDD = cleanedPath;
    }

    /// <summary> Réinitialise vers le chemin par défaut dans AppData. </summary>
    [RelayCommand]
    private void ResetToDefault()
    {
        AppSettingsService.Instance.Current.DatabasePath = null;
        AppSettingsService.Instance.Save();
        SetProperty(ref _pathToBDD, null, nameof(PathToBDD));
        StatusMessage = "✅ Chemin réinitialisé. Redémarrez l'application pour appliquer.";
    }

    // -------------------------------------------------------------------------
    //  Commandes — Dialogue de confirmation
    // -------------------------------------------------------------------------

    [RelayCommand]
    private void ConfirmDelete()
    {
        if (_deleteAction != null && _itemToDelete != null)
            _deleteAction(_itemToDelete);

        CancelDelete();
    }

    [RelayCommand]
    private void CancelDelete()
    {
        IsConfirmDialogOpen = false;
        _itemToDelete       = null;
        _deleteAction       = null;
    }

    // -------------------------------------------------------------------------
    //  Commandes — Modules
    // -------------------------------------------------------------------------

    [RelayCommand]
    private void PromptDeleteModule(ModuleViewModel module)
    {
        if (module == null) return;

        _itemToDelete = module;
        _deleteAction = item => DeleteModule((ModuleViewModel)item);
        ConfirmDialogMessage =
            $"Êtes-vous sûr de vouloir supprimer le module '{module.ShortName}' et toutes ses branches ?";
        IsConfirmDialogOpen = true;
    }

    [RelayCommand]
    private async Task AddModule()
    {
        var nouveauModule = new Module { Nom = "Nouveau Module" };

        try
        {
            using (var repo = new DataRepository())
                await repo.AjouterModuleAsync(nouveauModule);

            Modules.Add(new ModuleViewModel
            {
                Id              = nouveauModule.Id,
                Nom             = nouveauModule.Nom,
                Branches        = new List<Branche>(),
                JournalDeTravail = new List<Entree>(),
                TypesDeTravail  = new List<TypeTravail>()
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de l'ajout du module : {ex.Message}");
        }
    }

    private async void DeleteModule(ModuleViewModel module)
    {
        if (module == null) return;

        try
        {
            using (var repo = new DataRepository())
                await repo.SupprimerModuleAsync(new Module { Id = module.Id });

            Modules.Remove(module);

            if (EditingModule?.Id == module.Id)
                CloseEditModuleModal();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la suppression du module : {ex.Message}");
        }
    }

    [RelayCommand]
    private void EditModule(ModuleViewModel module)
    {
        if (module == null) return;

        EditingModule    = module;
        ModalBranches    = new ObservableCollection<Branche>(module.Branches ?? new List<Branche>());
        IsEditModuleModalOpen = true;
    }

    [RelayCommand]
    private void CloseEditModuleModal()
    {
        IsEditModuleModalOpen = false;
        EditingModule         = null;
        NouvelleBrancheNom    = string.Empty;
        ModalBranches.Clear();
    }

    [RelayCommand]
    private async Task SaveModuleName()
    {
        if (EditingModule == null || string.IsNullOrWhiteSpace(EditingModule.Nom)) return;

        try
        {
            using (var repo = new DataRepository())
                await repo.ModifierModuleAsync(new Module { Id = EditingModule.Id, Nom = EditingModule.Nom.Trim() });

            var index = Modules.IndexOf(EditingModule);
            if (index >= 0) Modules[index] = EditingModule;

            OnPropertyChanged(nameof(EditingModule));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur : {ex.Message}");
        }
    }

    // -------------------------------------------------------------------------
    //  Commandes — Branches
    // -------------------------------------------------------------------------

    [RelayCommand]
    private void PromptDeleteBranche(object branche)
    {
        if (branche == null) return;

        _itemToDelete = branche;
        _deleteAction = item => DeleteBranche(item);

        string nomBranche = branche is Branche b ? b.Nom : "cette branche";
        ConfirmDialogMessage  = $"Êtes-vous sûr de vouloir supprimer {nomBranche} ?";
        IsConfirmDialogOpen   = true;
    }

    [RelayCommand]
    private async Task AddBrancheToEditingModule()
    {
        if (EditingModule == null || string.IsNullOrWhiteSpace(NouvelleBrancheNom)) return;

        var nouvelleBranche = new Branche
        {
            Nom      = NouvelleBrancheNom.Trim(),
            ModuleId = EditingModule.Id,
            Type     = TypeCours.TM
        };

        try
        {
            using (var repo = new DataRepository())
                await repo.AjouterBrancheAsync(nouvelleBranche);

            EditingModule.Branches ??= new List<Branche>();
            EditingModule.Branches.Add(nouvelleBranche);
            ModalBranches.Add(nouvelleBranche);
            NouvelleBrancheNom = string.Empty;

            var index = Modules.IndexOf(EditingModule);
            if (index >= 0) Modules[index] = EditingModule;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur : {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task RenameBranche(Branche branche)
    {
        if (branche == null || string.IsNullOrWhiteSpace(branche.Nom) || EditingModule == null) return;

        try
        {
            using (var repo = new DataRepository())
                await repo.ModifierBrancheAsync(new Branche
                {
                    Id       = branche.Id,
                    Nom      = branche.Nom.Trim(),
                    ModuleId = branche.ModuleId,
                    Type     = branche.Type
                });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            /* Ignoré — déjà traité par la BDD */
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur : {ex.Message}");
        }
    }

    private async void DeleteBranche(object brancheObj)
    {
        if (brancheObj is not Branche branche || EditingModule == null) return;

        try
        {
            using (var repo = new DataRepository())
                await repo.SupprimerBrancheAsync(new Branche { Id = branche.Id });

            EditingModule.Branches?.Remove(branche);

            var brancheToRemove = ModalBranches.FirstOrDefault(b => b.Id == branche.Id);
            if (brancheToRemove != null) ModalBranches.Remove(brancheToRemove);

            var index = Modules.IndexOf(EditingModule);
            if (index >= 0) Modules[index] = EditingModule;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            Console.WriteLine("Info: Suppression ignorée (déjà traitée).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur : {ex.Message}");
        }
    }
}