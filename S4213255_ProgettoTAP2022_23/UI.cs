using System;
using Entities;

namespace ProgettoUI {
    class UI {
        static void Main(string[] args)
        {
            //Vogliamo utilizzare per il momento l'interfaccia utente per far eseguire il DbContext, per vedere che base di dati viene creata
            using (var c = new ProjContext())
            {
                //usando questo quindi tiriamo su e buttiamo giù la base di dati
                c.Database.EnsureDeleted();
                c.Database.EnsureCreated();
            }
            Console.WriteLine("Hello World!");

        }
    }
}