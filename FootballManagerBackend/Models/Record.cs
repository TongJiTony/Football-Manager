namespace FootballManagerBackend.Models
{
    public class Record
    {
        public string team_id { get; set; }
        public DateTime transaction_date { get; set; }
        public decimal amount { get; set; }
        public string description { get; set; }
        public int record_id { get; set; } // 记录 ID，插入后从数据库返回
    }


}


