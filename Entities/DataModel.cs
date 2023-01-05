using Microsoft.EntityFrameworkCore;
using TAP22_23.AuctionSite.Interface;

namespace Entities {
    public class ProjContext : TapDbContext {
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlServer(@"Data Source=.;Initial Catalog=Tap_Project_2022_23;Integrated Security = True;");
            options.LogTo(Console.WriteLine).EnableSensitiveDataLogging();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) { }

    }

    public class Auction {

    }

    public class Host {

    }

    public class User {

    }

    public class Site {

    }





}