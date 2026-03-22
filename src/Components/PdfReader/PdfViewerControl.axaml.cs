using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


namespace ScholarLog.Components.PdfReader;

public partial class PdfViewerControl : UserControl
{
    // Stockage ultra-léger : on ne garde que les données brutes compressées en WebP (quelques Ko par page)
    private readonly List<byte[]> _pagesCompresses = new(); 
    
    // Seule image lourde (décompressée) active en RAM à un instant T
    private Avalonia.Media.Imaging.Bitmap? _imageCourante; 
    
    // Déclaration de la propriété de liaison 
    public static readonly StyledProperty<byte[]?> CbzDataProperty =
        AvaloniaProperty.Register<PdfViewerControl, byte[]?>(nameof(CbzData));

    public byte[]? CbzData
    {
        get => GetValue(CbzDataProperty);
        set => SetValue(CbzDataProperty, value);
    }
    
    private int _pageCouranteIndex = -1;
    private CancellationTokenSource? _cancellationTokenSource;

    public PdfViewerControl()
    {
        InitializeComponent();
        MettreAJourInterface();
    }
    protected override async void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // On vérifie si c'est bien notre propriété CbzData qui a changé
        if (change.Property == CbzDataProperty)
        {
            // On récupère la nouvelle valeur
            var data = change.GetNewValue<byte[]?>();
            
            if (data != null && data.Length > 0)
            {
                await LoadFromMemoryAsync(data);
            }
            else
            {
                CloseDocument(); // On ferme si le binding devient nul
            }
        }
    }
    
    public async Task LoadFromMemoryAsync(byte[] cbzData)
    {
        CloseDocument();
        
        if (cbzData == null || cbzData.Length == 0) return;

        _cancellationTokenSource = new CancellationTokenSource();
        IndicateurStatut.Text = "Lecture du document de la BDD...";

        try
        {
            // 1. Extraction asynchrone depuis le BLOB via ton gestionnaire existing
            var fluxImages = ArchiveManager.ExtractImagesFromMemoryAsync(cbzData, _cancellationTokenSource.Token);
            
            // 2. Redimensionnement (ex: 2Mp) pour la légèreté
            var fluxResized = ImageProcessor.ResizeImagesAsync(fluxImages, 2_000_000, _cancellationTokenSource.Token);

            await foreach (var skBitmap in fluxResized)
            {
                // 3. Compression WebP RAM agressive
                byte[] compressedPage = await Task.Run(() => 
                {
                    using var image = SKImage.FromBitmap(skBitmap);
                    using var data = image.Encode(SKEncodedImageFormat.Webp, 40); 
                    return data.ToArray();
                }, _cancellationTokenSource.Token);
                
                _pagesCompresses.Add(compressedPage);
                skBitmap.Dispose(); // Vital

                if (_pagesCompresses.Count == 1) await AfficherPageAsync(0);
                MettreAJourInterface();
            }
            IndicateurStatut.Text = "Lecture terminée.";
        }
        catch (OperationCanceledException) { IndicateurStatut.Text = "Lecture annulée."; }
        catch (Exception ex) { IndicateurStatut.Text = $"Erreur : {ex.Message}"; }
    }

    /// <summary>
    /// Charge et compresse le PDF en mémoire de manière totalement asynchrone
    /// </summary>
    public async Task LoadPdfAsync(string pdfPath)
    {
        CloseDocument();
        
        if (!File.Exists(pdfPath))
        {
            IndicateurStatut.Text = "Fichier introuvable.";
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        IndicateurStatut.Text = "Compression In-Memory en cours...";

        try
        {
            // 1. Extraction asynchrone
            var fluxImages = PdfManager.ExtractImagesAsync(pdfPath, _cancellationTokenSource.Token);
            
            // 2. Redimensionnement (ex: 2Mp) pour éviter de saturer la RAM, géré sur thread secondaire
            var fluxResized = ImageProcessor.ResizeImagesAsync(fluxImages, 2_000_000, _cancellationTokenSource.Token);

            await foreach (var skBitmap in fluxResized)
            {
                // 3. L'encodage WebP est lourd (CPU-bound) : Task.Run obligatoire pour ne pas geler l'UI
                byte[] compressedData = await Task.Run(() => 
                {
                    using var image = SKImage.FromBitmap(skBitmap);
                    // Compression agressive (qualité 40) type "CBZ/BDD"
                    using var data = image.Encode(SKEncodedImageFormat.Webp, 40); 
                    return data.ToArray();
                }, _cancellationTokenSource.Token);
                
                _pagesCompresses.Add(compressedData);
                
                // Libération immédiate et vitale du pointeur natif Skia
                skBitmap.Dispose(); 

                // Affichage fluide : on charge la première page dès qu'elle est encodée
                if (_pagesCompresses.Count == 1)
                {
                    await AfficherPageAsync(0);
                }
                
                MettreAJourInterface();
            }
            IndicateurStatut.Text = "Lecture terminée et optimisée.";
        }
        catch (OperationCanceledException)
        {
            IndicateurStatut.Text = "Chargement annulé.";
        }
        catch (Exception ex)
        {
            IndicateurStatut.Text = $"Erreur : {ex.Message}";
        }
    }

    /// <summary>
    /// Affiche une page spécifique en la décompressant à la volée
    /// </summary>
    private async Task AfficherPageAsync(int index)
    {
        if (index < 0 || index >= _pagesCompresses.Count) return;

        // Le décodage de l'image Avalonia se fait en arrière-plan pour garder l'UI fluide
        var nouvelleImage = await Task.Run(() => 
        {
            using var ms = new MemoryStream(_pagesCompresses[index]);
            return new Avalonia.Media.Imaging.Bitmap(ms);
        });

        // 4. Gestion stricte de la RAM : on détruit l'ancienne page affichée
        _imageCourante?.Dispose();
        _imageCourante = nouvelleImage;

        // Mise à jour de la vue sur le Thread principal
        Dispatcher.UIThread.Post(() =>
        {
            ImagePdf.Source = _imageCourante;
            _pageCouranteIndex = index;
            MettreAJourInterface();
        });
    }

    /// <summary>
    /// Nettoie complètement la mémoire du composant
    /// </summary>
    public void CloseDocument()
    {
        _cancellationTokenSource?.Cancel();
        
        _imageCourante?.Dispose();
        _imageCourante = null;
        
        _pagesCompresses.Clear();
        
        Dispatcher.UIThread.Post(() =>
        {
            ImagePdf.Source = null;
            _pageCouranteIndex = -1;
            MettreAJourInterface();
        });
    }

    // --- Les événements de navigation deviennent asynchrones ---
    private async void BoutonPrecedent_Click(object? sender, RoutedEventArgs e)
    {
        if (_pageCouranteIndex > 0)
        {
            await AfficherPageAsync(_pageCouranteIndex - 1);
        }
    }

    private async void BoutonSuivant_Click(object? sender, RoutedEventArgs e)
    {
        if (_pageCouranteIndex < _pagesCompresses.Count - 1 && _pageCouranteIndex >= 0)
        {
            await AfficherPageAsync(_pageCouranteIndex + 1);
        }
    }

    private void MettreAJourInterface()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_pagesCompresses.Count == 0)
            {
                IndicateurPage.Text = "0 / 0";
            }
            else
            {
                IndicateurPage.Text = $"{_pageCouranteIndex + 1} / {_pagesCompresses.Count}";
            }
        });
    }
}