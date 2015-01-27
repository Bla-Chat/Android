using System;
using SQLite;

namespace BlaChat
{
	[Table("Messages")]
	public class Message
	{
		[PrimaryKey, AutoIncrement]
		public int id { get; set; }

		public string conversation { get; set; }

		public string author { get; set; }
		public string nick { get; set; }
		public string time { get; set; }
		public string text { get; set; }

		public Message ()
		{
		}
	}
}

