namespace FootballManagerBackend.Models
{
    public class Record
    {
        public long record_id { get; set; }
        public string team_id { get; set; }
        public DateTime transaction_date { get; set; }
        public string amount { get; set; }
        public string description { get; set; }

    }


}


