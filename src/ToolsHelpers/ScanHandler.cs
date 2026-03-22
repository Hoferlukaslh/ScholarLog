/*
    Fichier :   ScanHandler.cs
    Auteur  :   Lukas Hofer - TINF2
    Date    :   22.03.2026
    
    Projet  :   ScholarLog
    
    But     :   Module principal de traitement, compression et restitution des documents PDF.
            
                Objectif technique : Maintenir l'empreinte de la BDD extrêmement basse 
                (estimation max ~60 Mo) tout en garantissant une lecture haute qualité 
                et une gestion stricte de la RAM (Stream/In-Memory).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using SkiaSharp;
using PdfConvert = PDFtoImage.Conversion;

namespace ScholarLog;


/// <summary>
/// Responsable de la manipulation d'image
/// </summary>
public static class ImageProcessor
{
    /// <summary>
    /// Redimmentionne une image bitmap vers une définition maximal
    /// </summary>
    /// <param name="source">Image source</param>
    /// <param name="maxPixels">Mp maximal que dois faire l'image de sortie</param>
    /// <returns>Image de sortie</returns>
    public static SKBitmap ResizeToMaxPixels(SKBitmap source, double maxPixels)
    {
        double currentPixels = (double)source.Width * source.Height;

        // Si l'image est déjà plus petite que la cible, on retourne l'originale
        if (currentPixels <= maxPixels)
        {
            return source;
        }

        // Calcul du facteur d'échelle (Ratio)
        double scale = Math.Sqrt(maxPixels / currentPixels);
        int newWidth = (int)Math.Round(source.Width * scale);
        int newHeight = (int)Math.Round(source.Height * scale);

        // Redimensionnement avec haute qualité (Sinc/Lanczos)
        return source.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High);
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
    public static void CreatePdf(IEnumerable<SKBitmap> images, string pdfPath, int quality = 50)
    {
        var pdfMetadata = new SKDocumentPdfMetadata { EncodingQuality = quality };

        try
        {
            using (var stream = File.Create(pdfPath))
            using (var document = SKDocument.CreatePdf(stream, pdfMetadata))
            {
                foreach (var bitmap in images)
                {
                    if (bitmap == null) continue;

                    using (var canvas = document.BeginPage(bitmap.Width, bitmap.Height))
                    {
                        canvas.DrawBitmap(bitmap, 0, 0);
                    }
                    
                    document.EndPage();
                    bitmap.Dispose();
                }
                document.Close();
            }
            Console.WriteLine($"Succès : PDF généré dans '{pdfPath}'.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Une erreur est survenue lors de la création du PDF : {ex.Message}");
        }
    }
    
    /// <summary>
    /// Extrait les pages d'un PDF et les retourne une par une
    /// </summary>
    /// <param name="pdfPath">Chemin vers le PDF source</param>
    /// <returns>Flux d'images</returns>
    public static IEnumerable<SKBitmap> ExtractImages(string pdfPath)
    {
        if (!File.Exists(pdfPath)) yield break;

        // Le flux est maintenu ouvert pendant toute l'itération (le yield return)
        using (FileStream stream = File.OpenRead(pdfPath))
        {
            var images = PdfConvert.ToImages(stream, leaveOpen: true);
            foreach (var bitmap in images)
            {
                yield return bitmap;
            }
        }
    }
}


/// <summary>
/// Responsable des archives
/// </summary>
public static class ArchiveManager
{

    /// <summary>
    /// Extrait les images d'un CBZ/ZIP à la volée, SANS les écrire sur le disque
    /// </summary>
    /// <param name="pathToArchive">Chemin vers l'archive source</param>
    /// <returns>Flux d'images</returns>
    public static IEnumerable<SKBitmap> ExtractImages(string pathToArchive)
    {
        if (!File.Exists(pathToArchive)) yield break;

        using (var archive = ZipFile.OpenRead(pathToArchive))
        {
            var entries = archive.Entries
                .Where(e => IsSupportedImageFilePDF(e.FullName))
                .OrderBy(e => e.FullName);

            foreach (var entry in entries)
            {
                using (var stream = entry.Open())
                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    ms.Position = 0; // début flux au 
                    
                    var bitmap = SKBitmap.Decode(ms);
                    if (bitmap != null)
                    {
                        yield return bitmap;
                    }
                }
            }
        }
    }

   /// <summary>
   /// Compresse le répertoire en archive zip
   /// </summary>
   /// <param name="sourceDirectory">Répertoire source</param>
   /// <param name="destinationZipFilePath">Répertoire de destination</param>
   /// <exception cref="DirectoryNotFoundException">Erreur</exception>
    public static void CompressDirectory(string sourceDirectory, string destinationZipFilePath)
    {
        try
        {
            if (!Directory.Exists(sourceDirectory))
                throw new DirectoryNotFoundException($"Le dossier source n'existe pas : {sourceDirectory}");

            string destDirectory = Path.GetDirectoryName(destinationZipFilePath);
            if (!string.IsNullOrEmpty(destDirectory) && !Directory.Exists(destDirectory))
                Directory.CreateDirectory(destDirectory);

            if (File.Exists(destinationZipFilePath)) File.Delete(destinationZipFilePath); 

            ZipFile.CreateFromDirectory(sourceDirectory, destinationZipFilePath, CompressionLevel.Optimal, false);
            Console.WriteLine("L'archive a été créée avec succès !");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Une erreur est survenue lors de la compression : {ex.Message}");
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
   /// Crée une archive CBZ en mémoire et retourne les octets (idéal pour un BLOB SQLite)
   /// </summary>
   /// <param name="images">Flux d'images</param>
   /// <param name="format">Format d'encodage des images</param>
   /// <param name="quality">Qualité : 0=mauvais, 100=bon</param>
   /// <returns></returns>
    public static byte[] CreateCbzInMemory(IEnumerable<SKBitmap> images, SKEncodedImageFormat format = SKEncodedImageFormat.Webp, int quality = 40)
    {
        using (var ms = new MemoryStream())
        {
            // On crée le ZIP directement dans le MemoryStream
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                int i = 1;
                foreach (var bitmap in images)
                {
                    if (bitmap == null || bitmap.IsEmpty) continue;

                    string ext = format.ToString().ToLowerInvariant();
                    if (ext == "jpeg") ext = "jpg";

                    // Création de l'entrée dans le zip
                    var entry = archive.CreateEntry($"{i:D3}.{ext}", CompressionLevel.Optimal);
                    
                    using (var entryStream = entry.Open())
                    using (var image = SKImage.FromBitmap(bitmap))
                    using (var data = image.Encode(format, quality))
                    {
                        // On écrit l'image compressée (WebP) directement dans le ZIP
                        if (data != null) data.SaveTo(entryStream);
                    }

                    bitmap.Dispose(); // Libération immédiate
                    i++;
                }
            } // Le ZIP est finalisé ici
            
            return ms.ToArray(); // Retourne le fichier complet sous forme de tableau d'octets
        }
    }

    /// <summary>
    /// Extrait les images d'un CBZ stocké en mémoire (byte[])
    /// </summary>
    /// <param name="cbzData">Blob de données brut</param>
    /// <returns>Flux d'images</returns>
    public static IEnumerable<SKBitmap> ExtractImagesFromMemory(byte[] cbzData)
    {
        // Pas de bloc "using" sur le MemoryStream principal car le flux doit 
        // rester ouvert pendant tout le "yield return"
        var ms = new MemoryStream(cbzData);
        var archive = new ZipArchive(ms, ZipArchiveMode.Read);

        var entries = archive.Entries
            .Where(e => IsSupportedImageFilePDF(e.FullName))
            .OrderBy(e => e.FullName);

        foreach (var entry in entries)
        {
            using (var entryStream = entry.Open())
            using (var tempMs = new MemoryStream()) 
            {
                entryStream.CopyTo(tempMs);
                tempMs.Position = 0;

                var bitmap = SKBitmap.Decode(tempMs);
                if (bitmap != null)
                {
                    yield return bitmap;
                }
            }
        }

        // Nettoyage manuel à la fin de l'itération
        archive.Dispose();
        ms.Dispose();
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
    /// <param name="folderPath">Dossier source</param>
    /// <returns>Flux d'images</returns>
    public static IEnumerable<SKBitmap> GetImagesFromDirectory(string folderPath)
    {
        if (!Directory.Exists(folderPath)) yield break;

        var imageFiles = Directory.GetFiles(folderPath)
            .Where(ArchiveManager.IsSupportedImageFilePDF) // Utilisation de la dépendance
            .OrderBy(f => f);

        foreach (var file in imageFiles)
        {
            var bitmap = SKBitmap.Decode(file);
            if (bitmap != null)
            {
                yield return bitmap;
            }
        }
    }

    /// <summary>
    /// Prend un flux d'images et les sauvegarde physiquement dans un dossier
    /// </summary>
    /// <param name="images">Flux d'images</param>
    /// <param name="outputFolder">Dossier de sortie</param>
    /// <param name="format">Format d'image</param>
    /// <param name="quality">Qualité : 0=mauvais, 100=bon</param>
    public static void SaveImages(IEnumerable<SKBitmap> images, string outputFolder, 
        SKEncodedImageFormat format = SKEncodedImageFormat.Webp, int quality = 35)
    {
        if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

        int i = 1;
        foreach (var bitmap in images)
        {
            if (bitmap == null || bitmap.IsEmpty) continue;

            string ext = format.ToString().ToLowerInvariant();
            if (ext == "jpeg") ext = "jpg";
            
            string filePath = Path.Combine(outputFolder, $"{i:D3}.{ext}");

            try
            {
                using (var image = SKImage.FromBitmap(bitmap))
                using (var data = image.Encode(format, quality))
                {
                    if (data != null)
                    {
                        using (var outputStream = File.Create(filePath))
                        {
                            data.SaveTo(outputStream);
                        }
                        Console.WriteLine($"Page {i} traitée et sauvegardée.");
                    }
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