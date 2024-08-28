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

        private enum Standard
        {
            //系统最低转会计划判断标准，低于此标准的转会计划一定拒绝，其余的通过函数计算和随机判断
            base_transfer_fee = 100000, //转会费最少100000
            base_salary = 300000, //年薪最少300000
            base_contract_length = 2 //合同最少2年
        }

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

        public string[] JudgePlan(JsonElement plan, int playerRank, string position, int age)
        {
            //根据agent的判断标准，自动判断转会计划和合同计划是否同意
            return ["ok", "no reason"];
        }
    }
}
