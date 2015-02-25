using System;
using Android.App;
using Android.Util;
using System.Threading;
using Android.Content;
using Android.OS;
using System.Threading.Tasks;
using Android.Support.V4.App;
using Android.Media;


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

		public BackgroundService ()
		{
			ResetUpdateInterval ();
		}

		public override IBinder OnBind(Intent Intent) {
			return new ServiceBinder(this);
		}

		public void ResetUpdateInterval() {
			UpdateInterval = 200;
		}

		public override StartCommandResult OnStartCommand (Android.Content.Intent intent, StartCommandFlags flags, int startId)
		{
			Log.Debug ("BackgroundService", "BackgroundService started");
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

			if (user != null && user.user != null) {
				var t = new Thread (async () => {
					while (true) {
						await network.UpdateEvents (db, user);
						Thread.Sleep (UpdateInterval * Mode);

						if (UpdateInterval < 2000) {
							UpdateInterval += 100;
						} else if (UpdateInterval < 10000) {
							UpdateInterval += 500;
						} else if (UpdateInterval < 30000) {
							UpdateInterval += 1000;
						} else if (UpdateInterval < 60000) {
							UpdateInterval += 6000;
						} else if (UpdateInterval < 120000) {
							UpdateInterval += 10000;
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
			if (e.type == "onMessage") {
				await network.UpdateChats (db, user);
				var chat = db.Get<Chat> (e.msg);
				await network.UpdateHistory (db, user, chat, 10);
			}
			if (e.msg != ActiveConversation) {
				if (user.user != e.nick) {
					await Notify (network, e.nick, e.text);
				}
			}
			if (MainActivity != null) {
				MainActivity.OnUpdateRequested ();
			}
			if (ChatActivity != null) {
				ChatActivity.OnUpdateRequested ();
			}
			ResetUpdateInterval ();
			return 0;
		}

		private async Task<int> Notify(AsyncNetwork network, string title, string message) {
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
				.SetSound (RingtoneManager.GetDefaultUri (RingtoneType.Notification))
				.SetSmallIcon (Resource.Drawable.Icon);
				

			if ((int)Android.OS.Build.VERSION.SdkInt >= 14) {
				builder.SetLights(Android.Graphics.Color.Magenta , 500, 500);
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

			Vibrator v = (Vibrator) GetSystemService(Context.VibratorService); // Make phone vibrate
			v.Vibrate(300);

			return notificationId;
		}
	}
}

