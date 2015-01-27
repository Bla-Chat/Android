
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

namespace BlaChat
{
	[Activity (Label = "BlaChat", Icon = "@drawable/icon")]	
	public class ChatActivity : Activity
	{
		public BackgroundService service;
		private ServiceConnection serviceConnection = null;
		private AsyncNetwork network = new AsyncNetwork();
		private DataBaseWrapper db = null;
		private string conversation;
		private int visibleMessages = 30;
		private List<Message> displayedMessages = new List<Message> ();

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			db = new DataBaseWrapper (this.Resources);

			SetContentView (Resource.Layout.ChatActivity);

			conversation = Intent.GetStringExtra ("conversation");

			var chat = db.Get<Chat> (conversation);
			Title = "BlaChat " + chat.name;

			TextView message = FindViewById<TextView> (Resource.Id.message);
			InputMethodManager manager = (InputMethodManager) GetSystemService(InputMethodService);
			manager.HideSoftInputFromWindow(message.WindowToken, 0);
			message.Post(() => manager.HideSoftInputFromWindow(message.WindowToken, 0));

			User user = db.Table<User>().FirstOrDefault ();

			Button send = FindViewById<Button> (Resource.Id.send);
			send.Click += async delegate {
				var msg = message.Text;
				message.Text = "";

				await network.SendMessage (db, user, chat, msg);
				await network.UpdateHistory (db, user, chat, 30);
				UpdateMessagesScrollDown (user);
				OnBind();
			};

			Button more = FindViewById<Button> (Resource.Id.moreMessages);
			more.Click += delegate {
				visibleMessages *= 2;
				UpdateMessages (user);
				new Thread(async () => {
					await network.UpdateHistory (db, user, chat, visibleMessages);
					RunOnUiThread(() => UpdateMessages (user));
				}).Start();
				OnBind();
			};

			Button less = FindViewById<Button> (Resource.Id.lessMessages);
			less.Click += delegate {
				visibleMessages /= 2;
				UpdateMessages (user);
				OnBind();
			};

			Button defaultMessages = FindViewById<Button> (Resource.Id.defaultMessages);
			defaultMessages.Click += delegate {
				visibleMessages = 30;
				UpdateMessages (user);
				OnBind();
			};
		}

		public void OnBind() {
			if (service != null) {
				service.ResetUpdateInterval ();
				service.ActiveConversation = conversation;
			}
		}

		public void OnUnBind() {
			if (service != null) {
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

			User user = db.Table<User>().FirstOrDefault ();
			if (user != null && user.user != null) {
				UpdateMessagesScrollDown (user);

				new Thread(async () => {
					var chat = db.Get<Chat> (conversation);
					await network.UpdateHistory (db, user, chat, visibleMessages);
					RunOnUiThread(() => UpdateMessagesScrollDown (user));
				}).Start();
			}
		}

		public void OnUpdateRequested() {
			User user = db.Table<User>().FirstOrDefault ();
			if (user != null && user.user != null) {
				RunOnUiThread(() => UpdateMessagesScrollDown (user));
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

			TextView message = FindViewById<TextView> (Resource.Id.message);
			InputMethodManager manager = (InputMethodManager) GetSystemService(InputMethodService);
			manager.HideSoftInputFromWindow(message.WindowToken, 0);
			message.PostDelayed(() => manager.HideSoftInputFromWindow(message.WindowToken, 0), 200);

			ScrollView scrollView = FindViewById<ScrollView> (Resource.Id.messageScrollView);
			scrollView.FullScroll(FocusSearchDirection.Down);
			scrollView.PostDelayed (() => scrollView.FullScroll (FocusSearchDirection.Down), 200);
		}

		private void UpdateMessages(User user) {
			LinearLayout messageList = FindViewById<LinearLayout> (Resource.Id.messageLayout);
			messageList.RemoveAllViews ();
			var x = db.Table<Message> ();
			List<Message> tmp = x.Where(q => q.conversation == conversation).OrderBy(e => e.time).Reverse().Take(visibleMessages).Reverse().ToList();
			foreach (var elem in tmp) {
				View v = null;
				if (elem.text.StartsWith ("#image")) {
					if (elem.nick == user.user) {
						v = LayoutInflater.Inflate (Resource.Layout.ImageRight, null);
					} else {
						v = LayoutInflater.Inflate (Resource.Layout.ImageLeft, null);
						ImageView image = v.FindViewById<ImageView> (Resource.Id.messageImage);
						new Thread (async () => {
							try {
								var imageBitmap = await network.GetImageBitmapFromUrl(Resources.GetString(Resource.String.profileUrl) + elem.nick + ".png");
								RunOnUiThread(() => image.SetImageBitmap(imageBitmap));
							} catch (Exception e) {
								Log.Error("ChatMessageImage", e.StackTrace);
							}
						}).Start();
					}
					ImageView contentImage = v.FindViewById<ImageView> (Resource.Id.contentImage);
					new Thread (async () => {
						try {
							var uri = elem.text.Substring ("#image ".Length);
							var imageBitmap = await network.GetImageBitmapFromUrl(uri);
							RunOnUiThread(() => contentImage.SetImageBitmap(imageBitmap));
						} catch (Exception e) {
							Log.Error("ChatMessageImage", e.StackTrace);
						}
					}).Start();
				} else {
					if (elem.nick == user.user) {
						v = LayoutInflater.Inflate (Resource.Layout.MessageRight, null);
					} else {
						v = LayoutInflater.Inflate (Resource.Layout.MessageLeft, null);
						ImageView image = v.FindViewById<ImageView> (Resource.Id.messageImage);
						new Thread (async () => {
							try {
								var imageBitmap = await network.GetImageBitmapFromUrl(Resources.GetString(Resource.String.profileUrl) + elem.nick + ".png");
								RunOnUiThread(() => image.SetImageBitmap(imageBitmap));
							} catch (Exception e) {
								Log.Error("ChatMessageImage", e.StackTrace);
							}
						}).Start();
					}
					TextView text = v.FindViewById<TextView>(Resource.Id.messageText);
					text.Text = elem.text;
				}

				TextView caption = v.FindViewById<TextView>(Resource.Id.messageCaption);
				if (elem.nick != user.user) {
					caption.Text = elem.author + " (" + elem.time + ")";
				} else {
					caption.Text = "You (" + elem.time + ")";
				}
				messageList.AddView(v);
			}
			messageList.Post(() => messageList.RequestLayout ());
		}
	}
}

