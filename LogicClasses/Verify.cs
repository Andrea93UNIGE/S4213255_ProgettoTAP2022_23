using TAP22_23.AuctionSite.Interface;
using Entities;
using static LogicClasses.LogicClasses;
using TAP22_23.AlarmClock.Interface;
using LogicClasses;

namespace TapProjLogic
{
    internal class Verify {
        public static void IsEmpty(params object[] toVerifyObject) {
            if (toVerifyObject.Contains("")) {
                throw new AuctionSiteArgumentException(toVerifyObject + @"cannot be empty");
            }
        }
        public static void IsNull(params object[] toVerifyObject) {
            if (toVerifyObject.Any(elem => null == elem)) {
                throw new AuctionSiteArgumentNullException(toVerifyObject + @"cannot be null");
            }
        }
        public static void IsValidSiteNameLength(string siteName) {
            if (siteName.Length is < DomainConstraints.MinSiteName or > DomainConstraints.MaxSiteName)
                throw new AuctionSiteArgumentException(@"Wrong site name length");
        }
        public static void IsValidPasswordLength(string password) {
            if (password.Length < DomainConstraints.MinUserPassword) throw new AuctionSiteArgumentException(@"Wrong password length");
        }
        public static void IsValidUsernameLength(string username) {
            if (username.Length is < DomainConstraints.MinUserName or > DomainConstraints.MaxUserName) throw new AuctionSiteArgumentException(@"Wrong username length");
        } public static void IsMalformed(params object[] toVerifyObject) {
            if (toVerifyObject.Contains("")) {
                throw new AuctionSiteUnavailableDbException(toVerifyObject + @"cannot be empty");
            }
        }
        public static void SessionExpirationTime(int expirationTime) {
            if (expirationTime < 0) throw new AuctionSiteArgumentOutOfRangeException(@"Session is expired");
        }
        public static void IsCorrectBidIncrement(double bidIncrement) {
            if (bidIncrement < 0) throw new AuctionSiteArgumentOutOfRangeException("MinimumBidIncrement cannot be negative");
        }
        public static void IsCorrectOffer(double offer) {
            if (offer < 0 ) throw new AuctionSiteArgumentOutOfRangeException("Offer cannot be negative");
        }
        public static void IsCorrectTimezone(int timezone) {
            if (timezone is < DomainConstraints.MinTimeZone or > DomainConstraints.MaxTimeZone)
                throw new AuctionSiteArgumentOutOfRangeException(@"Wrong timezone");
        }
        public static void IsStartingPriceCorrect(double startingPrice) {
            if (startingPrice < 0) throw new AuctionSiteArgumentOutOfRangeException(@"Wrong startingPrice. It cannot be null");
        }
        public static void IsCorrectTime(DateTime endsOn, DateTime now) {
            if (now > endsOn) throw new AuctionSiteUnavailableTimeMachineException(@"Wrong time machine set");
        }
        public static bool IsValidAuction(DbAuction auction, DateTime now) {
            return now < auction.EndsOn; }
        public static bool IsValidISession(ISession session, DateTime now) { 
            return now < session.ValidUntil;
        }
        public static bool IsValidSession(DbSession session, DateTime now) {
            return now < session.ValidUntil;
        }
        public static UserObject ConvertDbUserToUserObject(DbUser dbUser, string connectionString, IAlarmClock alarmClock) {
            return (new UserObject(dbUser.Username, dbUser.Password, connectionString, alarmClock));
        }
        public static AuctionObject ConvertDbAuctionToAuctionObject(DbAuction dbAuction, string connectionString, IAlarmClock alarmClock) {
            var seller = ReturnUserString(dbAuction.SellerUsername, connectionString);
            var sellerObj = new UserObject(seller.Username, seller.Password, connectionString, alarmClock);

            return new AuctionObject(dbAuction.AuctionId, sellerObj, dbAuction.Description, dbAuction.EndsOn, dbAuction.MinimumBidIncrement, 
                dbAuction.StartingPrice, connectionString, alarmClock);
        }
        public static SessionObject ConvertDbSessionToSessionObject(DbSession dbSession, string connectionString, IAlarmClock alarmClock) {
            var dbUser = ReturnUserString(dbSession.UserUsername, connectionString);
            return new SessionObject(dbSession.SessionId, dbSession.ValidUntil, dbSession.MinimumBidIncrement, new UserObject(dbUser.Username, 
                    dbUser.Password, connectionString, alarmClock), connectionString, alarmClock);
        }
        public static DbSite ReturnSite(string name, ProjectDbContext context) {
            try {
                return context.Sites.Single(s => s.Name == name);
            }
            catch (InvalidOperationException e) {
                throw new AuctionSiteInvalidOperationException("Site doesn't exists",e);
            }
        }
        public static DbUser ReturnUserContext(string username, ProjectDbContext context) {
            try {
                return context.Users.Single(u => u.Username == username);
            }
            catch (InvalidOperationException e) {
                throw new AuctionSiteInvalidOperationException("User doesn't exists", e);
            }
        }
        public static DbUser ReturnUserString(string username, string connectionString) {
            using var c = new ProjectDbContext(connectionString);
            return ReturnUserContext(username, c);
        }
        public static DbSession ReturnUserFromSession(string username, ProjectDbContext context) {
            try {
                return context.Sessions.Single(s => s.User.Username == username);
            }
            catch (InvalidOperationException e) {
                throw new AuctionSiteInvalidOperationException("Cannot find the user on the db!", e);
            }
        }
        public static DbAuction ReturnAuction(int id, ProjectDbContext context) {
            try {
                return context.Auctions.Single(a => a.AuctionId == id);
            }
            catch (InvalidOperationException e) {
                throw new AuctionSiteInvalidOperationException(@"Auction doesn't exists", e);
            }
        }
    }
}
