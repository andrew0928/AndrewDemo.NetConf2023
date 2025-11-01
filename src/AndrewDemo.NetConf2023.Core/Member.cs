using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB;

namespace AndrewDemo.NetConf2023.Core
{
    public class Member
    {
        [BsonId(true)]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public string? ShopNotes { get; set; }

        // not implement password in this demo, just for demo
        // any non-empty string is valid
        [Obsolete("請改用 IShopDatabaseContext.Members 配合自訂服務處理登入流程。")]
        public static string Login(string name, string password)
        {
            var member = ShopDatabase.Current.Members.FindOne(m => m.Name == name);
            if (member == null) return null;

            // ignore password
            //if (string.IsNullOrEmpty(password)) return null;

            var token = CreateAccessToken(member);
            MemberLoggedIn?.Invoke(member, EventArgs.Empty);
            return token;
        }

        /// <summary>
        /// 註冊成功會傳回 access token
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [Obsolete("請改用 IShopDatabaseContext.Members 與 Token 儲存機制自行註冊。")]
        public static string Register(string name)
        {
            var existing = ShopDatabase.Current.Members.FindOne(m => m.Name == name);
            if (existing != null)
            {
                return null;
            }

            var member = new Member()
            {
                Name = name,
            };

            ShopDatabase.Current.Members.Insert(member);

            MemberRegistered?.Invoke(member, EventArgs.Empty);
            return CreateAccessToken(member);
        }

        [Obsolete("請改用 Token 儲存機制與 IShopDatabaseContext 直接查詢會員。")]
        public static Member GetCurrentMember(string accessToken)
        {
            var tokenRecord = ShopDatabase.Current.MemberTokens.FindById(accessToken);
            if (tokenRecord == null) return null;
            if (tokenRecord.Expire <= DateTime.Now) return null;

            return ShopDatabase.Current.Members.FindById(tokenRecord.MemberId);
        }

        [Obsolete("請改用 IShopDatabaseContext.Members.Upsert 更新會員資訊。")]
        public static Member SetShopNotes(string accessToken, string notes)
        {
            var tokenRecord = ShopDatabase.Current.MemberTokens.FindById(accessToken);
            if (tokenRecord == null) return null;
            if (tokenRecord.Expire <= DateTime.Now) return null;

            var member = ShopDatabase.Current.Members.FindById(tokenRecord.MemberId);
            if (member == null) return null;

            member.ShopNotes = notes;
            ShopDatabase.Current.Members.Upsert(member);

            return member;
        }

        public static event EventHandler<EventArgs> MemberRegistered;
        public static event EventHandler<EventArgs> MemberLoggedIn;

        //
        private static string CreateAccessToken(Member consumer)
        {
            string token = Guid.NewGuid().ToString("N");
            ShopDatabase.Current.MemberTokens.Upsert(new MemberAccessTokenRecord()
            {
                Token = token,
                Expire = DateTime.MaxValue,
                MemberId = consumer.Id
            });

            return token;
        }
    }
}