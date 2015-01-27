using System;
using SQLite;

namespace BlaChat
{
	[Table("Users")]
	public class User
	{
		[PrimaryKey, Column("_id")]
		public string user { get; set; }

		public string password { get; set; }

		public string server { get; set; }

		public string id { get; set; }

		public User ()
		{
		}
	}
}

