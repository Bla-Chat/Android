
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Json;

using System.Threading.Tasks;
using System.Net.Http;
using Android.Util;
using Android.Graphics;
using System.IO;
using System.Threading;
using System.Collections.Specialized;
using System.Net;

namespace BlaChat
{	
	public class AsyncNetwork
	{
        public static int MINI_PROFILE_SIZE = 100;
        public static int IMAGE_SIZE = 400;
		private static Object taskCacheLocker = new object();
		private static Semaphore semaphore = new Semaphore (1, 1);
		private Dictionary<string, Task<Bitmap>> bitmaps = new Dictionary<string, Task<Bitmap>>();
		private BackgroundService backgroundService = null;
		private Task<bool> updateEventTask = null;

		public void SetBackgroundService(BackgroundService bs) {
			backgroundService = bs;
		}

		public Task<Bitmap> GetImageBitmapFromUrl(string url, int reqWidth, int reqHeight)
		{
			lock (taskCacheLocker) {
				if (bitmaps.ContainsKey (url)) {
					return bitmaps [url];
				}
				return bitmaps[url] = GetImageBitmapFromUrlAsync(url, reqWidth, reqHeight);
			}
		}

		public Task<Bitmap> GetImageBitmapFromUrlNoCache (string url, int reqWidth, int reqHeight)
		{
			lock (taskCacheLocker) {
				return GetImageBitmapFromUrlAsync(url, reqWidth, reqHeight);
			}
		}

		private async Task<Bitmap> GetImageBitmapFromUrlAsync(string url, int reqWidth, int reqHeight) {
			Bitmap imageBitmap = null;

			string images = System.IO.Path.Combine (Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, "Pictures/BlaChat");

			Directory.CreateDirectory (images);

			string name = url.Substring (url.LastIndexOf ("/") + 1);

			string imageName = System.IO.Path.Combine (images, name);
			if (!File.Exists (imageName)) {
				byte[] imageBytes = null;
				for (int i = 0; i < 3 && imageBytes == null; i++) {
					semaphore.WaitOne();
					try {
						using(var httpClient = new HttpClient()) {
							imageBytes = await httpClient.GetByteArrayAsync (url);
						}
					} catch (Exception e) {
						//Log.Error ("BlaChat", e.StackTrace);
						//Log.Error ("BlaChat", "Image: " + url);
						imageBytes = null;
					} finally {
						semaphore.Release ();
					}

					if (imageBytes == null) {
						Thread.Sleep (1000);
					}
				}
				if (imageBytes == null) {
					return null;
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
					Log.Error ("BlaChat", e.StackTrace);
				}
			}
            if (File.Exists(imageName)) {
                BitmapFactory.Options options = GetBitmapOptionsOfImage(imageName);
                imageBitmap = LoadScaledDownBitmapForDisplay(imageName, options, reqWidth, reqHeight);
            }
			return imageBitmap;
		}

        private BitmapFactory.Options GetBitmapOptionsOfImage(string imageName)
        {
            BitmapFactory.Options options = new BitmapFactory.Options
            {
                InJustDecodeBounds = true
            };

            // The result will be null because InJustDecodeBounds == true.
            Bitmap imageBitmap = BitmapFactory.DecodeFile(imageName, options);
                        
            return options;
        }

        private static int CalculateInSampleSize(BitmapFactory.Options options, int reqWidth, int reqHeight)
        {
            // Raw height and width of image
            float height = options.OutHeight;
            float width = options.OutWidth;
            double inSampleSize = 1D;

            if (height > reqHeight || width > reqWidth)
            {
                int halfHeight = (int)(height / 2);
                int halfWidth = (int)(width / 2);

                // Calculate a inSampleSize that is a power of 2 - the decoder will use a value that is a power of two anyway.
                while ((halfHeight / inSampleSize) > reqHeight && (halfWidth / inSampleSize) > reqWidth)
                {
                    inSampleSize *= 2;
                }
            }

            return (int)inSampleSize;
        }

        private Bitmap LoadScaledDownBitmapForDisplay(string imageName, BitmapFactory.Options options, int reqWidth, int reqHeight)
        {
            // Calculate inSampleSize
            options.InSampleSize = CalculateInSampleSize(options, reqWidth, reqHeight);

            // Decode bitmap with inSampleSize set
            options.InJustDecodeBounds = false;

            return BitmapFactory.DecodeFile(imageName, options);
        }

        public async Task<string> Download(string url)
		{
			string contentsTask = null;
			while (contentsTask == null) {
				//semaphore.WaitOne();
				try {
					using(var httpClient = new HttpClient()) {
						contentsTask = await httpClient.GetStringAsync (url);
					}
				} catch (Exception e) {
					Log.Error ("BlaChat", e.StackTrace);
					contentsTask = null;
				} finally {
					//semaphore.Release ();
				}
				if (contentsTask == null) {
					Thread.Sleep (1000);
				}
			}
			return contentsTask;
		}

        private string getLatestTime(DataBaseWrapper db)
        {
            var x = db.Table<Chat>();
            string latest_conversation = x.OrderByDescending(e => e.time).First<Chat>().conversation;
            var y = db.Table<Message>();
            Message tmp = y.Where(q => q.conversation == latest_conversation).OrderBy(e => e.time).Last<Message>();
            string lastid = String.Format("\"time\":\"{0}\"", tmp.time);
            return lastid;
        }

		private async Task<bool> CommonNetworkOperations(DataBaseWrapper db, String request, User user, String actionKey, Action<JsonValue> action) {
            
            string encodedJson = escape (String.Format ("{{\"id\":\"{0}\", {1}, {2}}}", user.id, request, getLatestTime(db)));
			var result = JsonValue.Parse (await Download (user.server + "/xjcp2.php?msg=" + encodedJson));
			bool success = false;
			try {
				if (result.ContainsKey (actionKey)) {
					action.Invoke(result [actionKey]);
					success = true;
				}
			} catch (Exception ex) {
				Log.Error ("BlaChat", ex.StackTrace);
			}

			return success && await EventHandling (db, result) == 0;
		}

		public async Task<bool> Authenticate(DataBaseWrapper db, User user)
		{
			string encodedJson = escape (String.Format ("{{\"user\":\"{0}\", \"pw\":\"{1}\"}}", user.user, user.password));
			bool success = false;

			var result = JsonValue.Parse (await Download (user.server + "/xjcp2.php?msg=" + encodedJson));
			try {
				if (result.ContainsKey ("id")) {
					user.id = result ["id"];
					db.Update (user);
					success = true;
				}
			} catch (Exception ex) {
				Log.Error ("BlaChat", ex.StackTrace);
			} finally {
				EventHandling (db, result);
			}

			return success && await EventHandling (db, result) == 0;
		}

		public Task<bool> UpdateChats(DataBaseWrapper db, User user)
		{
			string request = String.Format ("\"getChats\":{{}}");

			return CommonNetworkOperations(db, request, user, "onGetChats", x => {
				JsonArray arr = (JsonArray)x;
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
			});
		}

		public Task<bool> UpdateContacts(DataBaseWrapper db, User user)
		{
			string request = String.Format ("\"getContacts\":{{}}");

			return CommonNetworkOperations (db, request, user, "onGetContacts", x => {
				JsonArray arr = (JsonArray)x;
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
			});
		}

		public Task<bool> SendMessage(DataBaseWrapper db, User user, Chat chat, string message)
		{
			message = message.Replace ("\\", "\\\\");
			message = message.Replace ("\"", "\\\"");
			string request = String.Format ("\"message\":{{\"conversation\":\"{0}\", \"message\":\"{1}\"}}", chat.conversation, message);
			return CommonNetworkOperations (db, request, user, "onMessage", x => { return; });
		}

		public async Task<bool> SendImage (DataBaseWrapper db, User user, Chat chat, Bitmap bitmap)
		{
			string encodedJson = escape (String.Format ("{{\"id\":\"{0}\", \"data\":{{\"conversation\":\"{1}\"}}, {2}}}", user.id, chat.conversation, getLatestTime(db)));
			var success = false;
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("msg", encodedJson);
			string tmp = null;
			while (tmp == null) {
				tmp = HttpUploadFile (user.server + "/xjcp2.php", bitmap, "uploadedfile", "image/png", nvc);
			}
			var result = JsonValue.Parse (tmp);

			try {
				success = 0 == await EventHandling (db, result);

				if (!result.ContainsKey ("onData")) {
					success = false;
				}
			} catch (Exception ex) {
				Log.Error ("BlaChat", ex.StackTrace);
			}

			return success;
		}

        private String HttpUploadFile(string url, Bitmap img, string paramName, string contentType, NameValueCollection nvc)
        {
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
            wr.ContentType = "multipart/form-data; boundary=" + boundary;
            wr.Method = "POST";
            wr.KeepAlive = true;
            wr.Credentials = CredentialCache.DefaultCredentials;

            Stream rs = wr.GetRequestStream();

            string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
            foreach (string key in nvc.Keys)
            {
                rs.Write(boundarybytes, 0, boundarybytes.Length);
                string formitem = string.Format(formdataTemplate, key, nvc[key]);
                byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                rs.Write(formitembytes, 0, formitembytes.Length);
            }
            rs.Write(boundarybytes, 0, boundarybytes.Length);

            string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
			string header = string.Format(headerTemplate, paramName, Guid.NewGuid() + ".png", contentType);
            byte[] headerbytes = Encoding.UTF8.GetBytes(header);
            rs.Write(headerbytes, 0, headerbytes.Length);

			img.Compress (Bitmap.CompressFormat.Png, 100, rs);

            byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
            rs.Write(trailer, 0, trailer.Length);
            rs.Close();

            WebResponse wresp = null;
            try
            {
                wresp = wr.GetResponse();
                Stream stream2 = wresp.GetResponseStream();
                StreamReader reader2 = new StreamReader(stream2);
				return reader2.ReadToEnd();
            }
            catch (Exception ex)
            {
                if (wresp != null)
                {
                    wresp.Close();
                    wresp = null;
                }
				return null;
            }
            finally
            {
                wr = null;
            }
        }

		public Task<bool> UpdateEvents(DataBaseWrapper db, User user)
		{
			lock (taskCacheLocker) {
				if (updateEventTask == null) {
					updateEventTask = InternalUpdateEvents(db, user);
				}
				return updateEventTask;
			}
		}

		private async Task<bool> InternalUpdateEvents(DataBaseWrapper db, User user) {
            string encodedJson = escape (String.Format("{{\"id\":\"{0}\", {1}}}", user.id, getLatestTime(db)));
            
            var result = JsonValue.Parse (await Download(user.server + "/xjcp2.php?msg=" + encodedJson));
			var success = false;
			try {
				success = 0 == await EventHandling (db, result);
			} catch (Exception ex) {
				Log.Error ("BlaChat", ex.StackTrace);
			}

			updateEventTask = null;

			return success;
		}

		public async Task<bool> RemoveEvent(DataBaseWrapper db, User user, Event e)
		{
			string encodedJson = escape(String.Format("{{\"id\":\"{0}\", \"removeEvent\":{{\"conversation\":\"{1}\"}}, {2}}}", user.id, e.msg, getLatestTime(db)));

			var result = JsonValue.Parse(await Download(user.server + "/xjcp2.php?msg=" + encodedJson));
			var success = false;
			try {
				success = 0 == await EventHandling (db, result);
			} catch (Exception ex) {
				Log.Error ("BlaChat", ex.StackTrace);
			}

			return success;
		}

		public async Task<bool> NewChat(DataBaseWrapper db, User user, List<User> users, string name)
		{
			return true;
		}

		public async Task<bool> RenameChat(DataBaseWrapper db, User user, Chat chat, string name)
		{
			return true;
		}

		public async Task<bool> SetName(DataBaseWrapper db, User user, string name)
		{
			return true;
		}

		public async Task<bool> AddFriend(DataBaseWrapper db, User user, string name)
		{
			return true;
		}
			
		public async Task<bool> SetStatus(DataBaseWrapper db, User user, string status)
		{
			return true;
		}

		public async Task<bool> SetProfileImage(DataBaseWrapper db, User user, object image)
		{
			return true;
		}

		public async Task<bool> SetGroupImage(DataBaseWrapper db, User user, Chat chat, object image)
		{
			return true;
		}

		public async Task<bool> InjectEvent(DataBaseWrapper db, User user, Event e)
		{
			return true;
		}

		public async Task<bool> Data(DataBaseWrapper db, User user, Chat chat, object data)
		{
			return true;
		}

		public Task<bool> UpdateHistory(DataBaseWrapper db, User user, Chat chat, int count)
		{
			string request = String.Format ("\"getHistory\":{{\"conversation\":\"{0}\", \"count\":\"{1}\"}}", chat.conversation, count);
			return CommonNetworkOperations (db, request, user, "onGetHistory", arr => {
				var msgs = arr ["messages"];
				string conversation = arr ["conversation"];

				foreach (JsonValue x in msgs) {
					try {
						var msg = new Message ();
						msg.conversation = conversation;
						msg.author = x ["author"];
						msg.nick = x ["nick"];
						msg.text = x ["text"];
						msg.time = x ["time"];
						db.InsertIfNotContains<Message>(msg);
					} catch (Exception e) {
						Log.Error ("BlaChat", e.StackTrace);
					}
				}
			});
		}

		private async Task<int> EventHandling(DataBaseWrapper db, JsonValue result) {
			if (result.ContainsKey ("events")) {
				JsonArray arr = (JsonArray) result ["events"];
				foreach (JsonValue v in arr) {
					var e = new Event () {
						type = v ["type"],
						msg = v ["msg"],
						nick = v ["nick"],
						text = v ["text"],
						time = v ["time"],
						author = v ["author"]
					};
					db.Insert (e);
				}

				if (backgroundService != null) {
					await backgroundService.UpdateNotifications();
				}
			}
			if (result.ContainsKey ("onLoginError")) {
				return 1;
			}
			return 0;
		}


		private static string escape(string str)
		{
			return Uri.EscapeDataString (str);
		}
	}
}

