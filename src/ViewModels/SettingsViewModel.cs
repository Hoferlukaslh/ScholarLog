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
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using Avalonia;
using Avalonia.Styling;
using ScholarLog.Data;

namespace ScholarLog.ViewModels;


public partial class SettingsViewModel : ViewModelBase
{
    
    
    [ObservableProperty] private string _pathToBDD = "/home/lukas/Documents/CloudSync/App.db";
    [ObservableProperty] private bool _isDarkModeEnabled;
    [ObservableProperty] private bool _showPathError = false; // déclencheur pour dire à la vue d'afficher le message d'erreur
    
    
    [ObservableProperty] private bool _isEditModuleModalOpen = false;
    [ObservableProperty] private ModuleViewModel? _editingModule;
    [ObservableProperty] private string _nouvelleBrancheNom = string.Empty;
    
    [RelayCommand]
    private void CloseEditModuleModal()
    {
        IsEditModuleModalOpen = false;
        EditingModule = null;
        NouvelleBrancheNom = string.Empty;
    }

    // Remplacez la méthode EditModule existante par celle-ci
    [RelayCommand]
    private void EditModule(ModuleViewModel module)
    {
        if (module == null) return;
        
        EditingModule = module;
        IsEditModuleModalOpen = true;
    }

    [RelayCommand]
    private void SaveModuleName()
    {
        if (EditingModule == null || string.IsNullOrWhiteSpace(EditingModule.Nom)) return;
        
        // Logique de sauvegarde en base de données pour le nom du module ici
        Console.WriteLine($"Action : Sauvegarder le nom du module {EditingModule.Nom}");
    }

    [RelayCommand]
    private void AddBrancheToEditingModule()
    {
        if (EditingModule == null || string.IsNullOrWhiteSpace(NouvelleBrancheNom)) return;

        // Logique pour créer et ajouter la nouvelle branche en base de données ici
        Console.WriteLine($"Action : Ajouter la branche {NouvelleBrancheNom} au module {EditingModule.Nom}");
        
        // Simulation d'ajout visuel (à adapter avec votre vrai modèle)
        // EditingModule.Branches.Add(new BrancheViewModel { Nom = NouvelleBrancheNom });

        NouvelleBrancheNom = string.Empty;
    }

    [RelayCommand]
    private void RenameBranche(object branche)
    {
        if (branche == null) return;

        // Logique de sauvegarde en base de données pour le nom de la branche ici
        // Cast l'objet en BrancheViewModel (ou le type correspondant) pour accéder à sa propriété Nom
        Console.WriteLine("Action : Sauvegarder le nouveau nom de la branche");
    }
    
    [ObservableProperty]
    private ObservableRangeCollection<ModuleViewModel> _modules = new();

    public SettingsViewModel()
    {
        // initialiser avec le thème actuel au démarrage
        if (Application.Current != null)
        {
            IsDarkModeEnabled = Application.Current.ActualThemeVariant == ThemeVariant.Dark;
            Modules = AppDataService.Instance.Modules;
        }
    }
    
    //  Propriétés pour le Modal de Confirmation 
    [ObservableProperty] private bool _isConfirmDialogOpen = false;
    [ObservableProperty] private string _confirmDialogMessage = string.Empty;
    
    // Variables temporaires pour stocker l'action et l'élément à supprimer
    private object? _itemToDelete;
    private Action<object>? _deleteAction;

    [RelayCommand]
    private void PromptDeleteModule(ModuleViewModel module)
    {
        if (module == null) return;
        _itemToDelete = module;
        _deleteAction = (item) => DeleteModule((ModuleViewModel)item);
        
        ConfirmDialogMessage = $"Êtes-vous sûr de vouloir supprimer le module '{module.ShortName}' et toutes ses branches ?";
        IsConfirmDialogOpen = true;
    }

    [RelayCommand]
    private void PromptDeleteBranche(object branche)
    {
        if (branche == null) return;
        _itemToDelete = branche;
        _deleteAction = (item) => DeleteBranche(item);
        
        ConfirmDialogMessage = $"Êtes-vous sûr de vouloir supprimer cette branche ?";
        IsConfirmDialogOpen = true;
    }

    [RelayCommand]
    private void ConfirmDelete()
    {
        if (_deleteAction != null && _itemToDelete != null)
        {
            _deleteAction(_itemToDelete);
        }
        CancelDelete(); // Réinitialise et ferme le modal
    }

    [RelayCommand]
    private void CancelDelete()
    {
        IsConfirmDialogOpen = false;
        _itemToDelete = null;
        _deleteAction = null;
    }

    // Cette méthode magique est appelée automatiquement par le Toolkit
    // quand IsDarkModeEnabled change
    partial void OnIsDarkModeEnabledChanged(bool value)
    {
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeVariant = value ? ThemeVariant.Dark : ThemeVariant.Light;
        }
    }
    
    [RelayCommand]
    private void AddModule()
    {
        Console.WriteLine("Action : Ajouter un nouveau module");
    }


    
    [RelayCommand]
    private void DeleteModule(ModuleViewModel module)
    {
        if (module == null) return;
        
        Console.WriteLine($"Action : Supprimer le module {module.ShortName}");
    }

    // Actions CRUD pour les Branches 

    [RelayCommand]
    private void AddBranche(ModuleViewModel parentModule)
    {
        if (parentModule == null) return;

        Console.WriteLine($"Action : Ajouter une branche au module {parentModule.ShortName}");
    }
    
    [RelayCommand]
    private void EditBranche(object branche) 
    {
        if (branche == null) return;

        Console.WriteLine("Action : Editer la branche");
    }

    [RelayCommand]
    private void DeleteBranche(object branche)
    {
        if (branche == null) return;

        Console.WriteLine("Action : Supprimer la branche");
    }

    [RelayCommand]
    private void SavePath()
    {
        string cleanedPath = PathToBDD?.Trim() ?? string.Empty;

        if (File.Exists(cleanedPath) || string.IsNullOrEmpty(cleanedPath))
        {
            PathToBDD = cleanedPath;
            ShowPathError = false;
            Console.WriteLine($"Chemin sauvegarde : {cleanedPath}");
        }
        else
        {
            Console.WriteLine($"Chemin invalide : {cleanedPath}");
            // On lève le drapeau, la Vue s'occupera d'afficher le Flyout
            ShowPathError = true; 
        }
    }
}
