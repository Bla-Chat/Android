
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
using Android.Text;
using System.Threading;
using Android.Util;

namespace BlaChat
{
	[Activity (Label = "Chat Settings", Icon = "@drawable/icon")]			
	public class ChatSettingsActivity : Activity
	{
		DataBaseWrapper db = null;
		User user = null;
		Setting setting = null;
		AsyncNetwork network;
		Chat chat;

		protected override void OnCreate (Bundle bundle)
		{
			db = new DataBaseWrapper (this.Resources);
			if ((setting = db.Table<Setting> ().FirstOrDefault ()) == null) {
				db.Insert(setting = new Setting ());
			}
			SetTheme (setting.Theme);
			base.OnCreate (bundle);

			network = new AsyncNetwork ();
			db = new DataBaseWrapper (this.Resources);
			user = db.Table<User>().FirstOrDefault ();

			if (setting.FontSize == Setting.Size.large) {
				SetContentView (Resource.Layout.ChatSettingsLarge);
			} else {
				SetContentView (Resource.Layout.ChatSettings);
			}

			var conversation = Intent.GetStringExtra ("conversation");
			chat = db.Get<Chat> (conversation);

			InitializeView();
		}

		void Refresh() {
			Finish();
			var intent = new Intent (this, typeof(ChatSettingsActivity));
			intent.PutExtra ("conversation", chat.conversation);
			StartActivity (intent);
		}

		void InitializeView ()
		{
			var notifications = FindViewById<CheckBox> (Resource.Id.chatNotifications);
			var readMessages = FindViewById<CheckBox> (Resource.Id.readMessages);

			notifications.Checked = chat.Notifications;
			readMessages.Checked = chat.ReadMessagesEnabled;

			if (!setting.ReadMessagesEnabled) {
				readMessages.Text += " (globaly disabled)";
			}
			if (!setting.Notifications) {
				notifications.Text += " (globaly disabled)";
			}

			notifications.CheckedChange += delegate { chat.Notifications = notifications.Checked; db.Update(chat); };
			readMessages.CheckedChange += delegate { chat.ReadMessagesEnabled = readMessages.Checked; db.Update(chat); };

			var rename = FindViewById<TextView> (Resource.Id.chatName);
			var setImage = FindViewById<ImageButton> (Resource.Id.chatImage);
			var visibleMessages = FindViewById<EditText> (Resource.Id.visibleMessages);

			visibleMessages.Text = chat.VisibleMessages.ToString();
			rename.Text = chat.name;

			rename.TextChanged += delegate {
				if (chat.name.Length > 0) {
					chat.name = rename.Text; db.Update(chat);
				}
			};

			visibleMessages.TextChanged += delegate {
				try { chat.VisibleMessages = int.Parse(visibleMessages.Text); db.Update(chat); } catch (Exception) { chat.VisibleMessages = 0; db.Update(chat); }
			};

			if (chat.conversation.Split (',').Count () != 2) {
				setImage.Click += delegate {
					Toast.MakeText(this, "Not implemented yet.", ToastLength.Short).Show();
				};
			} else {
				setImage.Click += delegate {
					Toast.MakeText(this, "Cannot be changed by you.", ToastLength.Short).Show();
				};
			}
		}
	}
}

