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
using ScholarLog.Data;
using ScholarLog.ViewModels;

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
                noteASupprimer = nd.NoteData;
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
}