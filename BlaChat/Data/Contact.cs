using SQLite;

namespace BlaChat
{
	[Table("Contacts")]
	public class Contact
	{
		[PrimaryKey]
		public string nick { get; set; }

		public string name { get; set; }
		public string status { get; set; }

		public Contact ()
		{
		}
	}
}

