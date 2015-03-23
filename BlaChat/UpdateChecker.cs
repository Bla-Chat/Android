using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Threading;
using System.Threading.Tasks;
using Android.Util;
using System.Collections.Generic;
using Android.Views.InputMethods;
using Android.Net;
using System.Net.Http;

namespace BlaChat
{
	public class UpdateChecker
	{
		public UpdateChecker(Context ctx, DataBaseWrapper db, Setting setting) {
			if (string.IsNullOrEmpty (setting.NewestVersion)) {
				setting.NewestVersion = Setting.CurrentVersion;
			}
			var connectivityManager = (ConnectivityManager)ctx.GetSystemService(Context.ConnectivityService);
			if (setting.Synchronisation != Setting.Frequency.wlan || connectivityManager.GetNetworkInfo(ConnectivityType.Wifi).GetState() == NetworkInfo.State.Connected) {
				new Thread (async () => {
					string contentsTask;
					try {
						using(var httpClient = new HttpClient()) {
							contentsTask = await httpClient.GetStringAsync ("https://raw.githubusercontent.com/Bla-Chat/Android/master/version.txt");
						}
					} catch (Exception e) {
						Log.Error ("BlaChat", e.StackTrace);
						contentsTask = null;
					} finally {
						//semaphore.Release ();
					}
					if (contentsTask != null) {
						setting.NewestVersion = contentsTask;
						db.Update(setting);
					}
				}).Start ();
			}
		}
	}

}


