using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Threading;
using Avalonia;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Interactivity;


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

    private int _currentPageIndex = 0;

    // propriété zoom
    private double _currentZoomScale = 1.0;
    private Size _currentOriginalImageSize; // Stocke la taille native de la page chargée

    // Paramètres de zoom (Multiplicateur 1.1 = +/- 10%)
    private const double ZoomFactor = 1.1;
    private const double MinZoomScale = 0.1; // 10% minimum
    private const double MaxZoomScale = 10.0; // 1000% maximum

    private bool _isDragging = false;
    private Point _lastMousePosition;

    public byte[]? CbzData
    {
        get => GetValue(CbzDataProperty);
        set => SetValue(CbzDataProperty, value);
    }

    // Déclaration de la propriété pour le titre du document
    public static readonly StyledProperty<string> DocumentTitleProperty =
        AvaloniaProperty.Register<PdfViewerControl, string>(nameof(DocumentTitle), "Evaluation");

    public string DocumentTitle
    {
        get => GetValue(DocumentTitleProperty);
        set => SetValue(DocumentTitleProperty, value);
    }

    private int _pageCouranteIndex = -1;
    private CancellationTokenSource? _cancellationTokenSource;

    public PdfViewerControl()
    {
        InitializeComponent();

        // Intercepte la molette en mode "Tunnel" 
        // L'événement est capturé et bloqué AVANT que le ScrollViewer natif ne scrolle.
        if (MainScrollViewer != null)
        {
            MainScrollViewer.AddHandler(InputElement.PointerWheelChangedEvent, ScrollViewer_PointerWheelChanged,
                Avalonia.Interactivity.RoutingStrategies.Tunnel);
        }

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
                this.Focus();
            }
            else
            {
                CloseDocument(); // On ferme si le binding devient nul
            }
        }
    }

    private void ImageViewbox_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        var properties = e.GetCurrentPoint(this).Properties;

        // On n'active le drag que si le bouton GAUCHE est enfoncé
        if (properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            _lastMousePosition = e.GetPosition(this); // Mémorise la position brute
            e.Pointer.Capture(ImageViewbox); // Capture le pointeur pour continuer le suivi même en dehors

            // Visual feedback : on remplace "Hand" par "Grabbing" (si disponible nativement)
            // Ou simplement "SizeAll" pour simuler une saisie. 
            // On le fait sur le parent visuel pour plus de visibilité.
            if (this.Parent is Control p) p.Cursor = new Cursor(StandardCursorType.SizeAll);
        }
    }

    private void ImageViewbox_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (_isDragging && MainScrollViewer != null)
        {
            Point currentMousePosition = e.GetPosition(this);

            // Calcule la différence brute (le delta)
            Vector delta = _lastMousePosition - currentMousePosition;

            // Applique le décalage directement sur le ScrollViewer existant
            // On soustrait car déplacer le contenu vers la droite (delta négatif)
            // nécessite de déplacer le viewport vers la gauche (offset négatif).
            MainScrollViewer.Offset = MainScrollViewer.Offset + delta;

            // Mémorise la nouvelle position pour le prochain calcul
            _lastMousePosition = currentMousePosition;
        }
    }

    private void ImageViewbox_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        // Nettoyage si le bouton relâché est le gauche
        if (e.InitialPressMouseButton == MouseButton.Left)
        {
            _isDragging = false;
            e.Pointer.Capture(null); // Libère la capture

            // Remet le curseur par défaut
            if (this.Parent is Control p) p.Cursor = null;
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

            // 2. Redimensionnement (ex: 2.5Mp) pour la légèreté
            var fluxResized = ImageProcessor.ResizeImagesAsync(fluxImages, 2_500_000, _cancellationTokenSource.Token);

            await foreach (var skBitmap in fluxResized)
            {
                // 3. Compression WebP RAM agressive
                byte[] compressedPage = await Task.Run(() =>
                {
                    using var image = SKImage.FromBitmap(skBitmap);
                    using var data = image.Encode(SKEncodedImageFormat.Webp, 45);
                    return data.ToArray();
                }, _cancellationTokenSource.Token);

                _pagesCompresses.Add(compressedPage);
                skBitmap.Dispose(); // Vital

                if (_pagesCompresses.Count == 1) await AfficherPageAsync(0);
                MettreAJourInterface();
            }

            IndicateurStatut.Text = "Lecture terminée.";
        }
        catch (OperationCanceledException)
        {
            IndicateurStatut.Text = "Lecture annulée.";
        }
        catch (Exception ex)
        {
            IndicateurStatut.Text = $"Erreur : {ex.Message}";
        }
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

            // 2. Redimensionnement (ex: 2.5Mp) pour éviter de saturer la RAM, géré sur thread secondaire
            var fluxResized = ImageProcessor.ResizeImagesAsync(fluxImages, 2_500_000, _cancellationTokenSource.Token);

            await foreach (var skBitmap in fluxResized)
            {
                // 3. L'encodage WebP est lourd (CPU-bound) : Task.Run obligatoire pour ne pas geler l'UI
                byte[] compressedData = await Task.Run(() =>
                {
                    using var image = SKImage.FromBitmap(skBitmap);
                    // Compression agressive (qualité 45) type "CBZ/BDD"
                    using var data = image.Encode(SKEncodedImageFormat.Webp, 45);
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

        //IndicateurStatut.Text = $"Décompression de la page {index+1}...";

        // Capture du token d'annulation actuel
        var token = _cancellationTokenSource?.Token ?? CancellationToken.None;

        try
        {
            // Décodage sur thread secondaire
            var nouvelleImage = await Task.Run(() =>
            {
                if (token.IsCancellationRequested || index >= _pagesCompresses.Count) return null;

                using var ms = new MemoryStream(_pagesCompresses[index]);
                return new Avalonia.Media.Imaging.Bitmap(ms);
            }, token);

            if (nouvelleImage == null || token.IsCancellationRequested)
            {
                nouvelleImage?.Dispose(); // On jette l'image si on a changé de page entre-temps
                return;
            }

            // Mise à jour sur Thread principal
            Dispatcher.UIThread.Post(() =>
            {
                if (token.IsCancellationRequested)
                {
                    nouvelleImage.Dispose();
                    return;
                }

                // CRITIQUE : Délier d'abord !
                if (ImagePdf != null) ImagePdf.Source = null;
                _imageCourante?.Dispose();

                _imageCourante = nouvelleImage;
                if (ImagePdf != null) ImagePdf.Source = _imageCourante;

                _pageCouranteIndex = index;
                _currentOriginalImageSize = _imageCourante.Size;

                MettreAJourInterface();
                AjusterALaHauteurDefaut();
            });
        }
        catch (OperationCanceledException)
        {
            // Le chargement de l'image a été annulé proprement, on ne fait rien
        }
    }

    /// <summary>
    /// Calcule le zoom pour que l'image tienne sur toute la hauteur disponible
    /// </summary>
    private void AjusterALaHauteurDefaut()
    {
        if (_imageCourante == null || MainScrollViewer == null) return;

        // On s'assure que le ScrollViewer a été rendu (Bounds valides)
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Récupère la largeur disponible réelle (Viewport)
            double availableHeight = MainScrollViewer.Bounds.Height;

            // Récupère la largeur native de l'image (plus la marge de 10px définies dans le XAML)
            double imageHeight = _currentOriginalImageSize.Height + 20;

            if (availableHeight > 0 && imageHeight > 0)
            {
                // Calcule le facteur de zoom nécessaire
                SetZoom(availableHeight / imageHeight);
            }
        }, DispatcherPriority.Loaded);
    }

    private void SetZoom(double targetScale)
    {
        // Clamp du zoom (Min / Max)
        targetScale = Math.Max(MinZoomScale, Math.Min(MaxZoomScale, targetScale));
        _currentZoomScale = targetScale;

        // Applique l'échelle en ajustant les dimensions brute de l'image DANS le Viewbox
        if (_imageCourante != null && ImageViewbox != null)
        {
            // C'est le Viewbox qui va appliquer le Stretch="Uniform" sur ces nouvelles dimensions
            ImageViewbox.Width = _currentOriginalImageSize.Width * _currentZoomScale;
            ImageViewbox.Height = _currentOriginalImageSize.Height * _currentZoomScale;
        }

        // Met à jour l'interface avec le pourcentage
        MettreAJourStatutZoom();
    }

    /// <summary>
    /// Gestion du zoom avec Ctrl + Molette
    /// </summary>
    private void ScrollViewer_PointerWheelChanged(object? sender, Avalonia.Input.PointerWheelEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            e.Handled = true; // Stoppe net le scroll natif grâce au RoutingStrategy.Tunnel !

            double factor = e.Delta.Y > 0 ? ZoomFactor : (1.0 / ZoomFactor);
            double newScale = Math.Max(MinZoomScale, Math.Min(MaxZoomScale, _currentZoomScale * factor));

            // Zoom avec point d'ancrage ciblé sous le curseur de la souris
            ApplyZoomWithAnchor(newScale, e.GetPosition(MainScrollViewer));
        }
    }

    /// <summary>
    /// Applique le zoom tout en gardant un point fixe à l'écran (ex: sous la souris)
    /// </summary>
    private void ApplyZoomWithAnchor(double newScale, Point anchorViewportPos)
    {
        if (MainScrollViewer == null || newScale == _currentZoomScale) return;

        // Ratio d'évolution entre l'ancien et le nouveau zoom
        double scaleRatio = newScale / _currentZoomScale;

        // 1. Calculer la position absolue du point d'ancrage dans le grand document
        Vector absolutePos = MainScrollViewer.Offset + new Vector(anchorViewportPos.X, anchorViewportPos.Y);

        // 2. Appliquer la nouvelle taille (La méthode SetZoom que tu as déjà)
        SetZoom(newScale);

        // 3. Calculer où se trouve maintenant ce même point après le redimensionnement
        Vector newAbsolutePos = absolutePos * scaleRatio;

        // 4. Déduire le nouvel offset du ScrollViewer pour que le point reste visuellement immobile
        Vector newOffset = newAbsolutePos - new Vector(anchorViewportPos.X, anchorViewportPos.Y);

        // 5. Appliquer l'offset uniquement après le redimensionnement du Layout visuel
        Dispatcher.UIThread.Post(() => { MainScrollViewer.Offset = newOffset; }, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Gestion des raccourcis clavier Ctrl + ou Ctrl -
    /// </summary>
    private void UserControl_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            double factor = 1.0;
            if (e.Key == Key.OemPlus || e.Key == Key.Add) factor = ZoomFactor;
            else if (e.Key == Key.OemMinus || e.Key == Key.Subtract) factor = 1.0 / ZoomFactor;

            if (factor != 1.0)
            {
                e.Handled = true;
                double newScale = Math.Max(MinZoomScale, Math.Min(MaxZoomScale, _currentZoomScale * factor));

                // Au clavier, comme on n'a pas de souris, on cible le milieu de l'écran
                Point center = new Point(MainScrollViewer.Bounds.Width / 2, MainScrollViewer.Bounds.Height / 2);
                ApplyZoomWithAnchor(newScale, center);
            }
        }

        switch (e.Key)
        {
            case Key.Left:
            case Key.PageUp:
                PagePrecedente();
                e.Handled = true;
                break;

            case Key.Right:
            case Key.PageDown:
            case Key.Space:
                PageSuivante();
                e.Handled = true;
                break;

            case Key.Home:
                AllerAPage(0);
                e.Handled = true;
                break;

            case Key.End:
                AllerAPage(_pagesCompresses.Count - 1);
                e.Handled = true;
                break;
        }
    }

    public async void PageSuivante()
    {
        // On utilise _pagesCompresses qui est ta vraie liste de données
        if (_pagesCompresses != null && _pageCouranteIndex < _pagesCompresses.Count - 1)
        {
            await AfficherPageAsync(_pageCouranteIndex + 1);
        }
    }

    public async void PagePrecedente()
    {
        if (_pageCouranteIndex > 0)
        {
            await AfficherPageAsync(_pageCouranteIndex - 1);
        }
    }

    public async void AllerAPage(int index)
    {
        if (_pagesCompresses != null && index >= 0 && index < _pagesCompresses.Count)
        {
            await AfficherPageAsync(index);
        }
    }

    /// <summary>
    /// Nettoie complètement la mémoire du composant
    /// </summary>
    public void CloseDocument()
    {
        _cancellationTokenSource?.Cancel();

        // Action de nettoyage sécurisée
        Action nettoyage = () =>
        {
            // Empêcher le moteur de rendu (MeasureOverride) de lire une image morte.
            if (ImagePdf != null) ImagePdf.Source = null;

            // Nettoyage
            _imageCourante?.Dispose();
            _imageCourante = null;

            _pagesCompresses.Clear();
            _pageCouranteIndex = -1;

            // Réinitialise le Viewbox
            if (ImageViewbox != null)
            {
                ImageViewbox.Width = Double.NaN;
                ImageViewbox.Height = Double.NaN;
            }

            MettreAJourInterface();
        };

        // Si on est déjà sur le thread UI, on l'exécute tout de suite. Sinon on la met en file d'attente.
        if (Dispatcher.UIThread.CheckAccess())
            nettoyage();
        else
            Dispatcher.UIThread.Post(nettoyage);
    }

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
                IndicateurStatut.Text = "Aucun document chargé.";

                // Réinitialise le Viewbox
                if (ImageViewbox != null)
                {
                    ImageViewbox.Width = Double.NaN;
                    ImageViewbox.Height = Double.NaN;
                }
            }
            else
            {
                IndicateurPage.Text = $"{_pageCouranteIndex + 1} / {_pagesCompresses.Count}";
                IndicateurStatut.Text = "Document chargé (Optimisé).";
                MettreAJourStatutZoom(); // Met à jour le % de zoom
            }
        });
    }

    private void MettreAJourStatutZoom()
    {
        // Affiche le pourcentage brut dans l'indicateur de statut
        IndicateurStatut.Text = $"Zoom : {(_currentZoomScale * 100):F0} %";
    }

    private async void BoutonTelecharger_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (CbzData == null || CbzData.Length == 0)
        {
            IndicateurStatut.Text = "Aucun document à télécharger.";
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        // Nettoyage du nom pour enlever les caractères interdits par Windows (ex: \ / : * ? " < > |)
        string safeTitle = string.IsNullOrWhiteSpace(DocumentTitle)
            ? "Evaluation"
            : string.Join("_", DocumentTitle.Split(Path.GetInvalidFileNameChars()));

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(
            new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Télécharger le document",
                DefaultExtension = "pdf",
                SuggestedFileName = $"{safeTitle}.pdf",
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Document PDF") { Patterns = new[] { "*.pdf" } }
                }
            });

        if (file != null)
        {
            try
            {
                IndicateurStatut.Text = "Création du PDF en cours...";

                string localPath = file.Path.LocalPath;

                // Extrait les images de notre archive WebP en mémoire
                var fluxImages = ArchiveManager.ExtractImagesFromMemoryAsync(CbzData);

                // Utilise ton gestionnaire pour ré-assembler un vrai PDF sur le disque
                await PdfManager.CreatePdfAsync(fluxImages, localPath, 75);

                IndicateurStatut.Text = "PDF sauvegardé avec succès.";
            }
            catch (Exception ex)
            {
                IndicateurStatut.Text = $"Erreur : {ex.Message}";
            }
        }
    }
}