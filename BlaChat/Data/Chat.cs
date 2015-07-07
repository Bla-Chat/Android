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

		public bool Marked { get; set; }
		public bool Notifications{ get; set; }
		public bool ReadMessagesEnabled { get; set; }
		public int VisibleMessages { get; set; }

		public Chat ()
		{
			Marked = false;
			Notifications = true;
			ReadMessagesEnabled = true;
			VisibleMessages = 0;
		}
	}
}

