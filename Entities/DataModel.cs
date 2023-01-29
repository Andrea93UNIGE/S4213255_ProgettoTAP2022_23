using System.ComponentModel.DataAnnotations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TAP22_23.AuctionSite.Interface;

namespace Entities
{
    public class ProjectDbContext : TapDbContext {
        public ProjectDbContext(string connectionString) : base(new DbContextOptionsBuilder<ProjectDbContext>().UseSqlServer(connectionString).Options) { }
        public virtual DbSet<DbAuction> Auctions { get; set; }
        public virtual DbSet<DbHost> Hosts { get; set; }
        public virtual DbSet<DbSession> Sessions { get; set; }
        public virtual DbSet<DbSite> Sites { get; set; }
        public virtual DbSet<DbUser> Users { get; set; }
        
        protected override void OnConfiguring(DbContextOptionsBuilder options) { 
            base.OnConfiguring(options);
            options.LogTo(Console.WriteLine).EnableSensitiveDataLogging();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            
            var host = modelBuilder.Entity<DbHost>();
            host.HasMany(h => h.ActiveSites).WithOne(s => s.ExternalHost).OnDelete(DeleteBehavior.Cascade);

            var site = modelBuilder.Entity<DbSite>();
            site.HasMany(s => s.ExternalUsers).WithOne(u => u.ExternalSite).OnDelete(DeleteBehavior.Cascade);
            site.HasMany(s => s.ExternalSessions).WithOne(s => s.ExternalSite).OnDelete(DeleteBehavior.Cascade);
            
            var session = modelBuilder.Entity<DbSession>();
            session.HasOne(s => s.User).WithOne(u => u.ExternalSession).OnDelete(DeleteBehavior.NoAction);
            session.HasMany(s => s.ExternalAuctions).WithOne(a => a.ActiveSession).OnDelete(DeleteBehavior.NoAction);
        }
    }
}

public class DbAuction {
    [Key]
    public int AuctionId { get; set; }
    public string Description { get; set; }
    public DateTime EndsOn { get; set; }
    public double MinimumBidIncrement { get; }
    public double CurrentPrice { get; set; }
    public double MaximumOffer { get; set; }
    public double StartingPrice { get; set; }
    public string? Winner { get; set; }
    public DbAuction(string sellerUsername, string description, DateTime endsOn, double minumumBidIncrement, 
        double startingPrice,double currentPrice, DbSession activeSession) {
        SellerUsername = sellerUsername;
        Description = description;
        EndsOn = endsOn;
        MinimumBidIncrement = minumumBidIncrement;
        StartingPrice = startingPrice;
        CurrentPrice = startingPrice;
        ActiveSession = activeSession;
    }

    public virtual DbUser? WinnerUser { get; set; }
    public virtual DbSession ActiveSession { get; set; }
    public virtual string ActiveSessionId { get; set; }
    public virtual string SellerUsername { get; set; }
    public DbAuction() { }
}



public class DbHost {
    [Key]
    public int HostId { get; set; }
    public virtual ICollection<DbSite> ActiveSites { get; set; } = new List<DbSite>();
    public DbHost() { }
}




public class DbSession {
    [Key]
    public string SessionId { get; set; }
    public DateTime ValidUntil { get; set; }
    public double MinimumBidIncrement { get; set; }
    public int SessionExpirationInSeconds { get; set; }
    public DbSession() { }
    public DbSession(string sessionId, DateTime validUntil, double minimumBidIncrement,int sessionExpirationInSeconds, DbSite externalSite, DbUser activeUser) {
        SessionId = sessionId;
        ValidUntil = validUntil;
        MinimumBidIncrement = minimumBidIncrement;
        SessionExpirationInSeconds = sessionExpirationInSeconds;
        ExternalSite = externalSite;
        User = activeUser;
    }

    public virtual DbSite ExternalSite { get; set; }
    public virtual string ExternalSiteName { get; set; }
    public virtual DbUser User { get; set; }
    public virtual string UserUsername { get; set; }
    public virtual List<DbAuction> ExternalAuctions { get; set; } = new List<DbAuction>();
}




public class DbSite {
    [Key, MinLength(DomainConstraints.MinSiteName), MaxLength(DomainConstraints.MaxSiteName)]
    public string Name { get; set; }
    [MinLength(DomainConstraints.MinTimeZone), MaxLength(DomainConstraints.MaxTimeZone)]
    public int Timezone { get; set; }
    public int SessionExpirationInSeconds { get; set; }
    public double MinimumBidIncrement { get; set; }
    public DbSite() { }
    public DbSite(string name, int timezone, int sessionExpirationInSeconds, double minimumBidIncrement, DbHost externalHost) {
        Name = name;
        Timezone = timezone;
        SessionExpirationInSeconds = sessionExpirationInSeconds;
        MinimumBidIncrement = minimumBidIncrement;
        ExternalHost = externalHost;
    }

    public virtual DbHost ExternalHost { get; set; }
    public virtual List<DbUser> ExternalUsers { get; set; } = new List<DbUser>();
    public virtual List<DbSession> ExternalSessions { get; set; } = new List<DbSession>();
}


public class DbUser {
    [Key, MinLength(DomainConstraints.MinUserName), MaxLength(DomainConstraints.MaxUserName)]
    public string Username { get; set; }
    [MinLength(DomainConstraints.MinUserPassword)]
    public string Password { get; set; }
    public DbUser() { }
    public DbUser(string username, string password, DbSite externalSite) {
        Username = username;
        Password = password;
        ExternalSite = externalSite;
    }
    public DbUser(string username) {
        Username = username;
    }

    public virtual DbSite ExternalSite { get; set; }
    public virtual DbSession ExternalSession { get; set; }
    public virtual ICollection<DbAuction> WonAuctions { get; set; }
}