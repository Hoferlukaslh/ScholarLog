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
        
        string pdf = @"C:\Users\lukas\Desktop\CBZ\TEST\base.pdf";
        
        string pdfName = "";
        string pdfBaseDirectory = "";
        
        if (File.Exists(pdf))
        {
            pdfName = Path.GetFileNameWithoutExtension(pdf);
            pdfBaseDirectory =  Path.GetDirectoryName(pdf);
        }

        string imageFolder = Path.Combine(pdfBaseDirectory, pdfName);
        ExtractPdfPages(pdf, imageFolder, SKEncodedImageFormat.Webp, 5);                   // Extrait les image dans un dossier
        CompresserRepertoire(imageFolder, imageFolder + ".cbz");   // Créer une archive CBZ
        
        if (Directory.Exists(imageFolder)) Directory.Delete(imageFolder, true);          // Supprime le dossier d'image précédement crée
        
        ExtractCbzImages(imageFolder + ".cbz", imageFolder);                         // Extrait les image de l'archive
        
        if(File.Exists(imageFolder + ".cbz"))  File.Delete(imageFolder + ".cbz");      // Supprime l'archive
    }

    private static void ExtractCbzImages(string pathToArchive, string folderPath)
    {
        // Vérification de l'existence de l'archive
        if (File.Exists(pathToArchive))
        {
            try
            {
                // Création du dossier de destination s'il n'existe pas
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                // Extraction de l'archive
                ZipFile.ExtractToDirectory(pathToArchive, folderPath, true);
                Console.WriteLine($"Succès : Extraction terminée dans '{folderPath}'.");
            }
            catch (InvalidDataException)
            {
                Console.WriteLine("Erreur : Le fichier spécifié n'est pas une archive valide (il est peut-être corrompu).");
                    
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Une erreur inattendue est survenue : {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"Erreur : L'archive '{pathToArchive}' est introuvable.");
        }
    }


    private static void ExtractPdfPages(string pdfPath, string outputFolder, 
        SKEncodedImageFormat format = SKEncodedImageFormat.Webp, int quality = 25)
    {
        if (!File.Exists(pdfPath)) return;
        if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

        // On utilise un bloc using pour garantir la fermeture du fichier dès la fin de la fonction
        using (FileStream stream = File.OpenRead(pdfPath))
        {
            int i = 1;
            
            var images = PdfConvert.ToImages(stream, leaveOpen: true);

            foreach (SKBitmap originalBitmap in images)
            {
                if (originalBitmap == null || originalBitmap.IsEmpty) continue;

                // Utilisation d'un bloc try-finally pour s'assurer que TOUT est disposé
                SKBitmap finalBitmap = null;
                try
                {
                    finalBitmap = ResizeToMaxPixels(originalBitmap, 2_500_000);
                    string filePath = Path.Combine(outputFolder, $"{i:D3}.webp");

                    using (var image = SKImage.FromBitmap(finalBitmap))
                    using (var data = image.Encode(format, quality))
                    {
                        if (data != null)
                        {
                            using (var outputStream = File.Create(filePath))
                            {
                                data.SaveTo(outputStream);
                            }
                            Console.WriteLine($"Page {i} traitée.");
                        }
                    }
                }
                finally
                {
                    // CRITIQUE : Libérer l'image originale ET l'image redimensionnée
                    originalBitmap.Dispose(); 
                    finalBitmap?.Dispose();
                    i++;
                }
            }
        } // Le 'stream' est fermé et le fichier PDF est libéré ici.
    }
    
    private static SKBitmap ResizeToMaxPixels(SKBitmap source, double maxPixels)
    {
        double currentPixels = (double)source.Width * source.Height;

        // Si l'image est déjà plus petite que la cible, on ne fait rien
        if (currentPixels <= maxPixels)
        {
            // On retourne l'original (attention : ne pas dispose l'original si on l'utilise !)
            return source;
        }

        // Calcul du facteur d'échelle (Ratio)
        double scale = Math.Sqrt(maxPixels / currentPixels);
        int newWidth = (int)Math.Round(source.Width * scale);
        int newHeight = (int)Math.Round(source.Height * scale);

        // Redimensionnement avec haute qualité (Sinc/Lanczos)
        return source.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High);
    }
    
    
    /// Compresse le contenu d'un dossier dans un fichier ZIP.
    public static void CompresserRepertoire(string sourceDirectory, string destinationZipFilePath)
    {
        try
        {
            // Vérifier si le dossier source existe
            if (!Directory.Exists(sourceDirectory))
                throw new DirectoryNotFoundException($"Le dossier source n'existe pas : {sourceDirectory}");

            String destDirectory = "";
            
            // Vérifier si le dossier de destination existe, sinon le créer
            destDirectory = Path.GetDirectoryName(destinationZipFilePath);
            if (!Directory.Exists(destDirectory))
                Directory.CreateDirectory(destDirectory);


            // Écrase si existe
            if (File.Exists(destinationZipFilePath)) File.Delete(destinationZipFilePath); 

            // Création de l'archive
            ZipFile.CreateFromDirectory(sourceDirectory, destinationZipFilePath, CompressionLevel.Optimal, false);

            Console.WriteLine("L'archive a été créée avec succès !");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Une erreur est survenue : {ex.Message}");
        }
    }
}