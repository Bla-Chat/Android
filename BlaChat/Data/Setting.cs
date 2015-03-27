using System;
using SQLite;

namespace BlaChat
{
	[Table("Settings")]
	public class Setting
	{
		public const string CurrentVersion = "2.9";

		public enum Size { large, medium, small };
		public enum Frequency { often, normal, rare, wlan }

		[PrimaryKey, AutoIncrement]
		public int id { get; set; }

		public int Theme { get; set; }

		public Size FontSize { get; set; }

		public bool Notifications { get; set; } 
		public bool Vibrate { get; set; } 
		public bool Sound { get; set; } 
		public bool Led { get; set; } 

		public Frequency Synchronisation { get; set; }

		public string NewestVersion { get; set; }

		public Setting ()
		{
			Theme = Resource.Style.LightHolo;
			FontSize = Size.medium;
			Notifications = true;
			Vibrate = true;
			Sound = true;
			Led = true;
			Synchronisation = Frequency.normal;
			NewestVersion = CurrentVersion;
		}
	}
}

