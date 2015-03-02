using Android.Content;
using Android.OS;

namespace BlaChat
{
	public class ServiceBinder : Binder
	{
		BackgroundService service;

		public ServiceBinder (BackgroundService service)
		{
			this.service = service;
		}

		public BackgroundService GetService ()
		{
			return service;
		}
	}

	class ServiceConnection : Java.Lang.Object, IServiceConnection
	{
		MainActivity activityA;
		ChatActivity activityB;

		public ServiceConnection (MainActivity activity)
		{
			this.activityA = activity;
		}

		public ServiceConnection (ChatActivity activity)
		{
			this.activityB = activity;
		}

		public void OnServiceConnected (ComponentName name, IBinder service)
		{
			var demoServiceBinder = service as ServiceBinder;
			if (demoServiceBinder != null) {
				if (activityA != null) {
					activityA.service = demoServiceBinder.GetService();
					activityA.OnBind ();
					activityA.service.MainActivity = activityA;
				} else {
					activityB.service = demoServiceBinder.GetService();
					activityB.OnBind ();
					activityB.service.ChatActivity = activityB;
				}
			}
		}

		public void OnServiceDisconnected (ComponentName name)
		{
			if (activityA != null) {
				activityA.OnUnBind ();
				activityA.service.MainActivity = null;
			} else {
				activityB.OnUnBind ();
				activityB.service.ChatActivity = null;
			}
			activityA.service = null;
			activityB.service = null;
		}
	}
}