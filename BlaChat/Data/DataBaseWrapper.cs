using System;
using SQLite;
using System.IO;
using Android.Content.Res;
using System.Collections.Generic;
using Android.Util;

namespace BlaChat
{
	public class DataBaseWrapper
	{
		private static readonly object locker = new object();
		private static SQLiteConnection db;

		public DataBaseWrapper (Resources resources)
		{
			var sqliteFilename = "bla.db3";

			// FIXME: Allow this once there is a correct Raw.data.sqlite
			//DataBaseWrapper.TryInit (resources, sqliteFilename);

			#if __ANDROID__
			// Just use whatever directory SpecialFolder.Personal returns
			string libraryPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal); ;
			#else
			// we need to put in /Library/ on iOS5.1 to meet Apple's iCloud terms
			// (they don't want non-user-generated data in Documents)
			string documentsPath = Environment.GetFolderPath (Environment.SpecialFolder.Personal); // Documents folder
			string libraryPath = Path.Combine (documentsPath, "..", "Library"); // Library folder instead
			#endif

			if (db == null) {
				db = new SQLiteConnection (Path.Combine (libraryPath, sqliteFilename));
			}

			// FIXME: Remove this once loading db is done correctly.
			// Create the user table if it doesn't already exist.
			db.CreateTable<User> ();
			db.CreateTable<Event> ();
			db.CreateTable<Message> ();
			db.CreateTable<Contact> ();
			db.CreateTable<Chat> ();
			db.CreateTable<Setting> ();
		}

		private static void TryInit (Resources resources, string sqliteFilename) {
			lock (locker) {
				#if __ANDROID__
				var docFolder = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
				var dbFile = Path.Combine (docFolder, sqliteFilename); // FILE NAME TO USE WHEN COPIED
				if (!System.IO.File.Exists (dbFile)) {
					var s = resources.OpenRawResource (Resource.Raw.data);  // DATA FILE RESOURCE ID
					FileStream writeStream = new FileStream (dbFile, FileMode.OpenOrCreate, FileAccess.Write);
					int Length = 256;
					Byte[] buffer = new Byte[Length];
					int bytesRead = s.Read (buffer, 0, Length);
					// write the required bytes
					while (bytesRead > 0) {
						writeStream.Write (buffer, 0, bytesRead);
						bytesRead = s.Read (buffer, 0, Length);
					}
					s.Close ();
					writeStream.Close ();
				}
				#else
				// Copy the database across (if it doesn't exist)
				var appdir = NSBundle.MainBundle.ResourcePath;
				var seedFile = Path.Combine (appdir, sqliteFilename);
				if (!File.Exists (Database.DatabaseFilePath))
					File.Copy (seedFile, Database.DatabaseFilePath);
				#endif
			}
		}

		public void DropUserSpecificData() {
			lock (locker) {
				db.DeleteAll<User> ();
				db.DeleteAll<Event> ();
				db.DeleteAll<Message> ();
				db.DeleteAll<Contact> ();
				db.DeleteAll<Chat> ();
			}
		}

		public void CreateTable<T> () {
			lock (locker) {
				try {
					db.CreateTable<T> ();
				} catch(SQLiteException e) {
					Log.Error("BlaChat", e.StackTrace);
				}
			}
		}

		public void Insert(object elem) {
			lock (locker) {
				try {
					db.Insert (elem);
				} catch(SQLiteException e) {
					Log.Error("BlaChat", e.StackTrace);
				}
			}
		}

		public void Update(object elem) {
			lock (locker) {
				try {
					db.Update (elem);
				} catch(SQLiteException e) {
					Log.Error("BlaChat", e.StackTrace);
				}
			}
		}

		public T Get<T>(object primaryKeyId) where T : new()  {
			lock (locker) {
				try {
					return db.Get<T> (primaryKeyId);
				} catch(Exception e) {
					Log.Error("BlaChat", e.StackTrace);
					return default(T);
				}
			}
		}

		public List<T> Query<T>(string query, object[] args) where T : new() {
			lock (locker) {
				try {
					return db.Query<T>(query, args);
				} catch(Exception e) {
					Log.Error("BlaChat", e.StackTrace);
					return null;
				}
			}
		}

		public int Delete<T>(object primaryKeyId) {
			lock (locker) {
				try {
					return db.Delete<T> (primaryKeyId);
				} catch(SQLiteException e) {
					Log.Error("BlaChat", e.StackTrace);
					return 0;
				}
			}
		}

		public TableQuery<T> Table<T>() where T : new() {
			lock (locker) {
				try {
					return db.Table<T>();
				} catch(SQLiteException e) {
					Log.Error("BlaChat", e.StackTrace);
					return null;
				}
			}
		}

		public TableQuery<T> Table<T>(T probe) where T : IEqualsExpression<T>, new() {
			lock (locker) {
				return Table<T> ().Where (probe.EqualsExpression ());
			}
		}

		public bool Contains<T>(T probe) where T : IEqualsExpression<T>, new() {
			lock (locker) {
				try {
					return Table<T> (probe).FirstOrDefault() != null;
				} catch (Exception e) {
					Log.Error("BlaChat", e.StackTrace);
					return false;
				}
			}
		}

		public void InsertIfNotContains<T>(T probe) where T : IEqualsExpression<T>, new()
		{
			lock (locker) {
				if (!Contains<T> (probe)) {
					Insert (probe);
				}
			}
		}
	}
}

