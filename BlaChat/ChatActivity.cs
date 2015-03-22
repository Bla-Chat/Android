
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Views.InputMethods;
using System.Threading.Tasks;
using System.Threading;
using Android.Util;
using Android.Graphics;
using System.Net;
using Android.Provider;

namespace BlaChat
{
	[Activity (Label = "BlaChat", Icon = "@drawable/icon", WindowSoftInputMode=SoftInput.StateHidden, ParentActivity=typeof(MainActivity))]
	public class ChatActivity : Activity
	{
		public static readonly int PickImageId = 1000;

		public BackgroundService service;
		private ServiceConnection serviceConnection = null;
		private AsyncNetwork network = new AsyncNetwork();
		private DataBaseWrapper db = null;
		private string conversation;
		private int visibleMessages = 30;
		private List<Message> displayedMessages = new List<Message> ();
		private Chat chat;
		private User user;
		Setting setting = null;

		protected override void OnCreate (Bundle bundle)
		{
			db = new DataBaseWrapper (this.Resources);
			if ((setting = db.Table<Setting> ().FirstOrDefault ()) == null) {
				db.Insert(setting = new Setting ());
			}
			SetTheme (setting.Theme);
			base.OnCreate (bundle);

			SetContentView (Resource.Layout.ChatActivity);

			conversation = Intent.GetStringExtra ("conversation");

			chat = db.Get<Chat> (conversation);
			Title = chat.name;
			ActionBar.SetDisplayHomeAsUpEnabled(true);
			ActionBar.SetIcon (Resource.Drawable.Icon);

			user = db.Table<User>().FirstOrDefault ();

			Button send = FindViewById<Button> (Resource.Id.send);
			send.Click += delegate {
				TextView message = FindViewById<TextView> (Resource.Id.message);
				var msg = message.Text;
				message.Text = "";

				if (msg.Equals("")) return;

				string tmp = send.Text;
				send.Enabled = false;

				InputMethodManager manager = (InputMethodManager) GetSystemService(InputMethodService);
				manager.HideSoftInputFromWindow(message.WindowToken, 0);
				message.Post(() => manager.HideSoftInputFromWindow(message.WindowToken, 0));

				OnBind();
				new Thread(async () => {
					await network.SendMessage (db, user, chat, msg);
					RunOnUiThread(() => {
						send.Enabled = true;
					});
				}).Start();
			};
		}

		public override bool OnPrepareOptionsMenu(IMenu menu)
		{
			menu.Clear ();
			MenuInflater.Inflate(Resource.Menu.chat, menu);
			if (visibleMessages <= 30) {
				var item = menu.FindItem (Resource.Id.action_lessMessages);
				item.SetVisible (false);
				item = menu.FindItem (Resource.Id.action_defaultMessages);
				item.SetVisible (false);
			}
			return base.OnPrepareOptionsMenu(menu);
		}

		public override bool OnOptionsItemSelected(IMenuItem item)
		{
			switch (item.ItemId)
			{
			case Resource.Id.action_rename:
				//do something
				return true;
			case Resource.Id.action_defaultMessages:
				defaultMessages();
				return true;
			case Resource.Id.action_lessMessages:
				lessMessages ();
				return true;
			case Resource.Id.action_moreMessages:
				moreMessages ();
				return true;
			case Resource.Id.action_sendImage:
				sendImage ();
				return true;
			case Resource.Id.action_setImage:
				//do something
				return true;
			case Resource.Id.action_settings:
				StartActivity (new Intent (this, typeof(SettingsActivity)));
				return true;
			}
			return base.OnOptionsItemSelected(item);
		}

		private void lessMessages() {
			visibleMessages -= 30;
			UpdateMessages (user);
			InvalidateOptionsMenu ();
			OnBind();
		}

		private void defaultMessages() {
			visibleMessages = 30;
			UpdateMessages (user);
			InvalidateOptionsMenu ();
			OnBind();
		}

		private void moreMessages() {
			OnBind();

			visibleMessages += 30;

			var x = db.Table<Message> ();
			if (x.Where(q => q.conversation == conversation).Count() < visibleMessages) {
				new Thread(async () => {
					await network.UpdateHistory(db, user, chat, visibleMessages);
					RunOnUiThread(() => UpdateMessages (user));
				}).Start();
			} else {
				UpdateMessages (user);
			}
			InvalidateOptionsMenu ();
		}

		private void sendImage() {
			OnBind ();

			Intent pickIntent = new Intent ();
			pickIntent.SetType ("image/*");
			pickIntent.SetAction (Intent.ActionGetContent);
			StartActivityForResult (Intent.CreateChooser (pickIntent, "Select Picture"), PickImageId);
		}

		protected override void OnActivityResult(int requestCode, Result resultCode, Intent data) {
			base.OnActivityResult (requestCode, resultCode, data);
			if (requestCode == PickImageId && resultCode == Result.Ok && data != null) {
				Toast.MakeText (this, "Sending image...", ToastLength.Long);

				Bitmap img = MediaStore.Images.Media.GetBitmap(ContentResolver, data.Data);
				new Thread(() => network.SendImage (db, user, chat, img)).Start();
			}
		}

		public void OnBind() {
			if (service != null) {
				service.ResetUpdateInterval ();
				service.ActiveConversation = conversation;
				service.ChatActivity = this;
				network.SetBackgroundService (service);
			}
		}

		public void OnUnBind() {
			if (service != null) {
				service.ChatActivity = null;
				service.ResetUpdateInterval ();
				if (service.ActiveConversation == conversation) {
					service.ActiveConversation = "";
				}
			}
		}

		protected override void OnResume() {
			base.OnResume ();

			if (service == null) {
				var sericeIntent = new Intent (this, typeof(BackgroundService));
				serviceConnection = new ServiceConnection (this);
				BindService (sericeIntent, serviceConnection, Bind.AutoCreate);
			}

			OnBind ();

			User user = db.Table<User>().FirstOrDefault ();
			if (user != null && user.user != null) {
				UpdateMessagesScrollDown (user);
			}
		}

		public async void OnUpdateRequested() {
			lock (this) {
				if (user != null && user.user != null) {
					RunOnUiThread (() => UpdateMessagesScrollDown (user));
				}
			}
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

		private void UpdateMessagesScrollDown(User user) {
				UpdateMessages (user);

				ScrollView scrollView = FindViewById<ScrollView> (Resource.Id.messageScrollView);
				scrollView.FullScroll (FocusSearchDirection.Down);
				scrollView.Post (() => scrollView.FullScroll (FocusSearchDirection.Down));
		}

		private void UpdateMessages(User user) {
			LinearLayout messageList = FindViewById<LinearLayout> (Resource.Id.messageLayout);
			messageList.RemoveAllViews ();
			var x = db.Table<Message> ();
			List<Message> tmp = x.Where(q => q.conversation == conversation).OrderBy(e => e.time).Reverse().Take(visibleMessages).Reverse().ToList();
			string prevDate = "0000-00-00";
			foreach (var elem in tmp) {
				if (!elem.time.StartsWith (prevDate)) {
					prevDate = TimeConverter.Convert (elem.time, "yyyy-MM-dd");
					View timeInsert;
					if (setting.FontSize == Setting.Size.large) {
						timeInsert = LayoutInflater.Inflate (Resource.Layout.TimeInsertLarge, null);
					} else {
						timeInsert = LayoutInflater.Inflate (Resource.Layout.TimeInsert, null);
					}
					TextView text = timeInsert.FindViewById<TextView> (Resource.Id.timeInsertTime);
					text.Text = TimeConverter.AutoConvertDate (elem.time);
					messageList.AddView(timeInsert);
				}

				View v = null;
				if (elem.text.StartsWith ("#image")) {
					if (elem.nick == user.user) {
						if (setting.FontSize == Setting.Size.large) {
							v = LayoutInflater.Inflate (Resource.Layout.ImageRightLarge, null);
						} else {
							v = LayoutInflater.Inflate (Resource.Layout.ImageRight, null);
						}
					} else {
						if (setting.FontSize == Setting.Size.large) {
							v = LayoutInflater.Inflate (Resource.Layout.ImageLeftLarge, null);
						} else {
							v = LayoutInflater.Inflate (Resource.Layout.ImageLeft, null);
						}
						ImageView image = v.FindViewById<ImageView> (Resource.Id.messageImage);
						new Thread (async () => {
							try {
								var imageBitmap = await network.GetImageBitmapFromUrl(Resources.GetString(Resource.String.profileUrl) + elem.nick + ".png");
								RunOnUiThread(() => image.SetImageBitmap(imageBitmap));
							} catch (Exception e) {
								Log.Error("BlaChat", e.StackTrace);
							}
						}).Start();
					}
					ImageView contentImage = v.FindViewById<ImageView> (Resource.Id.contentImage);
					contentImage.Click += delegate {
						string images = System.IO.Path.Combine (Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, "Pictures/BlaChat");
						var filename = elem.text.Substring ("#image ".Length);
						filename = filename.Substring (filename.LastIndexOf ("/") + 1);
						filename = System.IO.Path.Combine (images, filename);
						Intent intent = new Intent (Intent.ActionView);
						intent.SetDataAndType (Android.Net.Uri.Parse ("file://" + filename), "image/*");
						StartActivity (intent);
					};
					//contentImage.SetOnTouchListener (new TouchListener(this, elem.text.Substring ("#image ".Length)));

					new Thread (async () => {
						try {
							var uri = elem.text.Substring ("#image ".Length);
							var imageBitmap = await network.GetImageBitmapFromUrl(uri);
							RunOnUiThread(() => contentImage.SetImageBitmap(imageBitmap));
						} catch (Exception e) {
							Log.Error("BlaChat", e.StackTrace);
						}
					}).Start();
				} else {
					if (elem.nick == user.user) {
						if (setting.FontSize == Setting.Size.large) {
							v = LayoutInflater.Inflate (Resource.Layout.MessageRightLarge, null);
						} else {
							v = LayoutInflater.Inflate (Resource.Layout.MessageRight, null);
						}
					} else {
						if (setting.FontSize == Setting.Size.large) {
							v = LayoutInflater.Inflate (Resource.Layout.MessageLeftLarge, null);
						} else {
							v = LayoutInflater.Inflate (Resource.Layout.MessageLeft, null);
						}
						ImageView image = v.FindViewById<ImageView> (Resource.Id.messageImage);
						new Thread (async () => {
							try {
								var imageBitmap = await network.GetImageBitmapFromUrl(Resources.GetString(Resource.String.profileUrl) + elem.nick + ".png");
								RunOnUiThread(() => image.SetImageBitmap(imageBitmap));
							} catch (Exception e) {
								Log.Error("BlaChat", e.StackTrace);
							}
						}).Start();
					}
					TextView text = v.FindViewById<TextView>(Resource.Id.messageText);
					var escape = elem.text.Replace ("&quot;", "\"");
					escape = escape.Replace ("&lt;", "<");
					escape = escape.Replace ("&gt;", ">");
					escape = escape.Replace ("&amp;", "&");
					text.Text = escape;
				}

				TextView caption = v.FindViewById<TextView>(Resource.Id.messageCaption);
				if (elem.nick != user.user) {
					caption.Text = elem.author + " (" + elem.time.Substring(11, 5) + ")";
				} else {
					caption.Text = "Du (" + elem.time.Substring(11, 5) + ")";
				}
				messageList.AddView(v);
			}
			messageList.Post(() => messageList.RequestLayout ());
		}
	}
}

