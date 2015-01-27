
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Views.InputMethods;
using System.Threading.Tasks;
using System.Threading;

namespace BlaChat
{
	[Activity (Label = "BlaChat", Icon = "@drawable/icon")]	
	public class ChatActivity : Activity
	{
		private AsyncNetwork network = new AsyncNetwork();
		private DataBaseWrapper db = null;
		private string conversation;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			db = new DataBaseWrapper (this.Resources);

			SetContentView (Resource.Layout.ChatActivity);

			conversation = Intent.GetStringExtra ("conversation");

			var chat = db.Get<Chat> (conversation);
			Title = "BlaChat " + chat.name;

			User user = db.Table<User>().FirstOrDefault ();
			if (user != null && user.user != null) {
				UpdateMessages (user);
			}

			Button send = FindViewById<Button> (Resource.Id.send);
			send.Click += async delegate {
				EditText message = FindViewById<EditText> (Resource.Id.message);
				var msg = message.Text;
				message.Text = "";

				await network.SendMessage (db, user, chat, msg);
				await network.UpdateHistory (db, user, chat, 30);
				UpdateMessages (user);
			};
		}

		protected async override void OnResume() {
			base.OnResume ();

			TextView message = FindViewById<TextView> (Resource.Id.message);
			InputMethodManager manager = (InputMethodManager) GetSystemService(InputMethodService);
			manager.HideSoftInputFromWindow(message.WindowToken, 0);

			User user = db.Table<User>().FirstOrDefault ();
			if (user != null && user.user != null) {
				var chat = db.Get<Chat> (conversation);
				await network.UpdateHistory (db, user, chat, 30);
				UpdateMessages (user);
			}

			manager.HideSoftInputFromWindow(message.WindowToken, 0);
		}

		private void UpdateMessages(User user) {
			LinearLayout messageList = FindViewById<LinearLayout> (Resource.Id.messageLayout);
			messageList.RemoveAllViews ();
			var x = db.Table<Message> ();
			foreach (var elem in x.Where(q => q.conversation == conversation).OrderBy(e => e.time)) {
				View v = null;
				if (elem.nick == user.user) {
					v = LayoutInflater.Inflate (Resource.Layout.MessageRight, null);
				} else {
					v = LayoutInflater.Inflate (Resource.Layout.MessageLeft, null);
				}
				TextView text = v.FindViewById<TextView>(Resource.Id.messageText);
				text.Text = elem.text;
				messageList.AddView(v);
			}

			ScrollView scrollView = FindViewById<ScrollView> (Resource.Id.messageScrollView);
			scrollView.Post(() => scrollView.FullScroll(FocusSearchDirection.Down));
		}
	}
}

