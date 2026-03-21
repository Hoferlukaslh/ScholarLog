/*
    Fichier :   ScanHandler.cs
    Auteur  :   Lukas Hofer - TINF2
    Date    :   21.03.2026
    
    Projet  :   ScholarLog
    
    But     :   Démontrer l'archivage et l'extraction d'image (+compression) d'un scan pdf.
                Dans l'objectif de sauvegarder les scans dans la base de donnée SQLite.
            
                Des estimations montre que dans le pire des cas, les pdf de la BDD prendront
                un espace maximal de 60 Mo.
 */

using System.IO.Compression;
using SkiaSharp;
using PdfConvert = PDFtoImage.Conversion;

namespace CBZ;

/// <summary>
/// Responsable de la manipulation d'image
/// </summary>
public static class ImageProcessor
{
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

    internal static bool IsSupportedImageFilePDF(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".webp";
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
    public static void SaveImages(IEnumerable<SKBitmap> images, string outputFolder, 
        SKEncodedImageFormat format = SKEncodedImageFormat.Webp, int quality = 25)
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