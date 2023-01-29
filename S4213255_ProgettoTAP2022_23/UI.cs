using LogicClasses;

namespace ProgettoUI {
    class UI {
        static void Main(string[] args) {
            var ConnectionString = @"Data Source=.;Initial Catalog=Tap_Project_2022_23;Integrated Security = True;";
            var HostFactory = new HostFactoryObject ();
            HostFactory.CreateHost(ConnectionString);
            Console.WriteLine("Hello World!");
        }
    }
}