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
using Android.Text;
using Android.Graphics;
using System.Runtime.CompilerServices;

namespace BlaChat
{
	[Activity (Label = "BlaChat", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
		public BackgroundService service;
		private AsyncNetwork network = new AsyncNetwork();
		private DataBaseWrapper db = null;
		private ServiceConnection serviceConnection = null;
		Setting setting = null;
		int StartupTheme;

		protected override void OnCreate (Bundle bundle)
		{
			db = new DataBaseWrapper (this.Resources);
			if ((setting = db.Table<Setting> ().FirstOrDefault ()) == null) {
				db.Insert(setting = new Setting ());
			}
			StartupTheme = setting.Theme;
			SetTheme (setting.Theme);
			base.OnCreate (bundle);

			new UpdateChecker (this, db, setting);

			// Check if the application knows a user.
			User user = db.Table<User>().FirstOrDefault ();
			if (user != null && user.user != null) {
				initializeAuthenticated (user);
			} else {
				initializeNotAuthenticated ();
			}
		}

		public async void OnUpdateRequested() {
			User user = db.Table<User>().FirstOrDefault ();
			if (user != null && user.user != null) {
				RunOnUiThread (() => { if (isUnbound()) return; ShowChats(user); });
			}
		}

		public void OnBind() {
			if (service != null) {
				service.MainActivity = this;
				service.ResetUpdateInterval ();
				network.SetBackgroundService (service);
			}
		}

		public void OnUnBind() {
			if (service != null) {
				service.MainActivity = null;
				service.ResetUpdateInterval ();
			}
		}

        private bool isUnbound()
        {
            if (service != null)
            {
                return service.MainActivity == null;
            }
            return true;
        }

        public override bool OnPrepareOptionsMenu(IMenu menu)
		{
			menu.Clear ();
			MenuInflater.Inflate(Resource.Menu.main, menu);
			return base.OnPrepareOptionsMenu(menu);
		}

		public override bool OnOptionsItemSelected(IMenuItem item)
		{
			switch (item.ItemId)
			{
			case Resource.Id.action_addFriend:
				//do something
				return true;
			case Resource.Id.action_createConversation:
				//do something
				return true;
			case Resource.Id.action_settings:
				StartActivity (new Intent (this, typeof(SettingsActivity)));
				return true;
			}
			return base.OnOptionsItemSelected(item);
		}

		protected async override void OnResume() {
			base.OnResume ();
			if (StartupTheme != db.Table<Setting> ().FirstOrDefault ().Theme) {
				Refresh ();
			}
			User user = db.Table<User>().FirstOrDefault ();
			if (user != null && user.user != null) {
				ShowChats (user);
			}

			OnBind ();
		}

		private void Refresh() {
			Finish();
			StartActivity(new Intent(this, typeof(MainActivity)));
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

        [MethodImpl(MethodImplOptions.Synchronized)]
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
				View v;
				if (setting.FontSize == Setting.Size.large) {
					v = LayoutInflater.Inflate (Resource.Layout.ChatLarge, null);
				} else {
					v = LayoutInflater.Inflate (Resource.Layout.Chat, null);
				}
				TextView name = v.FindViewById<TextView>(Resource.Id.chatName);
				TextView message = v.FindViewById<TextView>(Resource.Id.chatMessage);
				TextView time = v.FindViewById<TextView>(Resource.Id.chatTime);
				ImageView image = v.FindViewById<ImageView> (Resource.Id.chatImage);

				if (elem.Marked)
                {
                    message.SetTypeface(null, TypefaceStyle.Bold);
				}

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
						var imageBitmap = await network.GetImageBitmapFromUrl(Resources.GetString(Resource.String.profileUrl) + conv + "_mini.png", AsyncNetwork.MINI_PROFILE_SIZE, AsyncNetwork.MINI_PROFILE_SIZE);
                        if (imageBitmap == null)
                        {
                            imageBitmap = await network.GetImageBitmapFromUrl(Resources.GetString(Resource.String.profileUrl) + "user_mini.png", AsyncNetwork.MINI_PROFILE_SIZE, AsyncNetwork.MINI_PROFILE_SIZE);
                        }
						RunOnUiThread(() => { if (isUnbound()) return; image.SetImageBitmap(imageBitmap); });
					} catch (Exception e) {
						Log.Error("BlaChat", e.StackTrace);
					}
				}).Start();

				name.TextFormatted = SpannableTools.GetSmiledText (this, new SpannableString(elem.name));
				var tmp = db.Table<Message> ().Where (q => q.conversation == elem.conversation).OrderByDescending (q => q.time);
				var lastMsg = tmp.FirstOrDefault ();
				if (lastMsg != null) {
					var escape = lastMsg.text.Replace ("&quot;", "\"");
					escape = escape.Replace ("&lt;", "<");
					escape = escape.Replace ("&gt;", ">");
					escape = escape.Replace ("&amp;", "&");
					if (escape.StartsWith ("#image")) {
						escape = "(image)";
					} else if (escape.StartsWith ("#video")) {
						escape = "(video)";
					}
					if (lastMsg.nick == user.user) {
						message.TextFormatted = SpannableTools.GetSmiledText (this, new SpannableString("Du: " + escape));
                        //message.SetTextColor(Color.DarkBlue);
                        message.SetTypeface(null, TypefaceStyle.Italic);
                    } else {
						if (elem.conversation.Split (',').Length == 2 && lastMsg.nick != "watchdog") {
							message.TextFormatted = SpannableTools.GetSmiledText (this, new SpannableString(escape));
						} else {
							message.TextFormatted = SpannableTools.GetSmiledText (this, new SpannableString(lastMsg.author + ": " + escape));
                        }
                        //message.SetTextColor(Color.DarkGreen);
                    }
					time.Text = TimeConverter.AutoConvert(lastMsg.time);
				} else {
					time.Text = "";
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

				InputMethodManager manager = (InputMethodManager) GetSystemService(InputMethodService);
				manager.HideSoftInputFromWindow(FindViewById<EditText> (Resource.Id.username).WindowToken, 0);
				manager.HideSoftInputFromWindow(FindViewById<EditText> (Resource.Id.password).WindowToken, 0);
				manager.HideSoftInputFromWindow(FindViewById<EditText> (Resource.Id.server).WindowToken, 0);

				login.Text = "Connecting...";
				login.Enabled = false;

				var user = new User() {
					user = FindViewById<EditText> (Resource.Id.username).Text.ToLower(),
					password = FindViewById<EditText> (Resource.Id.password).Text,
					server = !string.IsNullOrEmpty (FindViewById<EditText> (Resource.Id.server).Text) ?
						FindViewById<EditText> (Resource.Id.server).Text :
						FindViewById<EditText> (Resource.Id.server).Hint
				};
				if (user.user == "" || user.password == "") {
					login.Text = "Login";
					login.Enabled = true;
					return;
				}

				if(await network.Authenticate(db, user)) {
					login.Text = "Loading data...";
					new Thread(async () => {
						int i = 0;
						db.Insert(user);
						while(!await network.UpdateChats (db, user)) {
							await network.Authenticate(db, user);
						}
						var x = db.Table<Chat> ();
						int count = x.Count();
						var tasks = new List<Task<bool>>();
						RunOnUiThread(() => { if (isUnbound()) return; initializeAuthenticated(user); });

						foreach (var chat in x) {
							while(!await network.UpdateHistory(db, user, chat, 30)) {
								await network.Authenticate(db, user);
							}
							OnUpdateRequested();
						}
					}).Start();
				} else {
					login.Text = "Retry Login";
					login.Enabled = true;
				}
			};
		}
	}
}


