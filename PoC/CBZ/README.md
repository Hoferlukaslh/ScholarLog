# ScanHandler
explication

but
objectif


## Diagramme de classe 
``` mermaid
classDiagram

  %% Cette classe est actuellement un four tout %%
  class ScanHandler{
  %% propriétés %%
  
  %% méthodes %%
    +CreatePDF(string imagesFolder, String pdfPath, int quality)$
    +ExtractCbzImages(string PathToArchive, string folderPath)$
    +ExtractPdfPages(string pdfPath, string outputFolder, SKEncodedImageFormat format, int quality)$
    +ResizeToMaxPixels(SKBitmap source, double maxPixels) SKBitmap$
    +CompresserRepertoire(string sourceDirectory, string destinationZipFilePath)$
  }

    
```