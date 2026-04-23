/*
    Fichier      :  JournalPage.axaml.cs
    Projet       :  ScholarLog

    Description  :
        Code-behind de la vue JournalPage.
        Gère la logique purement visuelle et les interactions avec les services
        système (Sélecteur de fichiers) qui ne peuvent pas être faits directement
        dans le ViewModel.

    Auteur       :  Lukas Hofer - TINF2
    Date         :  19.03.2026

    Remarques    :
        - La gestion du presse-papier et de l'enregistrement sur disque a été
          déplacée dans le composant ModalExport (ModalExport.axaml.cs).
        - Gère le compte à rebours visuel de 3s pour la suppression sécurisée.
        - BoutonExporter_Tapped n'a plus besoin de retrouver le radio-bouton
          par nom (FindControl) : le composant ModalExport le réinitialise
          automatiquement à l'ouverture via OnPropertyChanged.
*/

using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using ScholarLog.Data;
using ScholarLog.ViewModels;

namespace ScholarLog.Views.Pages;

public partial class JournalPage : UserControl
{
    public JournalPage()
    {
        InitializeComponent();
    }

    // -------------------------------------------------------------------------
    // Suppression sécurisée (compte à rebours visuel de 3 secondes)
    // -------------------------------------------------------------------------

    private async void BoutonSupprimer_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Entree entree && this.DataContext is JournalViewModel vm)
        {
            bool isModifierPressed = e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control) ||
                                     e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Shift) ||
                                     e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Alt);

            if (isModifierPressed || entree.IsDeletePending)
            {
                await vm.ExecuterSuppressionCommand.ExecuteAsync(entree);
            }
            else
            {
                entree.IsDeletePending = true;
                await Task.Delay(3000);
                if (entree != null) entree.IsDeletePending = false;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Validation de la date via le contrôle calendrier
    // -------------------------------------------------------------------------

    private void CalendarDatePicker_OnSelectedDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is CalendarDatePicker picker && this.DataContext is JournalViewModel vm)
        {
            if (picker.SelectedDate == null)
            {
                picker.SelectedDate = DateTime.Now;
                return;
            }

            if (vm.EditingEntree != null)
            {
                vm.EditingEntree.Date = picker.SelectedDate.Value.Date;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Ouverture du modal d'exportation
    // -------------------------------------------------------------------------

    private void BoutonExporter_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (this.DataContext is JournalViewModel vm)
        {
            bool exportAll = e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control) ||
                             e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Shift) ||
                             e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Alt);

            vm.SetExportAllModules(exportAll);

            // Calcul du nom de fichier suggéré (logique trigramme conservée ici
            // pour éviter d'ajouter une propriété au ViewModel).
            string trigramme = "ALL";
            if (!exportAll)
            {
                string nomModule = vm.SelectedModule?.ShortName ?? "MOD";
                trigramme = nomModule.Length >= 3 ? nomModule[..3].ToUpper() : nomModule.ToUpper();
            }
            ExportModal.SuggestedFileBaseName = $"JournalDeTravail_{trigramme}";

            // Le composant ModalExport réinitialise le radio-bouton à MD automatiquement.
            vm.ChangerFormatExportationCommand.Execute("MD");
            vm.IsExportModalOpen = true;
        }
    }
}
