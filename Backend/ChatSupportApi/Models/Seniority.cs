namespace ChatSupportApi.Models
{
    public enum Seniority
    {
        Junior,
        MidLevel,
        Senior,
        TeamLead
    }

    public static class SeniorityExtensions
    {
        public static double GetMultiplier(this Seniority seniority)
        {
            return seniority switch
            {
                Seniority.Junior => 0.4,
                Seniority.MidLevel => 0.6,
                Seniority.Senior => 0.8,
                Seniority.TeamLead => 0.5,
                _ => 0.4
            };
        }

        public static int GetPriority(this Seniority seniority)
        {
            return seniority switch
            {
                Seniority.Junior => 1,
                Seniority.MidLevel => 2,
                Seniority.Senior => 3,
                Seniority.TeamLead => 4,
                _ => 1
            };
        }
    }
}
