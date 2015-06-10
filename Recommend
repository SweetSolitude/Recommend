using SOBM.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace SOBM.Custom
{
    public class Recommend
    {
        SOBMEntities db = new SOBMEntities();

        //how much history it takes into account.
        public int historySize = 5;

        public void ProcessRecommendations()
        {
            //select distinct list of all userIds in userHistory. 
            List<RecommenderModel> userHistory = new List<RecommenderModel>();
            List<UserHistory> allUserHistory = db.UserHistory.ToList();

            //extract all unique user ids
            List<string> userIds = allUserHistory.GroupBy(e => e.UserId).Select(g => g.FirstOrDefault()).Select(u => u.UserId).ToList();

            //create a list of UserRatings (EntityId & Rate) for each user in userHistory
            foreach (string userId in userIds)
            {
                RecommenderModel rec = new RecommenderModel();
                rec.userId = userId;

                rec.userRating = allUserHistory.Where(u => u.UserId == userId && u.IsActive == true).Select(p => new UserRatings { EntityId = p.EntityId, Interest = p.Interest, LastModifiedDate = p.DateModified, Id = p.Id }).ToList();
                userHistory.Add(rec);
            }
            

            DateTime lastJobProcess = db.Jobs.Find(1).LastComplete;
            foreach (string userId in userIds)
            {
                DateTime lastLoginDate = db.AspNetUsers.Where(x => x.Id == userId).Select(x => x.LastLogin).FirstOrDefault().Value;
                //dont update history if last recommendation process was after last login of user.
                if (lastLoginDate > lastJobProcess)
                {
                    //pass pref list with each id.. clean user history
                    UpdateUserHistory(userHistory.Where(x => x.userId == userId).FirstOrDefault(), userId);
                }
            }

            foreach (string userId in userIds)
            {
                DateTime lastLoginDate = db.AspNetUsers.Where(x => x.Id == userId).Select(x => x.LastLogin).FirstOrDefault().Value;
                //dont update preferences if last recommendation process was after last login of user.
                if (lastLoginDate > lastJobProcess)
                {
                    //pass pref list with each id
                    InactivateOldRecommendations(userId);
                    UpdateRecommendations(userHistory, userId);
                }
            }
        }

        private void InactivateOldRecommendations(string userId)
        {
            List<UserPreferences> oldPrefList = new List<UserPreferences>();
            oldPrefList = db.UserPreferences.Where(x => x.UserId == userId).ToList();
            foreach (UserPreferences oldPref in oldPrefList)
            {
                oldPref.IsActive = false;
                db.Entry(oldPref).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        //inactivate old history
        private void UpdateUserHistory(RecommenderModel userHistory, string userId)
        {
            int countUserHistory = userHistory.userRating.Count();
            if (countUserHistory > historySize)
            {
                //sorts history from oldest to most recent, purpose to remove old records
                userHistory.userRating = userHistory.userRating.OrderBy(x => x.LastModifiedDate);

                //inactivates all history records other than most recent historySize.
                foreach (UserRatings userRating in userHistory.userRating.Take(countUserHistory - historySize))
                {
                    UserHistory newUserHistory = new UserHistory();
                    newUserHistory = db.UserHistory.Find(userRating.Id);
                    newUserHistory.IsActive = false;
                    db.Entry(newUserHistory).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }
        }

        //group similar users, find books not in common and propose suggestions.
        public void UpdateRecommendations(List<RecommenderModel> userHistory, string userId)
        {
            double sim = 0;
            List<EntityValue> total = new List<EntityValue>();
            List<UserSim> simSum = new List<UserSim>();
            List<Ranks> ranks = new List<Ranks>();

            RecommenderModel mainUser = userHistory.Find(x => x.userId == userId);

            //loop through each preference of users
            foreach (RecommenderModel otherUser in userHistory)
            {
                //ensure same user is not having their preference calculated against
                if (otherUser.userId != userId)
                {
                    sim = SimilarityDistance(userHistory, userId, otherUser.userId);
                }

                if (sim > 0)
                {
                    foreach (EntityValue entVal in otherUser.userRating.Select(x => new EntityValue { entityId = x.EntityId, interestValue = x.Interest }))
                    {
                        //check if the main users list contains entity of other user. Enter if false
                        if (!mainUser.userRating.Select(x => x.EntityId).Contains(entVal.entityId))
                        {
                            UpdateEntityValue(entVal, sim, total);

                            UpdateSimSum(entVal, sim, simSum);
                        }

                    }
                }
            }

            foreach (EntityValue entVal in total)
            {
                Ranks userRank = new Ranks();
                userRank.entityId = entVal.entityId;
                userRank.rank = entVal.interestValue / (simSum.Where(x => x.entityId == entVal.entityId).Select(x => x.simValue).FirstOrDefault());
                ranks.Add(userRank);
            }
            SaveRanksToDB(ranks, userId);
        }



        public double SimilarityDistance(List<RecommenderModel> userHistory, string userId, string otherUserId)
        {
            RecommenderModel mainUser = userHistory.Find(u => u.userId == userId);
            RecommenderModel otherUser = userHistory.Find(u => u.userId == otherUserId);
            double sum = 0; //how similar users are
            double mainUserRating = 0;
            double otherUserRating = 0;


            //Find common history
            //List<string> commonHist = (mainUser.userRating.ToList().Intersect(otherUser.userRating.ToList())).ToList();
            List<String> commonHist = mainUser.userRating.Select(x => x.EntityId).ToList().Intersect(otherUser.userRating.Select(x => x.EntityId).ToList()).ToList();

            if (commonHist.Count() == 0)
            {
                return 0;
            }

            foreach (string entity in commonHist)
            {
                mainUserRating = mainUser.userRating.Where(y => y.EntityId == entity).Select(x => x.Interest).FirstOrDefault();
                otherUserRating = otherUser.userRating.Where(y => y.EntityId == entity).Select(x => x.Interest).FirstOrDefault();

                sum += Math.Pow(mainUserRating - otherUserRating, 2);
            }

            return 1 / (1 + Math.Sqrt(sum));
        }

        private void SaveRanksToDB(List<Ranks> ranks, string userId)
        {
            foreach (Ranks eachRank in ranks)
            {
                UserPreferences userPrefs = new UserPreferences();
                userPrefs.UserId = userId;
                userPrefs.EntityId = eachRank.entityId;
                userPrefs.Rank = eachRank.rank;
                userPrefs.IsActive = true;
                userPrefs.DateModified = DateTime.Now;

                db.UserPreferences.Add(userPrefs);
                db.SaveChanges();
            }
        }

        private void UpdateSimSum(EntityValue prefs, double sim, List<UserSim> simSum)
        {
            if (simSum.Find(x => x.entityId == prefs.entityId).entityId == null)
            {
                UserSim userSim = new UserSim();
                userSim.entityId = prefs.entityId;
                userSim.simValue = 0;

                simSum.Add(userSim);
            }

            UserSim userSimValue = new UserSim();
            UserSim oldUseSimValue = simSum.Find(x => x.entityId == prefs.entityId);
            userSimValue = oldUseSimValue;
            userSimValue.simValue += sim;
            simSum.Remove(oldUseSimValue);
            simSum.Add(userSimValue);

        }

        private void UpdateEntityValue(EntityValue prefs, double sim, List<EntityValue> total)
        {
            if (total.Find(x => x.entityId == prefs.entityId).entityId == null)
            {
                EntityValue prefValue = new EntityValue();
                prefValue.entityId = prefs.entityId;
                prefValue.interestValue = 0;

                total.Add(prefValue);
            }

            EntityValue entValue = new EntityValue();
            EntityValue oldEntValue = total.Find(x => x.entityId == prefs.entityId);
            entValue = oldEntValue;
            entValue.interestValue += prefs.interestValue * sim;
            total.Remove(oldEntValue);
            total.Add(entValue);
        }

        //holds id of entity not used by user, with its rating
        struct EntityValue
        {
            public string entityId;
            public double interestValue;
        };

        //holds id of entity not used by user, with its similar users sim
        struct UserSim
        {
            public string entityId;
            public double simValue;
        };

        //holds id of entity not used by user, with its ranking, higher ranking -> higher recommendation
        struct Ranks
        {
            public string entityId;
            public double rank;
        };

    }
}
