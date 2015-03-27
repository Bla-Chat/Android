using System;
using Android.App;
using Android.Util;
using System.Threading;
using Android.Content;
using Android.OS;
using System.Threading.Tasks;
using Android.Support.V4.App;
using Android.Media;
using Android.Net;


namespace BlaChat
{
	[Service]
	public class BackgroundService : Service
	{
		public string ActiveConversation { set; get; }
		public MainActivity MainActivity { set; get; }
		public ChatActivity ChatActivity { set; get; }
		public int Mode { set; get; }

		private DataBaseWrapper db = null;
		private AsyncNetwork network = null;
		private User user = null;
		private int UpdateInterval = 10000;
		private int connectivityMode = 1;

		public BackgroundService ()
		{
			ResetUpdateInterval ();
		}

		public override IBinder OnBind(Intent Intent) {
			return new ServiceBinder(this);
		}

		public void ResetUpdateInterval() {
			UpdateInterval = 1000;
		}

		public override StartCommandResult OnStartCommand (Intent intent, StartCommandFlags flags, int startId)
		{
			Log.Debug ("BlaChat", "BackgroundService started");
			ResetUpdateInterval ();
			DoWork ();
			return StartCommandResult.Sticky;
		}

		public void DoWork ()
		{
			db = new DataBaseWrapper (Resources);
			network = new AsyncNetwork ();
			network.SetBackgroundService (this);
			user = db.Table<User>().FirstOrDefault ();
			var connectivityManager = (ConnectivityManager)GetSystemService(ConnectivityService);

			if (user != null && user.user != null) {
				var t = new Thread (async () => {
					while (true) {
						Setting.Frequency f = db.Table<Setting>().FirstOrDefault().Synchronisation;
						if (f != Setting.Frequency.wlan || connectivityManager.GetNetworkInfo(ConnectivityType.Wifi).GetState() == NetworkInfo.State.Connected) {
							while(!await network.UpdateEvents (db, user)) {
								await network.Authenticate(db, user);
							}
						}

						// Wifi connection gets normal updates, other networks get 4 times worse update time.
						if (connectivityManager.GetNetworkInfo(ConnectivityType.Wifi).GetState() == NetworkInfo.State.Connected)
						{
							connectivityMode = 1;
						} else {
							connectivityMode = 4;
						}

						switch(f) {
						case Setting.Frequency.often:
							Mode = 1;
							break;
						case Setting.Frequency.normal:
							Mode = 2;
							break;
						case Setting.Frequency.rare:
							Mode = 4;
							break;
						case Setting.Frequency.wlan:
							Mode = 2;
							break;
						}

						Thread.Sleep (UpdateInterval * Mode* connectivityMode);

						if (UpdateInterval < 12000) {
							UpdateInterval += 1000;
						} else if (UpdateInterval < 30000) {
							UpdateInterval += 2000;
						} else {
							UpdateInterval = 120000;
						}
					}
				}
				);
				t.Start ();
			} else {
				StopSelf ();
			}
		}

		public async Task<int> UpdateNotifications() {
			if (db != null && network != null && user != null) {
				var events = db.Table<Event> ();
				foreach (var x in events) {
					await HandleEvent (db, network, user, x);
					db.Delete<Event> (x.id);
				}
			}
			return 0;
		}

		private async Task<int> HandleEvent(DataBaseWrapper db, AsyncNetwork network, User user, Event e) {
			if (e.type != "onMessage") {
				return 0;
			}
			var chat = db.Get<Chat> (e.msg);
			if (chat == null) {
				while(!await network.UpdateChats (db, user)) {
					await network.Authenticate(db, user);
				}
				chat = db.Get<Chat> (e.msg);
			}
			chat.time = e.time;
			db.Update (chat);

			var msg = new Message ();
			msg.conversation = chat.conversation;
			msg.author = e.author;
			msg.nick = e.nick;
			msg.text = e.text;
			msg.time = e.time;
			db.InsertIfNotContains<Message> (msg);

			ResetUpdateInterval ();
			if (e.msg != ActiveConversation) {
				if (user.user != e.nick) {
					await Notify (network, e.nick, e.text);
				}
			}
			if (ChatActivity != null) {
				ChatActivity.OnUpdateRequested ();
			} else if (MainActivity != null) {
				MainActivity.OnUpdateRequested ();
			}
			return 0;
		}

		private async Task<int> Notify(AsyncNetwork network, string title, string message) {
			Setting setting = db.Table<Setting> ().FirstOrDefault ();

			if (!setting.Notifications)
				return 0;

			// Set up an intent so that tapping the notifications returns to this app:
			Intent intent = new Intent (this, typeof(MainActivity));

			// Create a PendingIntent; we're only using one PendingIntent (ID = 0):
			const int pendingIntentId = 0;
			PendingIntent pendingIntent = 
				PendingIntent.GetActivity (this, pendingIntentId, intent, PendingIntentFlags.OneShot);
			var msg = message;
			if (msg.StartsWith ("#image")) {
				msg = "You received an image.";
			}
			NotificationCompat.Builder builder = new NotificationCompat.Builder (this)
				.SetContentIntent (pendingIntent)
				.SetContentTitle (title)
				.SetContentText (msg)
				.SetAutoCancel (true)
				.SetSmallIcon (Resource.Drawable.Icon);

			if (setting.Sound) {
				builder.SetSound (RingtoneManager.GetDefaultUri (RingtoneType.Notification));
			}
				

			if ((int)Android.OS.Build.VERSION.SdkInt >= 14) {
				if (setting.Led) {
					builder.SetLights (Android.Graphics.Color.Magenta, 500, 500);
				}
				if (message.StartsWith ("#image")) {
					// Instantiate the Image (Big Picture) style:
					NotificationCompat.BigPictureStyle picStyle = new NotificationCompat.BigPictureStyle ();

					// Convert the image to a bitmap before passing it into the style:
					picStyle.BigPicture (await network.GetImageBitmapFromUrlNoCache (message.Substring ("#image ".Length)));

					// Set the summary text that will appear with the image:
					picStyle.SetSummaryText (msg);

					// Plug this style into the builder:
					builder.SetStyle (picStyle);
				} else {
					NotificationCompat.BigTextStyle textStyle = new NotificationCompat.BigTextStyle ();

					// Fill it with text:
					textStyle.BigText (message);

					// Set the summary text:
					textStyle.SetSummaryText ("New message");
					builder.SetStyle (textStyle);
				}
			}

			// Build the notification:
			Notification notification = builder.Build();

			// Get the notification manager:
			NotificationManager notificationManager =
				GetSystemService (Context.NotificationService) as NotificationManager;

			// Publish the notification:
			const int notificationId = 0;
			notificationManager.Notify (notificationId, notification);

			if (setting.Vibrate) {
				Vibrator v = (Vibrator)GetSystemService (Context.VibratorService); // Make phone vibrate
				v.Vibrate (300);
			}

			return notificationId;
		}
	}
}

