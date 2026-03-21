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

using System;
using System.IO;
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
        SKBitmap image = new SKBitmap();
        
        
        
        return  image;
    }
}


/// <summary>
/// PResponsable des fichiers PDF
/// </summary>
public static class PdfManager
{
    /// <summary>
    /// Crée un PDF en consommant un flux d'images
    /// </summary>
    /// <param name="images">Flux d'images</param>
    /// <param name="pdfPath">Chemin de sortie du fichier PDF de sortie</param>
    /// <param name="quality">Qualité des images : 0 = mauvais, 100 = très bon</param>
    public static void CreatePdf(IEnumerable<SKBitmap> images, string pdfPath, int quality = 50)
    {
        
    }
    
    
    /// <summary>
    /// Extrait les pages d'un PDF et les retourne une par une
    /// </summary>
    /// <param name="pdfPath">Chemin vers PDF source</param>
    /// <returns>Flux d'images</returns>
    public static IEnumerable<SKBitmap> ExtractImages(string pdfPath)
    {
        IEnumerable<SKBitmap> images = new List<SKBitmap>();
        
        
        return images;
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
    /// <param name="pathToArchive">Chemin vers l'archive CBZ</param>
    /// <returns>Flux d'images</returns>
    public static IEnumerable<SKBitmap> ExtractImages(string pathToArchive)
    {
        IEnumerable<SKBitmap> images = new List<SKBitmap>();
        
        
        
        return images;
    }

    /// <summary>
    /// Compresse le répertoire en archive zip
    /// </summary>
    /// <param name="sourceDirectory">Dossier source</param>
    /// <param name="destinationZipFilePath">Chemin vers l'archive de sortie</param>
    public static void CompressDirectory(string sourceDirectory, string destinationZipFilePath)
    {
        
    }

    /// <summary>
    /// Méthode uniquement utilisé en interne pour controler si le type est supporté par le format PDF.
    /// </summary>
    /// <param name="filePath">Chemin de l'image</param>
    /// <returns>Oui = supporté, Non = non-supporté</returns>
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
    /// <param name="folderPath">Dossier source</param>
    /// <returns>Flux d'images</returns>
    public static IEnumerable<SKBitmap> GetImagesFromDirectory(string folderPath)
    {
        IEnumerable<SKBitmap> images = new List<SKBitmap>();

        return images;
    }

    
    /// <summary>
    /// Prend un flux d'images et les sauvegarde physiquement dans un dossier
    /// </summary>
    /// <param name="images">Flux d'images</param>
    /// <param name="outputFolder">Dossier cible</param>
    /// <param name="format">Format cible</param>
    /// <param name="quality">Qualité cible (1-100)</param>
    public static void SaveImages(IEnumerable<SKBitmap> images, string outputFolder, 
        SKEncodedImageFormat format = SKEncodedImageFormat.Webp, int quality = 25)
    {
        
        
    }
}