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

namespace BlaChat
{
	[Activity (Label = "BlaChat", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
		public BackgroundService service;
		private AsyncNetwork network = new AsyncNetwork();
		private DataBaseWrapper db = null;
		private ServiceConnection serviceConnection = null;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			db = new DataBaseWrapper (this.Resources);

			// Check if the application knows a user.
			User user = db.Table<User>().FirstOrDefault ();
			if (user != null && user.user != null) {
				initializeAuthenticated (user);
			} else {
				initializeNotAuthenticated ();
			}
		}

		public void OnUpdateRequested() {
			User user = db.Table<User>().FirstOrDefault ();
			if (user != null && user.user != null) {
				RunOnUiThread (() => ShowChats (user));
			}
		}

		public void OnBind() {
			if (service != null) {
				service.ResetUpdateInterval ();
			}
		}

		public void OnUnBind() {
			if (service != null) {
				service.ResetUpdateInterval ();
			}
		}

		protected async override void OnResume() {
			base.OnResume ();
			User user = db.Table<User>().FirstOrDefault ();
			if (user != null && user.user != null) {
				await network.UpdateChats (db, user);
				ShowChats (user);
			}

			OnBind ();
		}

		protected override void OnDestroy ()
		{
			base.OnDestroy ();

			if (service != null) {
				UnbindService (serviceConnection);
				OnUnBind ();
				service = null;
			}
		}

		protected override void OnPause ()
		{
			base.OnPause ();
			OnUnBind ();
		}

		private void initializeAuthenticated(User user) {
			StartService (new Intent (this, typeof(BackgroundService)));

			if (service == null) {
				var sericeIntent = new Intent (this, typeof(BackgroundService));
				serviceConnection = new ServiceConnection (this);
				BindService (sericeIntent, serviceConnection, Bind.AutoCreate);
			}

			SetContentView (Resource.Layout.Main);
			ShowChats (user);
		}

		private void ShowChats(User user) {
			TextView placeholder = FindViewById<TextView> (Resource.Id.placeholder);
			var x = db.Table<Chat> ();
			if (x.Count() > 0) {
				placeholder.Visibility = ViewStates.Gone;
			} else {
				placeholder.Visibility = ViewStates.Visible;
				placeholder.Text = "No chats found.";
			}
			LinearLayout chatList = FindViewById<LinearLayout> (Resource.Id.chatList);
			chatList.RemoveAllViews ();
			foreach (var elem in x.OrderByDescending(e => e.time)) {
				View v = LayoutInflater.Inflate (Resource.Layout.Chat, null);
				TextView name = v.FindViewById<TextView>(Resource.Id.chatName);
				TextView message = v.FindViewById<TextView>(Resource.Id.chatMessage);
				ImageView image = v.FindViewById<ImageView> (Resource.Id.chatImage);

				new Thread (async () => {
					try {
						string conv = elem.conversation;
						if (conv.Split(',').Length == 2) {
							if (conv.Split(',')[0] == user.user) {
								conv = conv.Split(',')[1];
							} else {
								conv = conv.Split(',')[0];
							}
						}
						var imageBitmap = await network.GetImageBitmapFromUrl(Resources.GetString(Resource.String.profileUrl) + conv + ".png");
						RunOnUiThread(() => image.SetImageBitmap(imageBitmap));
					} catch (Exception e) {
						Log.Error("ChatMessageImage", e.StackTrace);
					}
				}).Start();

				name.Text = elem.name;
				var tmp = db.Table<Message> ().Where (q => q.conversation == elem.conversation).OrderByDescending (q => q.time);
				var lastMsg = tmp.FirstOrDefault ();
				if (lastMsg != null) {
					if (lastMsg.nick != user.user) {
						message.Text = "> " + lastMsg.text;
					} else {
						message.Text = lastMsg.text;
					}
				} else {
					message.Text = "No message";
				}

				v.Clickable = true;
				v.Click += delegate {
					var intent = new Intent (this, typeof(ChatActivity));
					intent.PutExtra ("conversation", elem.conversation);
					StartActivity (intent);
				};
				chatList.AddView(v);
			}
			chatList.Post(() => chatList.RequestLayout ());
		}

		private void initializeNotAuthenticated() {
			SetContentView (Resource.Layout.Authentication);

			Button login = FindViewById<Button> (Resource.Id.login);
			login.Click += async delegate {
				login.Text = "Connecting...";
				login.Enabled = false;

				var user = new User() {
					user = FindViewById<EditText> (Resource.Id.username).Text,
					password = FindViewById<EditText> (Resource.Id.password).Text,
					server = FindViewById<EditText> (Resource.Id.server).Text != null
						&& FindViewById<EditText> (Resource.Id.server).Text != "" ?
						FindViewById<EditText> (Resource.Id.server).Text :
						FindViewById<EditText> (Resource.Id.server).Hint
				};
				if (user.user == "" || user.password == "") {
					login.Text = "Login";
					login.Enabled = true;
					return;
				}

				if(await network.Authenticate(db, user)) {
					db.Insert(user);
					initializeAuthenticated(user);
				} else {
					login.Text = "Retry Login";
					login.Enabled = true;
				}
			};
		}
	}
}


