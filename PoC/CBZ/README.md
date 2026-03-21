# ScanHandler
explication

but
objectif


## Diagramme de classe 
``` mermaid
classDiagram

  class ImageProcessor {
    %% Responsabilité : Manipulation d'images en mémoire %%
    +ResizeToMaxPixels(SKBitmap source, double maxPixels) SKBitmap
  }

  class PdfManager {
    %% Responsabilité : Génération et lecture de PDF %%
    +CreatePdf(IEnumerable~SKBitmap~ images, string pdfPath, int quality)
    +ExtractImages(string pdfPath) IEnumerable~SKBitmap~
  }

  class ArchiveManager {
    %% Responsabilité : Manipulation de fichiers compressés %%
    +ExtractImages(string pathToArchive) IEnumerable~SKBitmap~
    +CompressDirectory(string sourceDirectory, string destinationZipFilePath)
    ~IsSupportedImageFilePDF(string file) bool
  }

  class ImageStorageManager {
    %% Responsabilité : Sauvegarde physique des fichiers %%
    +SaveImages(IEnumerable~SKBitmap~ images, string outputFolder, SKEncodedImageFormat format, int quality)
  }

  %% Relations d'utilisation (dépendances) %%
  PdfManager ..> ImageProcessor : utilise
  ArchiveManager ..> ImageProcessor : utilise

    
```