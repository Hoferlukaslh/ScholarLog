/*
    Fichier : Program.cs
    Auteur : Lukas Hofer - TINF2
    Date : 21.03.2026
    
    Projet : ScholarLog
    
    But :   Démontrer l'archivage et l'extraction d'image (+compression) d'un scan pdf.
            Dans l'objectif de sauvegarder les scans dans la base de donnée SQLite.
            
            Des estimations montre que dans le pire des cas, les pdf de la BDD prendront
            un espace maximal de 60 Mo.
 */

using SkiaSharp;
using System.IO.Compression;
using PdfConvert = PDFtoImage.Conversion;

namespace CBZ;


class Program
{
    static async Task Main(string[] args)
    {
        
        string pdf = @"C:\Users\lukas\Desktop\ScholarLog\PoC\CBZ\TEST\base.pdf";
        
        string pdfName = "";
        string pdfBaseDirectory = "";
        
        if (File.Exists(pdf))
        {
            pdfName = Path.GetFileNameWithoutExtension(pdf);
            pdfBaseDirectory =  Path.GetDirectoryName(pdf);
        }

        string imageFolder = Path.Combine(pdfBaseDirectory, pdfName);
        ScanHandler.ExtractPdfPages(pdf, imageFolder, SKEncodedImageFormat.Webp, 40);                  // Extrait les image dans un dossier
        ScanHandler.CompresserRepertoire(imageFolder, imageFolder + ".cbz");   // Créer une archive CBZ
        if (Directory.Exists(imageFolder)) Directory.Delete(imageFolder, true);          // Supprime le dossier d'image précédement crée
        
        ScanHandler.ExtractCbzImages(imageFolder + ".cbz", imageFolder);                         // Extrait les image de l'archive
        if(File.Exists(imageFolder + ".cbz"))  File.Delete(imageFolder + ".cbz");      // Supprime l'archive
        

        ScanHandler.CreatePDF(imageFolder, Path.Combine(pdfBaseDirectory, "Generated.pdf"));                 // Génération du PDF
        if (Directory.Exists(imageFolder)) Directory.Delete(imageFolder, true);          // Supprime le dossier d'image précédement crée
    }
    
    
}