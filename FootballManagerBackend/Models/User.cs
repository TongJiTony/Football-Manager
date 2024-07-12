namespace FootballManagerBackend.Models
{
    public class User
    {
        private long userId;
        private string userName;
        private string userRight;
        private string userPassword;
        private string userPhone;
        private string icon;

        public long UserId
        {
            get { return userId; }
            set { userId = value; }
        }

        public string UserName
        {
            get { return userName; }
            set { userName = value; }
        }

        public string UserRight
        {
            get { return userRight; }
            set { userRight = value; }
        }

        public string UserPassword
        {
            get { return userPassword; }
            set { userPassword = value; }
        }

        public string UserPhone
        {
            get { return userPhone; }
            set { userPhone = value; }
        }

        public string Icon
        {
            get { return icon; }
            set { icon = value; }
        }
    }

    public class LoginRequest
    {
        public long user_id { get; set; }
        public string user_password { get; set; }
    }

    public class ChangePasswordRequest
    {
        public long user_id { get; set; }
        public string user_password { get; set; }
        public string new_password {  get; set; }
    }

    public class JwtConfig
    {
        public string Key { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        // 其他可能的JWT配置属性
    }

    public class ChangeAttriRequest
    {
        public long user_id { get; set; }  // 先接收为字符串
        public string new_name { get; set; }
        public string new_phone { get; set; }
        public string new_icon { get; set; }
    }

    public class DeleteRequest
    {
        public long user_id { get; set; }
        public string user_password { get; set; }
    }

}
