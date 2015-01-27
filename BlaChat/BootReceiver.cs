using System;
using Android.Content;

namespace BlaChat
{
	[BroadcastReceiver]
	class BootReceiver : BroadcastReceiver {
		public override void OnReceive(Context ctx, Intent intent) {
			ctx.StartService (new Intent (ctx, typeof(BackgroundService)));
		}
	}
}

