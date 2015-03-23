
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
using System.Threading;
using Android.Provider;
using Android.Graphics;
using Android.Util;

namespace BlaChat
{
	[Activity (Label = "BlaChat Share", Icon = "@drawable/icon")]			
	public class ShareActivity : Activity
	{
		public BackgroundService service;
		private AsyncNetwork network = new AsyncNetwork();
		private DataBaseWrapper db = null;
		private ServiceConnection serviceConnection = null;
		Setting setting = null;
		List<Android.Net.Uri> fileset = new List<Android.Net.Uri>();
		string textShare = null;

		protected override void OnCreate (Bundle bundle)
		{
			db = new DataBaseWrapper (this.Resources);
			if ((setting = db.Table<Setting> ().FirstOrDefault ()) == null) {
				db.Insert(setting = new Setting ());
			}
			SetTheme (setting.Theme);
			base.OnCreate (bundle);

			if (Intent.ActionSend == Intent.Action)
			{
				if (Intent.GetStringExtra(Intent.ExtraText) != null) {
					textShare = Intent.GetStringExtra(Intent.ExtraText);
				} else {
					var uriString = Intent.GetParcelableExtra (Intent.ExtraStream).ToString();
					var uri = Android.Net.Uri.Parse(uriString);
					if (uri != null) {
						fileset.Add(uri);
					}
				}
			}
			else if (Intent.ActionSendMultiple == Intent.Action)
			{
				var uris= Intent.GetParcelableArrayListExtra(Intent.ExtraStream);
				if (uris != null) {
					foreach(var uri in uris)                         
					{
						fileset.Add(Android.Net.Uri.Parse(uri.ToString()));
					}
				}
			}

			// Check if the application knows a user.
			User user = db.Table<User>().FirstOrDefault ();
			if (user != null && user.user != null) {
				initializeAuthenticated (user);
			}
		}

		public async void OnUpdateRequested() {
			User user = db.Table<User>().FirstOrDefault ();
			if (user != null && user.user != null) {
				RunOnUiThread (() => ShowChats (user));
			}
		}

		public void OnBind() {
			if (service != null) {
				service.ResetUpdateInterval ();
				network.SetBackgroundService (service);
			}
		}

		public void OnUnBind() {
			if (service != null) {
				service.ResetUpdateInterval ();
			}
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
			User user = db.Table<User>().FirstOrDefault ();
			if (user != null && user.user != null) {
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
						Log.Error("BlaChat", e.StackTrace);
					}
				}).Start();

				name.Text = elem.name;
				var tmp = db.Table<Message> ().Where (q => q.conversation == elem.conversation).OrderByDescending (q => q.time);
				var lastMsg = tmp.FirstOrDefault ();
				if (lastMsg != null) {
					var escape = lastMsg.text.Replace ("&quot;", "\"");
					escape = escape.Replace ("&lt;", "<");
					escape = escape.Replace ("&gt;", ">");
					escape = escape.Replace ("&amp;", "&");
					if (lastMsg.nick == user.user) {
						message.Text = "Du: " + escape;
					} else {
						if (elem.conversation.Split (',').Length == 2 && lastMsg.nick != "watchdog") {
							message.Text = escape;
						} else {
							message.Text = lastMsg.author + ": " + escape;
						}
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
					var chat = db.Get<Chat> (elem.conversation);
					new Thread(async () => {
					if (textShare != null) {
						await network.SendMessage(db, user, chat, "(Shared Text) " + textShare);
					}
					foreach(var file in fileset) {
						Bitmap img = MediaStore.Images.Media.GetBitmap(ContentResolver, file);
						await network.SendImage(db, user, chat, img);
					}
					}).Start();
				};
				chatList.AddView(v);
			}
			chatList.Post(() => chatList.RequestLayout ());
		}
	}
}

