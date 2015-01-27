using System;
using Android.App;
using Android.Util;
using System.Threading;
using Android.Content;
using Android.OS;
using System.Threading.Tasks;


namespace BlaChat
{
	[Service]
	public class BackgroundService : Service
	{

		private int UpdateInterval = 10000;
		public BackgroundService ()
		{
		}

		public override IBinder OnBind(Intent Intent) {
			return null;
		}

		public override StartCommandResult OnStartCommand (Android.Content.Intent intent, StartCommandFlags flags, int startId)
		{
			Log.Debug ("BackgroundService", "BackgroundService started");
			DoWork ();
			return StartCommandResult.Sticky;
		}

		public void DoWork ()
		{
			AsyncNetwork network = new AsyncNetwork ();
			DataBaseWrapper db = new DataBaseWrapper (this.Resources);

			User user = db.Table<User>().FirstOrDefault ();
			if (user != null && user.user != null) {
				var t = new Thread (async () => {
					while (true) {
						await network.UpdateEvents (db, user);
						Thread.Sleep (UpdateInterval);
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
				var t2 = new Thread (async () => {
					while (true) {
						var events = db.Table<Event>();
						foreach(var x in events) {
							await HandleEvent(db, network, user, x);
							db.Delete<Event>(x.id);
						}
						// Cap update at min once per 10s.
						int d = UpdateInterval / 10;
						Thread.Sleep (d < 10000 ? d : 10000);
					}
				}
				);
				t2.Start ();
			} else {
				this.StopSelf ();
			}
		}

		private async Task<int> HandleEvent(DataBaseWrapper db, AsyncNetwork network, User user, Event e) {
			if (e.type == "onMessage") {
				await network.UpdateChats (db, user);
				var chat = db.Get<Chat> (e.msg);
				await network.UpdateHistory(db, user, chat, 60);
			}
			Notify (e.nick, e.text);
			UpdateInterval = 200;
			return 0;
		}

		private void Notify(string title, string message) {
			var nMgr = (NotificationManager)GetSystemService (NotificationService);
			var notification = new Notification (Resource.Drawable.Icon, message);
			var pendingIntent = PendingIntent.GetActivity (this, 0, new Intent (this, typeof(BackgroundService)), 0);
			notification.SetLatestEventInfo (this, title, message, pendingIntent);
			nMgr.Notify (0, notification);
		}
	}
}

