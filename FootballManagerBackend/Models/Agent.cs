using System.Security.Cryptography;
using System.Text.Json;

namespace FootballManagerBackend.Models
{
    public class Agent
    {
        private static Agent? _instance;
        private static readonly object _lock = new object();

        public string? Connection_status { get; set; } //ready等待连接, connected已被连接但无方案, committed已提交方案但未保存
        public int? Connection_user { get; set; } //null无用户, 其他值为用户ID
        public Dictionary<string, object?>? Trans_plan { get; set; } //预存转会计划
        public Dictionary<string, object?>? Cont_plan { get; set; } //预存合同计划

        private readonly Dictionary<string, int> Standard = new Dictionary<string, int> {
            //系统最低转会计划判断标准，低于此标准的转会计划一定拒绝，其余的通过函数计算和随机判断
            { "base_transfer_fee", 100000 }, //转会费最少100000
            { "base_salary", 250000 }, //年薪最少250000
            { "base_contract_length", 8 } //合同最长8年（最短2年)
        };

        private Agent()
        {
            Connection_status = "ready";
            Connection_user = null;
            Trans_plan = null;
            Cont_plan = null;
        }

        // 获取单例实例
        public static Agent Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new Agent();
                    }
                }
                return _instance;
            }
        }

        // 更新连接状态
        public void UpdateConnectionStatus(string? status)
        {
            Connection_status = status;
        }

        // 更新操作用户
        public void UpdateOperatingUser(int? user)
        {
            Connection_user = user;
        }

        // 更新转会计划
        public void UpdateTransferPlan(Dictionary<string, object?>? plan)
        {
            Trans_plan = plan;
        }

        // 更新合同计划
        public void UpdateContractPlan(Dictionary<string, object?>? plan)
        {
            Cont_plan = plan;
        }

        public string[] JudgePlan(JsonElement plan, int playerRank, string? position, int age)
        {
            //根据agent的判断标准，自动判断转会计划和合同计划是否同意
            Random rand = new();
            string reason = "";
            DateTime start = new();
            DateTime end = new();
            int salary = 0;
            int transfee = 0;
            foreach (var item in plan.EnumerateObject())
            {
                switch (item.Name.ToLower())
                {
                    case "start_date":
                        start = DateTime.Parse(item.Value.GetString());
                        break;
                    case "end_date":
                        end = DateTime.Parse(item.Value.GetString());
                        break;
                    case "salary":
                        salary = item.Value.GetInt32();
                        break;
                    case "transfer_fees":
                        transfee = item.Value.GetInt32();
                        break;
                
                }
            }
            TimeSpan diff = end - start;
            int length = diff.Days / 365;

            int new_base_transfee = Standard["base_transfer_fee"];
            new_base_transfee += int.Max(-1500, (int)((playerRank - 85) * 4000 * (0.6 + rand.NextDouble()) + (25 - age) * 500 + rand.Next(0, 20000) + length * 7000 * (0.3 + rand.NextDouble())));
            switch (position)
            {
                case "守门员":
                    new_base_transfee += (int)(10000 * rand.NextDouble());
                    break;
                case "前锋":
                    new_base_transfee += (int)(30000 * rand.NextDouble());
                    break;
                case "中场":
                    new_base_transfee += (int)(20000 * rand.NextDouble());
                    break;
                case "后卫":
                    new_base_transfee += (int)(20000 * rand.NextDouble());
                    break;
            }

            int new_base_salary = Standard["base_salary"];
            new_base_salary += int.Max(600000, int.Max(0, (int)((playerRank - 75) * 5000 * (0.6 + rand.NextDouble()) + (25 - age) * 2000 + rand.Next(0, 20000) + length * 1000 * (0.3 + rand.NextDouble()))));
            switch (position)
            {
                case "守门员":
                    new_base_salary += (int)(50000 * rand.NextDouble());
                    break;
                case "前锋":
                    new_base_salary += (int)(100000 * rand.NextDouble());
                    break;
                case "中场":
                    new_base_salary += (int)(80000 * rand.NextDouble());
                    break;
                case "后卫":
                    new_base_salary += (int)(80000 * rand.NextDouble());
                    break;
            }
            int new_base_length = Standard["base_contract_length"];
            new_base_length += rand.Next(-4, 1);

            if (transfee < new_base_transfee)
            {
                reason += "转会费过低，经纪人希望至少为 " + new_base_transfee + " 元；";
            }
            if (salary < new_base_salary)
            {
                reason += "球员薪水过低，经纪人希望至少为 " + new_base_salary + " 元；";
            }
            if (length > new_base_length)
            {
                reason += "合同时限过长，经纪人希望不超过 " + new_base_length + " 年；";
            }
            if (length < 2)
            {
                reason += "合同时限过短，经纪人希望至少为 2 年；";
            }

            if (reason == "")
            {
                return ["ok", reason ];
            }
            else
            {
                return ["no", reason.Substring(0, reason.Length - 1)];
            }
        }
    }
}
