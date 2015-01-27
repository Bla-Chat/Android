using System;
using SQLite;

namespace BlaChat
{
	[Table("Chats")]
	public class Chat
	{
		[PrimaryKey]
		public string conversation { get; set; }

		public string name { get; set; }
		public string time { get; set; }


		public Chat ()
		{
		}
	}
}

