/*
    Fichier      :  NotesPage.axaml.cs
    Projet       :  ScholarLog

    Description  :
         Code-behind de la vue NotesPage permettant la gestion des notes (CRUD)
         et gérant les deux modes d'affichage (Liste / Par Module).

    Auteur       :  Lukas Hofer - TINF2
    Date         :  17.03.2026
*/

using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using ScholarLog.Data;

namespace ScholarLog.Pages;

// Classe locale pour aplatir les données de la vue Liste
public class NoteDisplay
{
    public Note NoteData { get; set; }
    public string BrancheNom { get; set; }
    public string ModuleNom { get; set; }
}

public partial class NotesPage : UserControl
{
    
    public static readonly StyledProperty<bool> IsListViewProperty =
        AvaloniaProperty.Register<NotesPage, bool>(nameof(IsListView), true); // Liste par défaut
    
    public bool IsListView
    {
        get => GetValue(IsListViewProperty);
        set => SetValue(IsListViewProperty, value);
    }

    // Collections pour l'affichage
    public ObservableRangeCollection<NoteDisplay> AllNotes { get; set; } = new ObservableRangeCollection<NoteDisplay>();
    public ObservableRangeCollection<Branche> SelectedModuleBranchesWithNotes { get; set; } = new ObservableRangeCollection<Branche>();
    public ObservableRangeCollection<ModuleViewModel> Modules { get; set; } = AppDataService.Instance.Modules;

    // Filtres
    public static readonly StyledProperty<ModuleViewModel?> SelectedModuleProperty = AvaloniaProperty.Register<NotesPage, ModuleViewModel?>(nameof(SelectedModule));
    public ModuleViewModel? SelectedModule { get => GetValue(SelectedModuleProperty); set => SetValue(SelectedModuleProperty, value); }

    // Modal
    public static readonly StyledProperty<bool> IsModalOpenProperty = AvaloniaProperty.Register<NotesPage, bool>(nameof(IsModalOpen), false);
    public bool IsModalOpen { get => GetValue(IsModalOpenProperty); set => SetValue(IsModalOpenProperty, value); }

    public static readonly StyledProperty<string> ModalTitleProperty = AvaloniaProperty.Register<NotesPage, string>(nameof(ModalTitle), "Nouvelle note");
    public string ModalTitle { get => GetValue(ModalTitleProperty); set => SetValue(ModalTitleProperty, value); }

    public static readonly StyledProperty<Note> EditingNoteProperty = AvaloniaProperty.Register<NotesPage, Note>(nameof(EditingNote));
    public Note EditingNote { get => GetValue(EditingNoteProperty); set => SetValue(EditingNoteProperty, value); }

    public static readonly StyledProperty<ModuleViewModel?> ModalSelectedModuleProperty = AvaloniaProperty.Register<NotesPage, ModuleViewModel?>(nameof(ModalSelectedModule));
    public ModuleViewModel? ModalSelectedModule { get => GetValue(ModalSelectedModuleProperty); set => SetValue(ModalSelectedModuleProperty, value); }

    public ObservableRangeCollection<Branche> ModalBranches { get; set; } = new ObservableRangeCollection<Branche>();

    public static readonly StyledProperty<Branche?> ModalSelectedBrancheProperty = AvaloniaProperty.Register<NotesPage, Branche?>(nameof(ModalSelectedBranche));
    public Branche? ModalSelectedBranche { get => GetValue(ModalSelectedBrancheProperty); set => SetValue(ModalSelectedBrancheProperty, value); }

    private bool _isEditingExisting;

    public NotesPage()
    {
        InitializeComponent();
        DataContext = this;

        if (Modules != null && Modules.Count > 0)
        {
            SelectedModule = Modules[0];
        }

        RefreshAllNotes();
    }
    
    
    // gère les mises à jour en cascade lorsque l'utilisateur sélectionne un module
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedModuleProperty)
        {
            RefreshSelectedModuleBranches();
        }
        else if (change.Property == ModalSelectedModuleProperty)
        {
            var newVal = change.GetNewValue<ModuleViewModel?>();
    
            if (newVal?.Branches != null)
            {
                // injection massive et instantanée
                ModalBranches.ReplaceAll(newVal.Branches);
            }
            else
            {
                ModalBranches.Clear();
            }

            // s'il s'agit d'un ajout, on présélectionne la première branche
            if (!_isEditingExisting && ModalBranches.Any())
            {
                ModalSelectedBranche = ModalBranches.First();
            }
        }
    }



    private void RefreshAllNotes()
    {
        AllNotes.Clear();
        var listeTemporaire = new System.Collections.Generic.List<NoteDisplay>();

        foreach (var module in Modules)
        {
            if (module.Branches == null) continue;
            foreach (var branche in module.Branches)
            {
                if (branche.Notes == null) continue;
                foreach (var note in branche.Notes)
                {
                    listeTemporaire.Add(new NoteDisplay
                    {
                        NoteData = note,
                        BrancheNom = branche.Nom,
                        ModuleNom = module.Nom 
                    });
                }
            }
        }

        AllNotes.ReplaceAll(listeTemporaire.OrderByDescending(n => n.NoteData.Date));
    }

    private void RefreshSelectedModuleBranches()
    {
        if (SelectedModule?.Branches == null) 
        {
            SelectedModuleBranchesWithNotes.Clear();
            return;
        }

        // filtre et on injecte tout d'un coup
        var branchesFiltrees = SelectedModule.Branches.Where(b => b.Notes != null && b.Notes.Count > 0);
        SelectedModuleBranchesWithNotes.ReplaceAll(branchesFiltrees);
    }

    // modal

    public void OuvrirModalAjout()
    {
        ModalTitle = "Nouvelle note";
        _isEditingExisting = false;
        
        EditingNote = new Note 
        { 
            Date = DateTime.Today,
            Valeur = 4.0, 
            titre = string.Empty
        };

        // rrésélection par défaut (le module actuellement affiché ou le premier)
        ModalSelectedModule = SelectedModule ?? Modules.FirstOrDefault();
    
        IsModalOpen = true;
    }

    public void OuvrirModalModification(Note noteAModifier)
    {
        if (noteAModifier == null) return;

        ModalTitle = "Modifier la note";
        _isEditingExisting = true;
    
        EditingNote = new Note
        {
            Id = noteAModifier.Id,
            Date = noteAModifier.Date,
            Valeur = noteAModifier.Valeur,
            titre = noteAModifier.titre,
            BrancheId = noteAModifier.BrancheId
        };
        
        // tTrouve le bon module lié à la note cliquée
        var moduleParent = Modules.FirstOrDefault(m => m.Branches.Any(b => b.Id == noteAModifier.BrancheId));
        if (moduleParent != null)
        {
            ModalSelectedModule = moduleParent; // déclenche le PropertyChanged et remplit ModalBranches
            ModalSelectedBranche = ModalBranches.FirstOrDefault(b => b.Id == noteAModifier.BrancheId);
        }

        IsModalOpen = true;
    }

    public void FermerModal()
    {
        IsModalOpen = false;
    }

    // gestion des données
    
    public async Task SauvegarderNote()
    {
        try 
        {
            if (ModalSelectedBranche == null || string.IsNullOrWhiteSpace(EditingNote?.titre))
                return; 

            EditingNote.BrancheId = ModalSelectedBranche.Id;

            // Opérations en Base de données
            using (var repo = new DataRepository())
            {
                if (_isEditingExisting) await repo.ModifierNoteAsync(EditingNote);
                else await repo.AjouterNoteAsync(EditingNote);
            }

            // Mise à jour de la mémoire locale 
            var moduleDest = Modules.FirstOrDefault(m => m.Id == ModalSelectedModule?.Id);
            if (moduleDest != null)
            {
                var brancheDest = moduleDest.Branches.FirstOrDefault(b => b.Id == ModalSelectedBranche.Id);
                if (brancheDest != null)
                {
                    if (_isEditingExisting)
                    {
                        // Au cas où la note aurait changé de branche, on nettoie d'abord partout
                        foreach(var m in Modules)
                        foreach(var b in m.Branches)
                        {
                            var oldNote = b.Notes.FirstOrDefault(n => n.Id == EditingNote.Id);
                            if (oldNote != null) b.Notes.Remove(oldNote);
                        }
                    }
                    
                    brancheDest.Notes.Add(EditingNote);
                    brancheDest.Notes = brancheDest.Notes.OrderByDescending(n => n.Date).ToList();
                }
            }

            // actualise les deux vues
            RefreshAllNotes();
            RefreshSelectedModuleBranches();
            
            FermerModal();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la sauvegarde : {ex.Message}");
        }
    }

    public async void SupprimerNote(Note noteASupprimer)
    {
        if (noteASupprimer == null) return;

        try
        {
            using (var repo = new DataRepository())
            {
                await repo.SupprimerNoteAsync(noteASupprimer);
            }

            // supprime de la mémoire
            foreach (var m in Modules)
            {
                if (m.Branches == null) continue;
                var b = m.Branches.FirstOrDefault(br => br.Id == noteASupprimer.BrancheId);
                if (b != null)
                {
                    var n = b.Notes.FirstOrDefault(nt => nt.Id == noteASupprimer.Id);
                    if (n != null) b.Notes.Remove(n);
                    break;
                }
            }

            RefreshAllNotes();
            RefreshSelectedModuleBranches();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la suppression : {ex.Message}");
        }
    }
    
    
    // gestion suppression
    private async void BoutonSupprimer_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (sender is Button btn)
        {
            Note? noteASupprimer = null;

            // On récupère la note selon la vue active (Liste globale ou Par Module)
            if (btn.DataContext is NoteDisplay nd)
                noteASupprimer = nd.NoteData;
            else if (btn.DataContext is Note n)
                noteASupprimer = n;

            if (noteASupprimer == null) return;

            // On vérifie si un modificateur (Ctrl, Shift, Alt) est pressé
            bool isModifierPressed = e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control) ||
                                     e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Shift) ||
                                     e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Alt);

            // Si c'est le 2ème clic (déjà rouge) ou si un modificateur est pressé -> on supprime direct
            if (isModifierPressed || noteASupprimer.IsDeletePending)
            {
                await ExecuterSuppression(noteASupprimer);
            }
            else
            {
                // 1er clic -> Mise en attente (devient rouge)
                noteASupprimer.IsDeletePending = true;
            
                // Attend 3 secondes
                await Task.Delay(3000);
            
                // Si elle n'a pas été supprimée entre-temps, on annule l'état
                if (noteASupprimer != null)
                    noteASupprimer.IsDeletePending = false;
            }
        }
    }

    private async Task ExecuterSuppression(Note noteASupprimer)
    {
        try
        {
            using (var repo = new DataRepository())
            {
                await repo.SupprimerNoteAsync(noteASupprimer);
            }

            // supprime de la mémoire
            foreach (var m in Modules)
            {
                if (m.Branches == null) continue;
                var b = m.Branches.FirstOrDefault(br => br.Id == noteASupprimer.BrancheId);
                if (b != null)
                {
                    var n = b.Notes.FirstOrDefault(nt => nt.Id == noteASupprimer.Id);
                    if (n != null) b.Notes.Remove(n);
                    break;
                }
            }

            // rafraîchit les deux vues
            RefreshAllNotes();
            RefreshSelectedModuleBranches();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la suppression : {ex.Message}");
        }
    }
    
    
    // sécurité sur la date
    private void CalendarDatePicker_OnSelectedDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is CalendarDatePicker picker)
        {
            // si l'utilisateur a effacé la date (null), date = aujourd'hui par sécurité
            if (picker.SelectedDate == null)
            {
                picker.SelectedDate = DateTime.Now;
                return;
            }

            // on s'assure que notre objet EditingNote est bien mis à jour
            if (EditingNote != null)
            {
                // Conversion forcée de DateTimeOffset? vers DateTime
                EditingNote.Date = picker.SelectedDate.Value.Date;
            }
        }
    }
}