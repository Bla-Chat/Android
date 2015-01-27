
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

namespace BlaChat
{	
	public class AsyncNetwork
	{
		private HttpClient httpClient;

		public AsyncNetwork()
		{
			httpClient = new HttpClient();
		}

		public async Task<string> Download(string url)
		{
			Task<string> contentsTask = httpClient.GetStringAsync(url);
			return await contentsTask;
		}

		public async Task<bool> Authenticate(DataBaseWrapper db, User user)
		{
			string encodedJson = escape(String.Format("{{\"user\":\"{0}\", \"pw\":\"{1}\"}}", user.user, user.password));

			var result = JsonValue.Parse(await Download(user.server + "/xjcp.php?msg=" + encodedJson));

			EventHandling (db, result);

			if (result.ContainsKey ("id")) {
				user.id = result ["id"];
				db.Update (user);
				return true;
			}

			return false;
		}

		public async Task<bool> UpdateChats(DataBaseWrapper db, User user)
		{
			string encodedJson = escape(String.Format("{{\"id\":\"{0}\", \"getChats\":{{}}}}", user.id));

			var result = JsonValue.Parse(await Download(user.server + "/xjcp.php?msg=" + encodedJson));

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

			return false;
		}

		public async Task<bool> UpdateContacts(DataBaseWrapper db, User user)
		{
			string encodedJson = escape(String.Format("{{\"id\":\"{0}\", \"getContacts\":{{}}}}", user.id));

			var result = JsonValue.Parse(await Download(user.server + "/xjcp.php?msg=" + encodedJson));

			EventHandling (db, result);

			if (result.ContainsKey ("onGetContacts")) {
				JsonArray arr = (JsonArray) result ["onGetContacts"];
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

			return false;
		}

		public async Task<bool> SendMessage(DataBaseWrapper db, User user, Chat chat, string message)
		{
			string encodedJson = escape(String.Format("{{\"id\":\"{0}\", \"message\":{{\"conversation\":\"{1}\", \"message\":\"{2}\"}}}}", user.id, chat.conversation, message));

			var result = JsonValue.Parse(await Download(user.server + "/xjcp.php?msg=" + encodedJson));

			EventHandling (db, result);

			if (result.ContainsKey ("onMessage")) {
				return true;
			}

			return false;
		}

		public async Task<bool> UpdateEvents(DataBaseWrapper db, User user)
		{
			string encodedJson = escape(String.Format("{{\"id\":\"{0}\"}}", user.id));

			var result = JsonValue.Parse(await Download(user.server + "/xjcp.php?msg=" + encodedJson));

			EventHandling (db, result);

			return true;
		}

		public async Task<bool> RemoveEvent(DataBaseWrapper db, User user, Event e)
		{
			string encodedJson = escape(String.Format("{{\"id\":\"{0}\", \"removeEvent\":{{\"conversation\":\"{1}\"}}}}", user.id, e.msg));

			var result = JsonValue.Parse(await Download(user.server + "/xjcp.php?msg=" + encodedJson));

			EventHandling (db, result);

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

			EventHandling (db, result);

			if (result.ContainsKey ("onGetHistory")) {
				var arr = result ["onGetHistory"];
				//foreach (JsonValue v in arr) {
				var msgs = arr ["messages"];
				string conversation = arr ["conversation"];

				foreach (JsonValue x in msgs) {
					try {
						bool found = false;
						foreach (var s in db.Table<Message>()) {
							if (s.nick == x["nick"] && s.conversation == conversation  && s.author == x["author"]  && s.text == x["text"]  && s.time == x["time"]) {
								found = true;
								break;
							}
						}
						if (!found) {
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
				//}
				return true;
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

