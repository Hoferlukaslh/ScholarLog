/*
    Fichier      :  NotesPage.axaml.cs
    Projet       :  ScholarLog

    Description  :
        Code-behind de la vue NotesPage.
        Gère les interactions spécifiques à l'interface utilisateur pour la gestion
        des notes, notamment la sécurité lors de la suppression (timer) et
        la validation des dates via le contrôle calendrier.

    Auteur       :  Lukas Hofer - TINF2
    Date         :  19.03.2026

    Remarques    :
        - Utilise un système de confirmation visuelle (IsDeletePending) de 3 secondes
          pour éviter les suppressions accidentelles de notes.
        - L'appuis de Ctrl, Alt ou Shit + la corbeille permet une suppression instantanée.
        - Supporte la suppression rapide via les touches modificatrices (Ctrl/Shift/Alt).
*/


using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ScholarLog.Data;
using ScholarLog.ViewModels;
using ScholarLog.Component;

namespace ScholarLog.Views.Pages;

public partial class NotesPage : UserControl
{
    public NotesPage()
    {
        InitializeComponent();
    }

    // gestion de la sécurité sur la date (logique de contrôle UI)
    private void CalendarDatePicker_OnSelectedDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is CalendarDatePicker picker && this.DataContext is NotesViewModel vm)
        {
            if (picker.SelectedDate == null)
            {
                picker.SelectedDate = DateTime.Now;
                return;
            }

            if (vm.EditingNote != null)
            {
                vm.EditingNote.Date = picker.SelectedDate.Value.Date;
            }
        }
    }

    // logique de l'interface (clic avec modificateur, timer de 3 secondes)
    private async void BoutonSupprimer_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (sender is Button btn && this.DataContext is NotesViewModel vm)
        {
            Note? noteASupprimer = null;

            if (btn.DataContext is NoteViewModel nd)
                noteASupprimer = nd;
            else if (btn.DataContext is Note n)
                noteASupprimer = n;

            if (noteASupprimer == null) return;

            bool isModifierPressed = e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control) ||
                                     e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Shift) ||
                                     e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Alt);

            if (isModifierPressed || noteASupprimer.IsDeletePending)
            {
                await vm.ExecuterSuppressionCommand.ExecuteAsync(noteASupprimer);
            }
            else
            {
                noteASupprimer.IsDeletePending = true;
                await Task.Delay(3000);

                if (noteASupprimer != null)
                    noteASupprimer.IsDeletePending = false;
            }
        }
    }

    // Nouvelle méthode pour ouvrir l'explorateur de fichiers
    private async void BoutonJoindrePdf_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        // Ouvre le FilePicker natif de l'OS
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Sélectionner un fichier PDF",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("Documents PDF") { Patterns = new[] { "*.pdf" } } }
        });

        if (files.Count > 0 && this.DataContext is NotesViewModel vm)
        {
            // On envoie le chemin du fichier au ViewModel pour qu'il le traite
            await vm.TraiterPdfAttacheAsync(files[0].Path.LocalPath, files[0].Name);
        }
    }
    
    // -------------------------------------------------------------------------
    // Ouverture du modal d'exportation des notes
    // -------------------------------------------------------------------------
    private void BoutonExporter_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (this.DataContext is NotesViewModel vm)
        {
            // Détermine le contexte d'export :
            // Si l'utilisateur est sur la vue Liste -> Exporte tout
            // Si l'utilisateur est sur la vue par Module -> Exporte le module sélectionné
            bool exportAll = vm.IsListView;

            // Transmet l'intention au ViewModel
            vm.SetExportContext(exportAll);

            // Construction du nom de fichier
            string trigramme = "ALL";
            if (!exportAll && vm.SelectedModule != null)
            {
                string nomModule = vm.SelectedModule.Nom ?? "MOD";
                trigramme = nomModule.Length >= 3 ? nomModule[..3].ToUpper() : nomModule.ToUpper();
            }
            
            ExportModal.SuggestedFileBaseName = $"MesNotes_{trigramme}";

            // Par défaut on affiche en Markdown à l'ouverture
            vm.ChangerFormatExportationCommand.Execute("MD");
            vm.IsExportModalOpen = true;
        }
    }

    /// <summary>
    /// Sécurisation de la saisie de note : empêche la valeur null et clamp entre 1 et 6
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void NoteNumericUpDown_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (sender is NumericUpDown nud)
        {
            if (nud.Value == null || nud.Value < 1.0m)
            {
                nud.Value = 1.0m;
            }
            else if (nud.Value > 6.0m)
            {
                nud.Value = 6.0m;
            }
        }
    }
}