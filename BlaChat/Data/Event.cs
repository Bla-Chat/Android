using SQLite;

namespace BlaChat
{
	[Table("Events")]
	public class Event
	{
		[PrimaryKey, AutoIncrement]
		public int id { get; set; }

		public string type { get; set; }
		public string msg { get; set; }
		public string nick { get; set; }
		public string text { get; set; }

		public Event ()
		{
		}
	}
}

