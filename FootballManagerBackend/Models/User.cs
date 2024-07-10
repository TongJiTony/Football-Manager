namespace FootballManagerBackend.Models
{
    public class User
    {
        private int userId;
        private string userName;
        private string userRight;
        private string userPassword;
        private long userPhone;
        private string icon;

        public int UserId
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

        public long UserPhone
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


}
