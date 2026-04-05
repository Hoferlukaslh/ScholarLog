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
    
    [ObservableProperty] private ObservableCollection<Branche> _modalBranches = new();
    

    [RelayCommand]
    private void PromptDeleteBranche(object branche)
    {
        if (branche == null) return;
        
        _itemToDelete = branche;
        _deleteAction = (item) => DeleteBranche(item);
        
        string nomBranche = branche is Branche b ? b.Nom : "cette branche";
        
        ConfirmDialogMessage = $"Êtes-vous sûr de vouloir supprimer {nomBranche} ?";
        IsConfirmDialogOpen = true;
    }
    
    [RelayCommand]
    private void PromptDeleteModule(ModuleViewModel module)
    {
        if (module == null) return;
        
        _itemToDelete = module;
        _deleteAction = (item) => DeleteModule((ModuleViewModel)item);
        
        ConfirmDialogMessage = $"Êtes-vous sûr de vouloir supprimer le module '{module.ShortName}' et toutes ses branches ?";
        IsConfirmDialogOpen = true;
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
    
    
    //  Commandes pour les Modules 

    [RelayCommand]
    private async Task AddModule()
    {
        var nouveauModule = new Module 
        { 
            Nom = "Nouveau Module" 
        };

        try
        {
            using (var repo = new DataRepository())
            {
                await repo.AjouterModuleAsync(nouveauModule);
            }

            // Création du ViewModel avec le nouvel ID généré par la BDD
            var moduleVM = new ModuleViewModel
            {
                Id = nouveauModule.Id,
                Nom = nouveauModule.Nom,
                Branches = new List<Branche>(),
                JournalDeTravail = new List<Entree>(),
                TypesDeTravail = new List<TypeTravail>()
            };

            Modules.Add(moduleVM);
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
            {
                // Un stub suffit pour la suppression via EF Core
                var moduleStub = new Module { Id = module.Id };
                await repo.SupprimerModuleAsync(moduleStub);
            }

            // Mise à jour de l'interface
            Modules.Remove(module);
            
            // Si le module était en cours d'édition, on ferme le modal
            if (EditingModule?.Id == module.Id)
            {
                CloseEditModuleModal();
            }
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
        
        EditingModule = module;
        // On remplit la liste dynamique du modal au moment de l'ouverture
        ModalBranches = new ObservableCollection<Branche>(module.Branches ?? new List<Branche>());
        IsEditModuleModalOpen = true;
    }

    [RelayCommand]
    private void CloseEditModuleModal()
    {
        IsEditModuleModalOpen = false;
        EditingModule = null;
        NouvelleBrancheNom = string.Empty;
        ModalBranches.Clear(); // On vide pour la prochaine fois
    }

    [RelayCommand]
    private async Task SaveModuleName()
    {
        if (EditingModule == null || string.IsNullOrWhiteSpace(EditingModule.Nom)) return;

        try
        {
            using (var repo = new DataRepository())
            {
                var modToUpdate = new Module { Id = EditingModule.Id, Nom = EditingModule.Nom.Trim() };
                await repo.ModifierModuleAsync(modToUpdate);
            }
            
            // Mise à jour de la tuile en arrière-plan
            var index = Modules.IndexOf(EditingModule);
            if (index >= 0) Modules[index] = EditingModule;
            
            OnPropertyChanged(nameof(EditingModule)); // Met à jour le titre du header du modal
        }
        catch (Exception ex) { Console.WriteLine($"Erreur : {ex.Message}"); }
    }

    [RelayCommand]
    private async Task AddBrancheToEditingModule() // voir pour attribuer la branche du projet de module
    {
        if (EditingModule == null || string.IsNullOrWhiteSpace(NouvelleBrancheNom)) return;

        var nouvelleBranche = new Branche 
        { 
            Nom = NouvelleBrancheNom.Trim(), 
            ModuleId = EditingModule.Id,
            Type = TypeCours.TM 
        };

        try
        {
            using (var repo = new DataRepository())
            {
                await repo.AjouterBrancheAsync(nouvelleBranche);
            }

            // Sauvegarde en mémoire
            EditingModule.Branches ??= new List<Branche>();
            EditingModule.Branches.Add(nouvelleBranche);
            
            // Mise à jour de l'UI du modal SANS artefacts visuels
            ModalBranches.Add(nouvelleBranche);

            NouvelleBrancheNom = string.Empty;

            //  Mise à jour du compteur sur la tuile derrière
            var index = Modules.IndexOf(EditingModule);
            if (index >= 0) Modules[index] = EditingModule;
        }
        catch (Exception ex) { Console.WriteLine($"Erreur : {ex.Message}"); }
    }

    [RelayCommand]
    private async Task RenameBranche(Branche branche)
    {
        if (branche == null || string.IsNullOrWhiteSpace(branche.Nom) || EditingModule == null) return;

        try
        {
            using (var repo = new DataRepository())
            {
                var brancheToUpdate = new Branche { Id = branche.Id, Nom = branche.Nom.Trim(), ModuleId = branche.ModuleId, Type = branche.Type };
                await repo.ModifierBrancheAsync(brancheToUpdate);
            }
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException) { /* Ignoré */ }
        catch (Exception ex) { Console.WriteLine($"Erreur : {ex.Message}"); }
    }

    private async void DeleteBranche(object brancheObj)
    {
        if (brancheObj is not Branche branche || EditingModule == null) return;

        try
        {
            using (var repo = new DataRepository())
            {
                var brancheStub = new Branche { Id = branche.Id };
                await repo.SupprimerBrancheAsync(brancheStub);
            }
            
            // Suppression de la mémoire
            EditingModule.Branches?.Remove(branche);
            
            // Mise à jour de l'UI du modal SANS artefacts
            var brancheToRemove = ModalBranches.FirstOrDefault(b => b.Id == branche.Id);
            if (brancheToRemove != null) ModalBranches.Remove(brancheToRemove);

            // Mise à jour du compteur sur la tuile derrière
            var index = Modules.IndexOf(EditingModule);
            if (index >= 0) Modules[index] = EditingModule;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            // Sécurité anti-crash si la branche n'est déjà plus dans la BDD
            Console.WriteLine("Info: Suppression ignorée (déjà traitée).");
        }
        catch (Exception ex) { Console.WriteLine($"Erreur : {ex.Message}"); }
    }
}
