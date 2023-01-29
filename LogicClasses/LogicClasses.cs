using Entities;
using Microsoft.Data.SqlClient;
using TAP22_23.AlarmClock.Interface;
using TAP22_23.AuctionSite.Interface;
using static TapProjLogic.Verify;
using ISite = TAP22_23.AuctionSite.Interface.ISite;

namespace LogicClasses
{
    public class LogicClasses {
        public class ClockFactoryClass : IAlarmClockFactory {
            public IAlarmClock InstantiateAlarmClock(int timezone) {
                try {
                    return new ClockClass(timezone, DateTime.Now);
                }
                catch (Exception) {
                    throw new AuctionSiteArgumentOutOfRangeException();
                }
            }

            public class ClockClass : IAlarmClock {
                public IAlarm InstantiateAlarm(int frequencyInMs) {
                    try {
                        return new AlarmClass();
                    }
                    catch (Exception) {
                        throw new AuctionSiteArgumentOutOfRangeException();
                    }
                }
                public DateTime Now { get; }
                public int Timezone { get; }
                public ClockClass(int TimeZone, DateTime Now) {
                    this.Timezone = TimeZone;
                    this.Now = Now;
                }
            }

            public class AlarmClass : IAlarm {
                public void Dispose() {
                    throw new NotImplementedException();
                }
                public event Action? RingingEvent;
            }
        }
        

        public class AuctionObject : IAuction {
            public IUser? CurrentWinner() {
                using var c = new ProjectDbContext(ConnectionString);
                var winnerUser = c.Auctions.Where(a => a.AuctionId == Id).Select(a => a.WinnerUser).Single();
                return winnerUser != null ? new UserObject(winnerUser.Username, winnerUser.Password, ConnectionString, AlarmClock) : null;
            }

            public double CurrentPrice() {
                try {
                    using var c = new ProjectDbContext(ConnectionString);
                    return c.Auctions.Where(a => a.AuctionId == Id).Select(a => a.CurrentPrice).Single();
                }
                catch (Exception e) {
                    if (e is AuctionSiteInvalidOperationException) throw;
                    throw new AuctionSiteUnavailableDbException(@"");
                }
            }

            public void Delete() {
                try {
                    using var c = new ProjectDbContext(ConnectionString);
                    var auctionToBeDelete = c.Auctions.Single(s => s.AuctionId == Id);
                    c.Auctions.Remove(auctionToBeDelete);
                    c.SaveChanges();
                }
                catch (Exception ) {
                    throw new AuctionSiteInvalidOperationException();
                }
            }

            public bool Bid(ISession session, double offer) {
                IsNull(session);
                IsCorrectOffer(offer);
                if (!IsValidISession(session, AlarmClock.Now))
                    throw new AuctionSiteArgumentException("The session is expired!");

                using var c = new ProjectDbContext(ConnectionString);
                var auction = ReturnAuction(Id, c);
                var loggedUser = c.Users.Single(s => s.Username == session.User.Username);
                var theSession = c.Sessions.Single(s => session.Id == s.SessionId);
                 
                //bidding on an auction increases the validity time of the bidder session
                theSession.ValidUntil = AlarmClock.Now.AddSeconds(theSession.SessionExpirationInSeconds);

                //check last winner user: 
                var lastWinner = auction.Winner;

                if (offer < auction.StartingPrice) return false;
                if (offer >= auction.StartingPrice) {

                    //SE IL VINCITORE FA UNA BID < DELLA VECCHIA MAXIMUMOFFER RITORNA FALSO
                    if (lastWinner == loggedUser.ToString() && offer < auction.MaximumOffer) return false;

                    //SE offer della 2° bid < di offer 1° bid il Winner non deve cambiare. 
                    if (lastWinner != loggedUser.Username && offer < auction.MaximumOffer) {
                        auction.CurrentPrice = offer + MinimumBidIncrement;
                        c.SaveChanges();
                        return true;
                    }

                    //CONTROLLO CHE LO STESSO BIDDER PUO FARE UNA 2° BID PIU GRANDE DELLA 1°, MA NON PUO FARNE UNA PIU PICCOLA
                    if (lastWinner == loggedUser.Username && offer < auction.MaximumOffer) return false;

                    //1° BID
                    if (lastWinner == null) {
                        auction.MaximumOffer = offer;
                        //current price rimane lo stesso.
                        auction.Winner = loggedUser.Username;
                        auction.WinnerUser = loggedUser;
                        c.SaveChanges();
                        return true;
                    }

                    //2° BID | SAME BIDDER
                    if (lastWinner == loggedUser.Username) {
                        auction.MaximumOffer = offer;
                        //current price rimane lo stesso.
                        auction.Winner = loggedUser.Username;
                        auction.WinnerUser = loggedUser;
                        c.SaveChanges();
                        return true;
                    }

                    //2° BID | DIFFERENT BIDDER | 2°BID > 1°BID
                    if (lastWinner != loggedUser.Username && offer > auction.MaximumOffer) {
                        //currentprice diventa numero più piccolo tra (offer) e (maximumOffer + minimumBidIncrement)
                        auction.CurrentPrice = Math.Min(offer, auction.MaximumOffer + MinimumBidIncrement);
                        auction.MaximumOffer = offer;
                        auction.Winner = loggedUser.Username;
                        auction.WinnerUser = loggedUser;
                        c.SaveChanges();
                        return true;
                    }
                }
                return true;
            }

            public override bool Equals(object o) {
                return o is AuctionObject auction && Id == auction.Id;
            }
            public AuctionObject(int id, IUser seller, string description, DateTime endsOn, double minumumBidIncrement,
                double startingPrice, string connectionString, IAlarmClock alarmClock)
            {
                Id = id;
                Seller = seller;
                Description = description;
                EndsOn = endsOn;
                MinimumBidIncrement = minumumBidIncrement;
                //StartingPrice = startingPrice;
                ConnectionString = connectionString;
                AlarmClock = alarmClock;
            }

            public AuctionObject(int id, string connectionString, IAlarmClock alarmClock) {
                Id = id;
                ConnectionString = connectionString;
                AlarmClock = alarmClock;
            }
            public int Id { get; set; }
            public string Description { get; set; }
            public DateTime EndsOn { get; set; }
            private IAlarmClock AlarmClock { get; }
            private string ConnectionString { get; }
            public double MinimumBidIncrement { get; set; }
            public IUser Seller { get; }
        }
    }


    
    public class HostObject : IHost {
        public void CreateSite(string name, int timezone, int sessionExpirationTimeInSeconds, double minimumBidIncrement) {
            IsEmpty(name);
            IsNull(name);
            IsValidSiteNameLength(name);
            IsCorrectTimezone(timezone);
            SessionExpirationTime(sessionExpirationTimeInSeconds);
            IsCorrectBidIncrement(minimumBidIncrement);

            using var c = new ProjectDbContext(ConnectionString);
            if (c.Sites.Any(s => s.Name == name)) throw new AuctionSiteNameAlreadyInUseException(@"Site named " + name + " already exists");
            var host = c.Hosts.Single();
            c.Sites.Add(new DbSite(name, timezone, sessionExpirationTimeInSeconds, minimumBidIncrement, host));
            c.SaveChanges();
        }
        public ISite LoadSite(string name) {
            IsEmpty(name);
            IsNull(name);
            IsValidSiteNameLength(name);

            DbSite site;
            using (var c = new ProjectDbContext(ConnectionString)) {
                try {
                    site = c.Sites.Single(s => s.Name == name);
                }
                catch (InvalidOperationException e) {
                    throw new AuctionSiteInexistentNameException(@"Site doesn't exists!");
                }
            }
            return new SiteObject(name,ConnectionString,AlarmClockFactory.InstantiateAlarmClock(site.Timezone));
        }

        public IEnumerable<(string Name, int TimeZone)> GetSiteInfos() {
            using (var c = new ProjectDbContext(ConnectionString))
                foreach (var site in c.Sites)
                    yield return (site.Name, site.Timezone);
        }

        private string ConnectionString { get; }
        private IAlarmClockFactory AlarmClockFactory { get; }
        public HostObject(IAlarmClockFactory alarmClockFactory, string connectionString) {
            AlarmClockFactory = alarmClockFactory;
            ConnectionString = connectionString;
        }
    }


    public class HostFactoryObject : IHostFactory {
        public void CreateHost(string connectionString) {
            IsNull(connectionString);
            IsMalformed(connectionString);
            try {
                using var c = new ProjectDbContext(connectionString);
                c.Database.EnsureDeleted();
                c.Database.EnsureCreated();
                c.Add(new DbHost());
                c.SaveChanges();
            }
            catch (SqlException e) { throw new AuctionSiteUnavailableDbException(@"Database connection error");
            }
        }

        public IHost LoadHost(string connectionString, IAlarmClockFactory alarmClockFactory) {
            IsNull(connectionString);
            IsMalformed(connectionString);

            if (alarmClockFactory == null) throw new AuctionSiteArgumentNullException(@"Alarmclock error");
            try {
                using var c = new ProjectDbContext(connectionString);
                if (!c.Hosts.Any()) throw new AuctionSiteInvalidOperationException(@"Cannot load host");
                return new HostObject(alarmClockFactory, connectionString);
            }
            catch (SqlException e) {
                throw new AuctionSiteUnavailableDbException(@"Database connection error");
            }
            
        }
    }



   




    public class SessionObject : ISession {
        public override bool Equals(object? obj) => this.Equals(obj as SessionObject);
        public bool Equals(SessionObject? s) {
            if (s is null) return false;
            return this == s;
        }
        public static bool operator ==(SessionObject a, SessionObject? b) {
            return a.Id.Equals(b.Id); }
        public static bool operator !=(SessionObject a, SessionObject b) {
            return !a.Id.Equals(b.Id);
        }

        public IAuction CreateAuction(string description, DateTime endsOn, double startingPrice) {
            IsNull(description);
            IsEmpty(description);
            IsStartingPriceCorrect(startingPrice);
            IsCorrectTime(endsOn, AlarmClock.Now);
            
            DbAuction newDbAuction;
            using (var c = new ProjectDbContext(ConnectionString)) {
                var dbSession = ReturnUserFromSession(User.Username, c);
                if (!IsValidSession(dbSession, Now)) throw new AuctionSiteInvalidOperationException(@"Session expired");
                newDbAuction = new DbAuction(User.Username, description, endsOn, MinimumBidIncrement, startingPrice, currentPrice, dbSession); 
                c.Auctions.Add(newDbAuction);
                var expirationTime = ReturnSite(dbSession.ExternalSiteName, c).SessionExpirationInSeconds;
                dbSession.ValidUntil = AlarmClock.Now.AddSeconds(expirationTime);
                c.SaveChanges();
            }
            return ConvertDbAuctionToAuctionObject(newDbAuction, ConnectionString, AlarmClock);
        }

        public void Logout() {
            if (!IsValid()) throw new AuctionSiteInvalidOperationException();
            using var c = new ProjectDbContext(ConnectionString);
            var currentSession = c.Sessions.Single(s => s.SessionId == Id);
            c.Remove(currentSession);
            c.SaveChanges();
        }

        public bool IsValid() {
            try {
                using var c = new ProjectDbContext(ConnectionString);
                var validSession = c.Sessions.Single(s => s.SessionId == Id);
                return validSession.ValidUntil > AlarmClock.Now;
            }
            catch (Exception e) {
                if (e is AuctionSiteInvalidOperationException) throw;
                throw new AuctionSiteUnavailableDbException(e.Message);
            }
        }
        public string Id { get; set; }
        public DateTime ValidUntil {
            get {
                try {
                    using var c = new ProjectDbContext(ConnectionString);
                    return c.Sessions.Where(s => s.SessionId == Id).Select(s => s.ValidUntil).Single();
                }
                catch (Exception e) {
                    throw new AuctionSiteArgumentException(@"");
                }
            }
            
        }
        public IUser User { get; }
        public double MinimumBidIncrement { get; }
        public double currentPrice { get; }
        private string ConnectionString { get; }
        private IAlarmClock AlarmClock { get; set; }
        private DateTime Now => AlarmClock.Now;
        public SessionObject(string id, DateTime validUntil, double minimumBidIncrement, IUser user, string connectionString, IAlarmClock alarmClock) {
            Id = id;
            MinimumBidIncrement = minimumBidIncrement;
            User = user;
            ConnectionString = connectionString;
            AlarmClock = alarmClock;
        }
        public SessionObject(string id, string connectionString, IAlarmClock alarmClock) {
            Id = id;
            ConnectionString = connectionString;
            AlarmClock = alarmClock;
        }

    }

    

    public class SiteObject : ISite {
        public IEnumerable<IUser> ToyGetUsers() {
            using var c = new ProjectDbContext(ConnectionString);
            if (!c.Sites.Any(s => s.Name == Name)) throw new AuctionSiteInvalidOperationException(@"Site doesn't exists");
            return c.Users.Where(u => u.ExternalSite.Name == Name).ToList()
                .Select(user => ConvertDbUserToUserObject(user, ConnectionString, AlarmClock)).Cast<IUser>().ToList();
        }


        public IEnumerable<ISession> ToyGetSessions() {
            using var c = new ProjectDbContext(ConnectionString);
            if (!c.Sites.Any(s => s.Name == Name)) throw new AuctionSiteInvalidOperationException(@"Site doesn't exists");

            foreach (var session in c.Sessions.Where(s => s.ExternalSiteName == Name)) {
                if (session.ValidUntil < AlarmClock.Now) { c.Remove(session); }
                else yield return ConvertDbSessionToSessionObject(session, ConnectionString, AlarmClock);
            }
        }

        public IEnumerable<IAuction> ToyGetAuctions(bool onlyNotEnded) {
            using var c = new ProjectDbContext(ConnectionString);
            if (!c.Sites.Any(s => s.Name == Name)) throw new AuctionSiteInvalidOperationException(@"Site doesn't exists");

            return (from session in c.Sessions.Where(s => s.ExternalSite.Name == Name).ToList() 
                from auction in c.Auctions.Where(a => a.ActiveSessionId == session.SessionId).ToList() 
                where (onlyNotEnded && IsValidAuction(auction, Now())) || (!onlyNotEnded) 
                select ConvertDbAuctionToAuctionObject(auction, ConnectionString, AlarmClock)).Cast<IAuction>().ToList();
        }


        public ISession? Login(string username, string password) {
            IsNull(username,password);
            IsEmpty(username,password);
            IsValidPasswordLength(password);
            
            try {
                using var c = new ProjectDbContext(ConnectionString);
                var dbSite = ReturnSite(Name, c);
                var dbUser = c.Users.Single(u => u.Username == username);
                if (dbUser.Password != password) return null;

                var session = c.Sessions.SingleOrDefault(s => s.User.Username == username);
                if (session != default) { 
                    session.ValidUntil = AlarmClock.Now.AddSeconds(SessionExpirationInSeconds);
                    c.SaveChanges();
                    return ConvertDbSessionToSessionObject(session, ConnectionString, AlarmClock);
                }

                var sessionId = dbUser.Username + "_session";
                var dbSession = new DbSession(sessionId, AlarmClock.Now.AddSeconds(SessionExpirationInSeconds), 
                    MinimumBidIncrement,SessionExpirationInSeconds, dbSite, dbUser);
                c.Sessions.Add(dbSession);
                c.SaveChanges();
                var newSession = ConvertDbSessionToSessionObject(dbSession, ConnectionString, AlarmClock);
                return newSession;
            }
            catch (InvalidOperationException) {
                return null;
            }
            
        }

        public void CreateUser(string username, string password) {
            IsNull(username, password);
            IsEmpty(username, password);
            IsValidUsernameLength(username);
            IsValidPasswordLength(password);

            using var c = new ProjectDbContext(ConnectionString);
            var dbSite = ReturnSite(Name, c);
            
            if (c.Users.Any(u => u.Username == username)) throw new AuctionSiteNameAlreadyInUseException("User with same name already exists");

            var dbUser = new DbUser(username, password, dbSite);
            c.Users.Add(dbUser);
            c.SaveChanges();
        }

        public void Delete() {
            using var c = new ProjectDbContext(ConnectionString);
            var siteToRemove = ReturnSite(Name, c);
            c.Sites.Remove(siteToRemove);
            c.SaveChanges();
        }

        public DateTime Now() {
            return AlarmClock.Now;
        }

        public void CleanupSessions() {
            using var c = new ProjectDbContext(ConnectionString);
            foreach (var elem in c.Sites.Where(s => s.Name == Name)
                         .Select(s => s.ExternalSessions).Single()
                         .Where(elem => elem.ValidUntil < AlarmClock.Now))
                c.Sessions.Remove(elem);
            c.SaveChanges();
        }
        public int Timezone {
            get {
                try {
                    using var c = new ProjectDbContext(ConnectionString);
                    return c.Sites.Where(s => s.Name == Name).Select(s => s.Timezone).Single();
                }
                catch (Exception e){
                    throw new AuctionSiteUnavailableDbException(@"");
                }
            }
        }
        public int SessionExpirationInSeconds {
            get {
                try {
                    using var c = new ProjectDbContext(ConnectionString);
                    return c.Sites.Where(s => s.Name == Name).Select(s => s.SessionExpirationInSeconds).Single();
                }
                catch (Exception e) {
                    throw new AuctionSiteUnavailableDbException(@"");
                }
            }
        }

        public double MinimumBidIncrement {
            get {
                try {
                    using var c = new ProjectDbContext(ConnectionString);
                    return c.Sites.Where(s => s.Name == Name).Select(s => s.MinimumBidIncrement).Single();
                }
                catch (Exception e) {
                    throw new AuctionSiteUnavailableDbException(@"");
                }
            }
        }
        public SiteObject(string siteName, string connectionString, IAlarmClock alarmClock) {
            Name = siteName;
            ConnectionString = connectionString;
            AlarmClock = alarmClock;
            var alarmRing = AlarmClock.InstantiateAlarm(5 * 60 * 1000);
            alarmRing.RingingEvent += CleanupSessions;
        }
        public string Name { get; }
        public IAlarmClock AlarmClock { get; set; }
        private string ConnectionString { get; }
    }


    
    public class UserObject : IUser {
        public IEnumerable<IAuction> WonAuctions() {
            using var c = new ProjectDbContext(ConnectionString);
            var auctions = c.Users.Where(u => u.Username == Username).Select(u => u.WonAuctions).Single(); 
            var auctionList = auctions.Select(item => new LogicClasses.AuctionObject(item.AuctionId, ConnectionString, AlarmClock)).Cast<IAuction>().ToList();
            return auctionList;
        }

        public void Delete() {
            using var c = new ProjectDbContext(ConnectionString);
            try {
                var dbUser = c.Users.Single(u => u.Username == Username);
                var dbSession = c.Sessions.SingleOrDefault(s => s.UserUsername == Username);
                if (dbSession != default) c.Sessions.Remove(dbSession);
                c.Users.Remove(dbUser);
                c.SaveChanges();
            }
            catch (InvalidOperationException e) {
                throw new AuctionSiteInvalidOperationException(@"User doesn't exists");
            }
        }
        public override bool Equals(object obj) {
            return obj is UserObject user && Username == user.Username;
        }

        public string Username { get; set; }
        public string Password { get; }
        public string ConnectionString { get; set; }
        private IAlarmClock AlarmClock { get; }
        public UserObject(string username, string password, string connectionString, IAlarmClock alarmClock) {
            Username = username;
            Password = password;
            ConnectionString = connectionString;
            AlarmClock = alarmClock;
        }
    }






}



