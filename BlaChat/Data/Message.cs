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

		public override bool Equals (object obj)
		{
			if (obj.GetType() == typeof(Message)) {
				Message other = (Message)obj;
				return conversation == other.conversation && author == other.author && nick == other.nick && time == other.time && text == other.text;
			} else {
				return false;
			}
		}

		public override int GetHashCode ()
		{
			return conversation.GetHashCode()  + author.GetHashCode() + nick.GetHashCode() + time.GetHashCode() + text.GetHashCode();
		}
	}
}

