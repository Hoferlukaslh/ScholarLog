/*
    Fichier :   ScanHandler.cs
    Auteur  :   Lukas Hofer - TINF2
    Date    :   22.03.2026
    
    Projet  :   ScholarLog
    
    But     :   Module principal de traitement, compression et restitution des documents PDF.
            
                Objectif technique : Maintenir l'empreinte de la BDD extrêmement basse 
                (estimation max ~60 Mo) tout en garantissant une lecture haute qualité, 
                une gestion stricte de la RAM (Stream/In-Memory), ET une interface 
                graphique (GUI) 100% fluide (Non-bloquante).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;
using PdfConvert = PDFtoImage.Conversion;

namespace ScholarLog;

/// <summary>
/// Responsable de la manipulation d'image
/// </summary>
public static class ImageProcessor
{
    /// <summary>
    /// Redimensionne une image bitmap vers une définition maximale
    /// </summary>
    /// <param name="source">Image source</param>
    /// <param name="maxPixels">Mp maximal que doit faire l'image de sortie</param>
    /// <param name="quality">Qualité du filtre (Medium par défaut pour soulager le CPU du GUI)</param>
    /// <returns>Image de sortie</returns>
    public static async Task<SKBitmap> ResizeToMaxPixelsAsync(SKBitmap source, double maxPixels, SKFilterQuality quality = SKFilterQuality.Medium)
    {
        double currentPixels = (double)source.Width * source.Height;

        if (currentPixels <= maxPixels)
            return source;

        double scale = Math.Sqrt(maxPixels / currentPixels);
        int newWidth = (int)Math.Round(source.Width * scale);
        int newHeight = (int)Math.Round(source.Height * scale);

        // Offload le redimensionnement (CPU-bound) sur un thread d'arrière-plan
        return await Task.Run(() => 
            source.Resize(new SKImageInfo(newWidth, newHeight), quality));
    }
    
    /// <summary>
    /// Fonction utilitaire pour appliquer le redimensionnement asynchrone sur un flux d'images.
    /// </summary>
    /// <param name="source">Image source</param>
    /// <param name="maxPixels">définition maximale</param>
    /// <param name="ct">Token d'annulation</param>
    /// <returns>Flux image</returns>
    public static async IAsyncEnumerable<SKBitmap> ResizeImagesAsync(IAsyncEnumerable<SKBitmap> source, double maxPixels, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var img in source.WithCancellation(ct))
        {
            var resized = await ImageProcessor.ResizeToMaxPixelsAsync(img, maxPixels);
            
            // Sécurité mémoire vitale : si l'image a été redimensionnée, on obtient une NOUVELLE instance.
            // Il faut absolument détruire l'image d'origine pour ne pas fuir en RAM.
            if (!ReferenceEquals(img, resized))
            {
                img.Dispose();
            }
            
            yield return resized;
        }
    }
}

/// <summary>
/// Responsable des fichiers PDF
/// </summary>
public static class PdfManager
{
    /// <summary>
    /// Crée un PDF en consommant un flux d'images
    /// </summary>
    /// <param name="images">Flux d'images</param>
    /// <param name="pdfPath">Chemin cible du PDF généré</param>
    /// <param name="quality">Qualité de compression : 0=mauvais, 100=bon</param>
    /// <param name="cancellationToken">Token d'annulation</param>
    public static async Task CreatePdfAsync(IAsyncEnumerable<SKBitmap> images, string pdfPath, int quality = 50, CancellationToken cancellationToken = default)
    {
        var pdfMetadata = new SKDocumentPdfMetadata { EncodingQuality = quality };

        try
        {
            // Utilisation des "using" simplifiés (C# 8+)
            using var stream = File.Create(pdfPath);
            using var document = SKDocument.CreatePdf(stream, pdfMetadata);

            await foreach (var bitmap in images.WithCancellation(cancellationToken))
            {
                if (bitmap == null) continue;

                // Le dessin sur le canvas est très rapide, mais on l'isole par sécurité
                await Task.Run(() =>
                {
                    using var canvas = document.BeginPage(bitmap.Width, bitmap.Height);
                    canvas.DrawBitmap(bitmap, 0, 0);
                    document.EndPage();
                }, cancellationToken);
                
                bitmap.Dispose(); // Libération immédiate vitale
            }
            document.Close();
            Console.WriteLine($"Succès : PDF généré dans '{pdfPath}'.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Une erreur est survenue lors de la création du PDF : {ex.Message}");
        }
    }
    
    /// <summary>
    /// Extrait les pages d'un PDF et les retourne une par une de manière asynchrone
    /// </summary>
    /// <param name="pdfPath">Chemin vers le PDF source</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Flux d'images</returns>
    public static async IAsyncEnumerable<SKBitmap> ExtractImagesAsync(string pdfPath, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!File.Exists(pdfPath)) yield break;

        using var stream = File.OpenRead(pdfPath);
        
        // On suppose que ToImages est synchrone. On l'encapsule intelligemment.
        var images = PdfConvert.ToImages(stream, leaveOpen: true);
        
        foreach (var bitmap in images)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // Task.Yield() rend brièvement la main au Thread UI entre chaque page
            await Task.Yield(); 
            yield return bitmap;
        }
    }
}

/// <summary>
/// Responsable des archives
/// </summary>
public static class ArchiveManager
{
    /// <summary>
    /// Extrait les images d'un CBZ/ZIP à la volée, sans bloquer l'UI
    /// </summary>
    /// <param name="pathToArchive">Chemin vers l'archive source</param>
    /// <param name="cancellationToken">Token d'annulation</param>
    /// <returns>Flux d'images</returns>
    public static async IAsyncEnumerable<SKBitmap> ExtractImagesAsync(string pathToArchive, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!File.Exists(pathToArchive)) yield break;

        using var archive = ZipFile.OpenRead(pathToArchive);
        var entries = archive.Entries
            .Where(e => IsSupportedImageFilePDF(e.FullName))
            .OrderBy(e => e.FullName);

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var stream = entry.Open();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, cancellationToken);
            ms.Position = 0;
            
            // Le décodage de l'image est CPU-bound : on l'envoie sur un thread de travail
            var bitmap = await Task.Run(() => SKBitmap.Decode(ms), cancellationToken);
            if (bitmap != null)
            {
                yield return bitmap;
            }
        }
    }

    /// <summary>
    /// Compresse le répertoire en archive zip de façon asynchronisé
    /// </summary>
    /// <param name="sourceDirectory">Répertoire source</param>
    /// <param name="destinationZipFilePath">Répertoire de destination</param>
    /// <exception cref="DirectoryNotFoundException">Erreur</exception>
    public static async Task CompressDirectoryAsync(string sourceDirectory, string destinationZipFilePath)
    {
        try
        {
            if (!Directory.Exists(sourceDirectory))
                throw new DirectoryNotFoundException($"Le dossier n'existe pas : {sourceDirectory}");

            string destDirectory = Path.GetDirectoryName(destinationZipFilePath);
            if (!string.IsNullOrEmpty(destDirectory) && !Directory.Exists(destDirectory))
                Directory.CreateDirectory(destDirectory);

            if (File.Exists(destinationZipFilePath)) File.Delete(destinationZipFilePath); 

            // La compression ZIP est lourde, on libère le thread UI
            await Task.Run(() => ZipFile.CreateFromDirectory(sourceDirectory, destinationZipFilePath, CompressionLevel.Optimal, false));
            Console.WriteLine("L'archive a été créée avec succès !");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur de compression : {ex.Message}");
        }
    }

    
    /// <summary>
    /// Compart les format supporté avec le format du fichier
    /// </summary>
    /// <param name="filePath">Chemin vers fichier</param>
    /// <returns>Oui = format supporté, non = non-supporté</returns>
    public static bool IsSupportedImageFilePDF(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".webp";
    }
    
    /// <summary>
    /// Crée une archive CBZ en mémoire à partir d'un flux asynchrone
    /// </summary>
    /// <param name="images">Flux d'images</param>
    /// <param name="format">Format d'encodage des images</param>
    /// <param name="quality">Qualité : 0=mauvais, 100=bon</param>
    /// <param name="cancellationToken">Token d'annulation</param>
    /// <returns>Blog de donnée de l'archive</returns>
    public static async Task<byte[]> CreateCbzInMemoryAsync(IAsyncEnumerable<SKBitmap> images, SKEncodedImageFormat format = SKEncodedImageFormat.Webp, int quality = 40, CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            int i = 1;
            await foreach (var bitmap in images.WithCancellation(cancellationToken))
            {
                if (bitmap == null || bitmap.IsEmpty) continue;

                string ext = format.ToString().ToLowerInvariant();
                if (ext == "jpeg") ext = "jpg";

                var entry = archive.CreateEntry($"{i:D3}.{ext}", CompressionLevel.Optimal);
                
                using (var entryStream = entry.Open())
                {
                    // L'encodage (surtout WebP) est très lourd : Task.Run obligatoire
                    await Task.Run(() => 
                    {
                        using var image = SKImage.FromBitmap(bitmap);
                        using var data = image.Encode(format, quality);
                        data?.SaveTo(entryStream);
                    }, cancellationToken);
                }

                bitmap.Dispose();
                i++;
            }
        } 
        return ms.ToArray(); 
    }

    /// <summary>
    /// Extrait les images d'un CBZ stocké en mémoire (byte[])
    /// </summary>
    /// <param name="cbzData">Blob de données brut</param>
    /// <param name="cancellationToken">Token d'annulation</param>
    /// <returns>Flux d'images</returns>
    public static async IAsyncEnumerable<SKBitmap> ExtractImagesFromMemoryAsync(byte[] cbzData, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Ces usings garantissent la fermeture propre même si le GUI fait un "Break" ou "Take(1)"
        using var ms = new MemoryStream(cbzData);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

        var entries = archive.Entries
            .Where(e => IsSupportedImageFilePDF(e.FullName))
            .OrderBy(e => e.FullName);

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var entryStream = entry.Open();
            using var tempMs = new MemoryStream();
            await entryStream.CopyToAsync(tempMs, cancellationToken);
            tempMs.Position = 0;

            // Décodage asynchrone
            var bitmap = await Task.Run(() => SKBitmap.Decode(tempMs), cancellationToken);
            if (bitmap != null)
            {
                yield return bitmap;
            }
        }
    }
}

/// <summary>
/// Responsable de l'interaction avec les dossiers classiques
/// </summary>
public static class DirectoryManager
{
    /// <summary>
    /// Lit un dossier et retourne les images une par une dans un flux d'image
    /// </summary>
    /// <param name="folderPath"></param>
    /// <param name="cancellationToken">Token d'annulation</param>
    /// <returns>Flux d'images</returns>
    public static async IAsyncEnumerable<SKBitmap> GetImagesFromDirectoryAsync(string folderPath, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(folderPath)) yield break;

        var imageFiles = Directory.GetFiles(folderPath)
            .Where(ArchiveManager.IsSupportedImageFilePDF)
            .OrderBy(f => f);

        foreach (var file in imageFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // Évite le gel de l'UI pendant la lecture du disque et le décodage
            var bitmap = await Task.Run(() => SKBitmap.Decode(file), cancellationToken);
            if (bitmap != null)
            {
                yield return bitmap;
            }
        }
    }

    /// <summary>
    /// Prend un flux d'images et les sauvegarde physiquement dans un dossier de manière non-synchronisé
    /// </summary>
    /// <param name="images">Flux d'images</param>
    /// <param name="outputFolder">Dossier de sortie</param>
    /// <param name="format">Format d'image</param>
    /// <param name="quality">Qualité : 0=mauvais, 100=bon</param>
    /// <param name="cancellationToken">Token d'annulation</param>
    public static async Task SaveImagesAsync(IAsyncEnumerable<SKBitmap> images, string outputFolder, 
        SKEncodedImageFormat format = SKEncodedImageFormat.Webp, int quality = 35, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

        int i = 1;
        await foreach (var bitmap in images.WithCancellation(cancellationToken))
        {
            if (bitmap == null || bitmap.IsEmpty) continue;

            string ext = format.ToString().ToLowerInvariant();
            if (ext == "jpeg") ext = "jpg";
            
            string filePath = Path.Combine(outputFolder, $"{i:D3}.{ext}");

            try
            {
                // Encodage sur un thread secondaire
                using var data = await Task.Run(() => 
                {
                    using var image = SKImage.FromBitmap(bitmap);
                    return image.Encode(format, quality);
                }, cancellationToken);

                if (data != null)
                {
                    using var outputStream = File.Create(filePath);
                    // Sauvegarde disque asynchrone
                    await outputStream.WriteAsync(data.ToArray(), cancellationToken);
                    Console.WriteLine($"Page {i} traitée et sauvegardée.");
                }
            }
            finally
            {
                bitmap.Dispose();
                i++;
            }
        }
    }
}