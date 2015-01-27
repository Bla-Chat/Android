
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Json;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Threading.Tasks;
using System.Net.Http;
using Android.Util;
using Android.Graphics;
using System.Net;
using System.IO;
using System.Threading;


namespace BlaChat
{	
	public class AsyncNetwork
	{
		private HttpClient httpClient;
		private Dictionary<string, Task<Bitmap>> bitmaps = new Dictionary<string, Task<Bitmap>>();

		public AsyncNetwork()
		{
			httpClient = new HttpClient();
		}

		public Task<Bitmap> GetImageBitmapFromUrl(string url)
		{
			lock (httpClient) {
				if (bitmaps.ContainsKey (url)) {
					return bitmaps [url];
				}
				return bitmaps[url] = GetImageBitmapFromUrlAsync(url);
			}
		}

		private async Task<Bitmap> GetImageBitmapFromUrlAsync(string url) {
			Bitmap imageBitmap = null;

			string images = System.IO.Path.Combine (System.Environment.GetFolderPath (System.Environment.SpecialFolder.Personal), "images");

			Directory.CreateDirectory (images);

			string name = url.Substring (url.LastIndexOf ("/") + 1);

			string imageName = System.IO.Path.Combine (images, name);
			if (!File.Exists (imageName)) {
				byte[] imageBytes = null;
				while (imageBytes == null) {
					try {
						imageBytes = await httpClient.GetByteArrayAsync (url);
					} catch (Exception e) {
						Log.Error ("NetworkImage", e.StackTrace);
					}
				}
				if (imageBytes != null && imageBytes.Length > 0) {
					imageBitmap = BitmapFactory.DecodeByteArray (imageBytes, 0, imageBytes.Length);
				}
				try {
					Stream s = File.OpenWrite (imageName);
					imageBitmap.Compress (Bitmap.CompressFormat.Png, 100, s);
					s.Flush ();
					s.Close ();
				} catch (Exception e) {
					Log.Error ("BitmapFlush", e.StackTrace);
				}
			} else {
				imageBitmap = BitmapFactory.DecodeFile (imageName, new BitmapFactory.Options ());
			}
			return imageBitmap;
		}

		public async Task<string> Download(string url)
		{
			string contentsTask = null;
			while (contentsTask == null) {
				try {
					contentsTask = await httpClient.GetStringAsync (url);
				} catch (Exception e) {
					Log.Error ("NetworkDownload", e.StackTrace);
				}
			}
			return contentsTask;
		}

		public async Task<bool> Authenticate(DataBaseWrapper db, User user)
		{
			string encodedJson = escape (String.Format ("{{\"user\":\"{0}\", \"pw\":\"{1}\"}}", user.user, user.password));

			var result = JsonValue.Parse (await Download (user.server + "/xjcp.php?msg=" + encodedJson));
			try {
				EventHandling (db, result);

				if (result.ContainsKey ("id")) {
					user.id = result ["id"];
					db.Update (user);
					return true;
				}
			} catch (Exception ex) {
				Log.Error ("AsyncNetworkError", ex.StackTrace);
			}

			return false;
		}

		public async Task<bool> UpdateChats(DataBaseWrapper db, User user)
		{
			string encodedJson = escape (String.Format ("{{\"id\":\"{0}\", \"getChats\":{{}}}}", user.id));

			var result = JsonValue.Parse (await Download (user.server + "/xjcp.php?msg=" + encodedJson));
			try {
				EventHandling (db, result);

				if (result.ContainsKey ("onGetChats")) {
					JsonArray arr = (JsonArray)result ["onGetChats"];
					foreach (JsonValue v in arr) {
						string conv = v ["conversation"];
						var chat = db.Get<Chat> (conv);
						if (chat == null) {
							chat = new Chat () {
								conversation = conv
							};
							db.Insert (chat);
						}
						chat.name = v ["name"];
						chat.time = v ["time"];
						db.Update (chat);

					}
					return true;
				}
			} catch (Exception ex) {
				Log.Error ("AsyncNetworkError", ex.StackTrace);
			}

			return false;
		}

		public async Task<bool> UpdateContacts(DataBaseWrapper db, User user)
		{
			string encodedJson = escape (String.Format ("{{\"id\":\"{0}\", \"getContacts\":{{}}}}", user.id));

			var result = JsonValue.Parse (await Download (user.server + "/xjcp.php?msg=" + encodedJson));
			try {
				EventHandling (db, result);

				if (result.ContainsKey ("onGetContacts")) {
					JsonArray arr = (JsonArray)result ["onGetContacts"];
					foreach (JsonValue v in arr) {
						var contact = db.Get<Contact> (v ["nick"]);
						if (contact == null) {
							contact = new Contact () {
								nick = v ["nick"]
							};
							db.Insert (contact);
						}
						contact.name = v ["name"];
						contact.status = v ["status"];
						db.Update (contact);

					}
					return true;
				}
			} catch (Exception ex) {
				Log.Error ("AsyncNetworkError", ex.StackTrace);
			}

			return false;
		}

		public async Task<bool> SendMessage(DataBaseWrapper db, User user, Chat chat, string message)
		{
			string encodedJson = escape (String.Format ("{{\"id\":\"{0}\", \"message\":{{\"conversation\":\"{1}\", \"message\":\"{2}\"}}}}", user.id, chat.conversation, message));

			var result = JsonValue.Parse (await Download (user.server + "/xjcp.php?msg=" + encodedJson));
			try {
				EventHandling (db, result);

				if (result.ContainsKey ("onMessage")) {
					return true;
				}
			} catch (Exception ex) {
				Log.Error ("AsyncNetworkError", ex.StackTrace);
			}

			return false;
		}

		public async Task<bool> UpdateEvents(DataBaseWrapper db, User user)
		{
			string encodedJson = escape(String.Format("{{\"id\":\"{0}\"}}", user.id));

			var result = JsonValue.Parse(await Download(user.server + "/xjcp.php?msg=" + encodedJson));

			try {
				EventHandling (db, result);
			} catch (Exception ex) {
				Log.Error ("AsyncNetworkError", ex.StackTrace);
			}

			return true;
		}

		public async Task<bool> RemoveEvent(DataBaseWrapper db, User user, Event e)
		{
			string encodedJson = escape(String.Format("{{\"id\":\"{0}\", \"removeEvent\":{{\"conversation\":\"{1}\"}}}}", user.id, e.msg));

			var result = JsonValue.Parse(await Download(user.server + "/xjcp.php?msg=" + encodedJson));

			try {
				EventHandling (db, result);
			} catch (Exception ex) {
				Log.Error ("AsyncNetworkError", ex.StackTrace);
			}

			return true;
		}

		public async Task<bool> NewChat(DataBaseWrapper db, User user, List<User> users, string name)
		{
			return false;
		}

		public async Task<bool> RenameChat(DataBaseWrapper db, User user, Chat chat, string name)
		{
			return false;
		}

		public async Task<bool> SetName(DataBaseWrapper db, User user, string name)
		{
			return false;
		}

		public async Task<bool> AddFriend(DataBaseWrapper db, User user, string name)
		{
			return false;
		}
			
		public async Task<bool> SetStatus(DataBaseWrapper db, User user, string status)
		{
			return false;
		}

		public async Task<bool> SetProfileImage(DataBaseWrapper db, User user, object image)
		{
			return false;
		}

		public async Task<bool> SetGroupImage(DataBaseWrapper db, User user, Chat chat, object image)
		{
			return false;
		}

		public async Task<bool> InjectEvent(DataBaseWrapper db, User user, Event e)
		{
			return false;
		}

		public async Task<bool> Data(DataBaseWrapper db, User user, Chat chat, object data)
		{
			return false;
		}

		public async Task<bool> UpdateHistory(DataBaseWrapper db, User user, Chat chat, int count)
		{
			string encodedJson = escape (String.Format ("{{\"id\":\"{0}\", \"getHistory\":{{\"conversation\":\"{1}\", \"count\":\"{2}\"}}}}", user.id, chat.conversation, count));

			var result = JsonValue.Parse (await Download (user.server + "/xjcp.php?msg=" + encodedJson));

			try {
				EventHandling (db, result);

				if (result.ContainsKey ("onGetHistory")) {
					var arr = result ["onGetHistory"];
					var msgs = arr ["messages"];
					string conversation = arr ["conversation"];

					foreach (JsonValue x in msgs) {
						try {
							var tmp = db.Table<Message> ().Reverse ().Where (s => s.nick == x ["nick"] && s.conversation == conversation && s.author == x ["author"] && s.text == x ["text"] && s.time == x ["time"]).FirstOrDefault ();
							if (tmp == null) {
								var msg = new Message ();
								msg.conversation = conversation;
								msg.author = x ["author"];
								msg.nick = x ["nick"];
								msg.text = x ["text"];
								msg.time = x ["time"];
								db.Insert (msg);
							}
						} catch (Exception e) {
							Log.Error ("AsyncNetworkError", e.StackTrace);
						}
					}
					return true;
				}
			} catch (Exception e) {
				Log.Error ("AsyncNetworkError", e.StackTrace);
			}

			return false;
		}

		private void EventHandling(DataBaseWrapper db, JsonValue result) {
			if (result.ContainsKey ("events")) {
				JsonArray arr = (JsonArray) result ["events"];
				foreach (JsonValue v in arr) {
					var e = new Event () {
						type = v ["type"],
						msg = v ["msg"],
						nick = v ["nick"],
						text = v ["text"]
					};
					db.Insert (e);
				}
			}
		}


		private static string escape(string str)
		{
			string str2 = "0123456789ABCDEF";
			int length = str.Length;
			StringBuilder builder = new StringBuilder(length * 2);
			int num3 = -1;
			while (++num3 < length)
			{
				char ch = str[num3];
				int num2 = ch;
				if ((((0x41 > num2) || (num2 > 90)) &&
					((0x61 > num2) || (num2 > 0x7a))) &&
					((0x30 > num2) || (num2 > 0x39)))
				{
					switch (ch)
					{
					case '@':
					case '*':
					case '_':
					case '+':
					case '-':
					case '.':
					case '/':
						goto Label_0125;
					}
					builder.Append('%');
					if (num2 < 0x100)
					{
						builder.Append(str2[num2 / 0x10]);
						ch = str2[num2 % 0x10];
					}
					else
					{
						builder.Append('u');
						builder.Append(str2[(num2 >> 12) % 0x10]);
						builder.Append(str2[(num2 >> 8) % 0x10]);
						builder.Append(str2[(num2 >> 4) % 0x10]);
						ch = str2[num2 % 0x10];
					}
				}
				Label_0125:
				builder.Append(ch);
			}
			return builder.ToString();
		}
	}
}

