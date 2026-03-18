using System;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ScholarLog.Data;
using ScholarLog.ViewModels;

namespace ScholarLog.Pages;

public partial class JournalPage : UserControl
{
    public JournalPage()
    {
        InitializeComponent();
    }

    // --- GESTION VISUELLE DE LA SUPPRESSION (3s) ---
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

    // --- LOGIQUE UI : EXPORTATION ET SYSTEME DE FICHIERS ---
    private void BoutonExporter_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (this.DataContext is JournalViewModel vm)
        {
            bool exportAll = e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control) ||
                             e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Shift) ||
                             e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Alt);

            vm.SetExportAllModules(exportAll);

            // Coche le bouton radio dans l'UI (Code-behind)
            this.FindControl<RadioButton>("RbExportMd").IsChecked = true;
            
            vm.ChangerFormatExportationCommand.Execute("MD");
            vm.IsExportModalOpen = true;
        }
    }

    public async void CopierPressePapier()
    {
        if (this.DataContext is JournalViewModel vm && !string.IsNullOrEmpty(vm.ExportPreviewText))
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard != null) 
                await topLevel.Clipboard.SetTextAsync(vm.ExportPreviewText);
        }
    }

    public async void SauvegarderFichierExportation()
    {
        if (this.DataContext is JournalViewModel vm && !string.IsNullOrEmpty(vm.ExportPreviewText))
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                string extension = vm.GetCurrentExportFormat().ToLower();
                string trigramme = "ALL";
                
                if (!vm.GetExportAllModules())
                {
                    string nomModule = vm.SelectedModule?.ShortName ?? "MOD";
                    trigramme = nomModule.Length >= 3 ? nomModule.Substring(0, 3).ToUpper() : nomModule.ToUpper();
                }

                var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Exporter le journal de travail",
                    SuggestedFileName = $"JournalDeTravail_{trigramme}.{extension}",
                    DefaultExtension = extension
                });

                if (file != null)
                {
                    await using var stream = await file.OpenWriteAsync();
                    using var writer = new System.IO.StreamWriter(stream, Encoding.UTF8);
                    await writer.WriteAsync(vm.ExportPreviewText);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur sauvegarde: {ex.Message}");
            }
        }
    }
    
}