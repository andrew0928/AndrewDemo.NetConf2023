namespace AndrewDemo.NetConf2023.Core
{
    public class Member
    {
        public int Id { get; set; }
        public string Name { get; set; }

        // not implement password in this demo, just for demo
        // any non-empty string is valid
        public static string Login(string name, string password)
        {
            var m = _database.Where(x => x.Value.Name == name).Select(x => x.Value).FirstOrDefault();
            if (m == null) return null;

            // ignore password
            //if (string.IsNullOrEmpty(password)) return null;

            return CreateAccessToken(m);
        }

        /// <summary>
        /// 註冊成功會傳回 access token
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string Register(string name)
        {
            if ((from x in _database where x.Value.Name == name select x.Value).Any())
            {
                return null;
            }

            var m = new Member()
            {
                Id = _current_number++,
                Name = name
            };

            MemberRegistered?.Invoke(m, new EventArgs() { });

            _database.Add(m.Id, m);
            return CreateAccessToken(m);
        }

        public static Member GetCurrentMember(string accessToken)
        {
            // access token validation
            if (AccessTokens.ContainsKey(accessToken))
            {
                var (expire, consumer) = AccessTokens[accessToken];
                if (expire > DateTime.Now)
                {
                    return consumer;
                }
            }

            return null;
        }

        public static event EventHandler<EventArgs> MemberRegistered;
        public static event EventHandler<EventArgs> MemberLoggedIn;

        private static int _current_number = 1;
        private static Dictionary<int, Member> _database = new Dictionary<int, Member>()
        {
            //{ 1, new Member() { Id = 1, Name = "andrew" } },
            //{ 2, new Member() { Id = 2, Name = "poy"} }
        };

        //[Obsolete("member: cross model data access!")]
        private static Dictionary<int, Member> Database { get { return _database; } }

        //
        private static Dictionary<string, (DateTime expire, Member consumer)> AccessTokens = new Dictionary<string, (DateTime expire, Member consumer)>();

        private static string CreateAccessToken(Member consumer)
        {
            string token = Guid.NewGuid().ToString("N");
            AccessTokens.Add(token, (DateTime.MaxValue, consumer));

            return token;
        }
    }
}