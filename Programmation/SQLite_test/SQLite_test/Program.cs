using Microsoft.Data.Sqlite;

namespace SQLite_test;

class Program
{
    static void Main(string[] args)
    {
        // 1. On instancie le repository unique
        var repo = new DataRepository();

        // Création des modules
        List<Module> modules = new List<Module>();
        /*modules.Add(new Module { Nom = "M0 - Connaissances fondamentales" });
        modules.Add(new Module { Nom = "M1 - Management" });
        modules.Add(new Module { Nom = "M2 - Automatisation et robotique" });
        modules.Add(new Module { Nom = "M3 - Informatique industrielle" });
        modules.Add(new Module { Nom = "M4 - Réseaux et protocoles de communication" });
        
        foreach (Module module in modules)
        {
            repo.AjouterModule(module);
            Console.WriteLine($"Module : {module.Nom} ajouté a la BDD");
        }
        
        
        // Ajouter un module, mais pas dans la BDD
        foreach (Module module in modules)
        {
            Console.WriteLine($"Module : {module.Nom} dans la liste");
        }
        
        */
        
        modules = repo.GetModules(); // Actualisation
         
        // Affichage
        Console.WriteLine($"Voici les {modules.Count} modules dans la BDD : ");
        foreach (Module module in modules) Console.WriteLine($"Module : {module.Id} - {module.Nom}");
        

        /*
        Branche maBranche = new Branche();
        maBranche = modules[3].Branches[0];
  

        var note = new Note
        {
            Date =  DateTime.Now,
            Description = " ",
            Valeur = 7.8f,
            BrancheId = maBranche.Id,
        };
        
        repo.AjouterNote(note);
        
        
        modules = repo.GetModules(); // Actualisation
        
        
        foreach (var m in modules)
        {
            Console.WriteLine($"Module : {m.Nom} - Moyenne : {m.CalculerMoyenne()}");
        }*/
        
        /*
        TypeTravail travail = new TypeTravail
        {
            Nom = "COUCOU"
        };
        
        repo.AjouterTypeTravail(travail);

        Entree entree = new Entree
        {
            Date = DateTime.Now,
            Duree = 2.5f,
            TypeTravailId =  travail.Id,
            ModuleId = modules[0].Id,
            Description = "test de déploiement",

        };
        
        repo.AjouterEntree(entree);
        
        
        */
       
        
       
        
        TypeTravail newType =  new TypeTravail { Nom = "wow" };
        
        repo.AjouterTypeTravail(newType);
        
        List<TypeTravail> typesTravails = repo.GetTypeTravail();
        modules = repo.GetModules(); // Actualisation
        
        foreach (TypeTravail t in typesTravails)
        {
            Console.WriteLine($"{t.Id} -  {t.Nom}");
        }
        
        
        // afficher les entrées du module 0 :
        foreach (Entree e in modules[0].Entrees)
        {
            Console.WriteLine($"{e.Id} - {e.Date} - {typesTravails[e.TypeTravailId-1].Nom}: {e.Description}");
        }
    }
}