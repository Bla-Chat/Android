using SQLite;
using System;

namespace BlaChat
{
	[Table("Messages")]
	public class Message : IEqualsExpression<Message>
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

		public System.Linq.Expressions.Expression<Func<Message, bool>> EqualsExpression ()
		{
			return other => (conversation == other.conversation && nick == other.nick && time == other.time && text == other.text);
		}

		public override int GetHashCode ()
		{
			return conversation.GetHashCode()  + author.GetHashCode() + nick.GetHashCode() + time.GetHashCode() + text.GetHashCode();
		}
	}
}

