
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
	[Activity (Label = "Settings")]			
	public class SettingsActivity : Activity
	{
		DataBaseWrapper db = null;
		User user = null;
		Setting setting = null;
		AsyncNetwork network;

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
				SetContentView (Resource.Layout.SettingsLarge);
			} else {
				SetContentView (Resource.Layout.Settings);
			}

			InitializeView();
		}

		void Refresh() {
			Finish();
			StartActivity(new Intent(this, typeof(SettingsActivity)));
		}

		void InitializeView ()
		{
			var smallFont = FindViewById<RadioButton> (Resource.Id.smallfont);
			var mediumFont = FindViewById<RadioButton> (Resource.Id.mediumfont);
			var largeFont = FindViewById<RadioButton> (Resource.Id.largefont);

			smallFont.Checked = setting.FontSize == Setting.Size.small;
			mediumFont.Checked = setting.FontSize == Setting.Size.medium;
			largeFont.Checked = setting.FontSize == Setting.Size.large;

			largeFont.CheckedChange += delegate { if (largeFont.Checked) { setting.FontSize = Setting.Size.large; db.Update(setting); Refresh(); }};
			mediumFont.CheckedChange += delegate { if (mediumFont.Checked) { setting.FontSize = Setting.Size.medium; db.Update(setting); Refresh(); }};
			smallFont.CheckedChange += delegate { if (smallFont.Checked) { setting.FontSize = Setting.Size.small; db.Update(setting); Refresh(); }};

			var lightTheme = FindViewById<RadioButton> (Resource.Id.lighttheme);
			var darkTheme = FindViewById<RadioButton> (Resource.Id.darktheme);
			var materialTheme = FindViewById<RadioButton> (Resource.Id.materialtheme);
			var materialThemeDark = FindViewById<RadioButton> (Resource.Id.materialthemeDark);
			var materialThemeBlue = FindViewById<RadioButton> (Resource.Id.materialthemeBlue);
			var materialThemeDarkBlue = FindViewById<RadioButton> (Resource.Id.materialthemeDarkBlue);
			var materialThemeGreen = FindViewById<RadioButton> (Resource.Id.materialthemeGreen);
			var materialThemeDarkGreen = FindViewById<RadioButton> (Resource.Id.materialthemeDarkGreen);

			lightTheme.Checked = setting.Theme == Resource.Style.LightHolo;
			darkTheme.Checked = setting.Theme == Resource.Style.DarkHolo;
			lightTheme.CheckedChange += delegate { if (lightTheme.Checked) {setting.Theme = Resource.Style.LightHolo; db.Update(setting);  Refresh();} };
			darkTheme.CheckedChange += delegate { if (darkTheme.Checked) { setting.Theme = Resource.Style.DarkHolo; db.Update(setting);  Refresh();}};

			if ((int)Android.OS.Build.VERSION.SdkInt >= 21) {
				materialTheme.Checked = setting.Theme == Resource.Style.LightMaterial;
				materialThemeDark.Checked = setting.Theme == Resource.Style.DarkMaterial;
				materialTheme.CheckedChange += delegate { if (materialTheme.Checked) { setting.Theme = Resource.Style.LightMaterial; db.Update(setting);  Refresh();}};
				materialThemeDark.CheckedChange += delegate { if (materialThemeDark.Checked) { setting.Theme = Resource.Style.DarkMaterial; db.Update(setting);  Refresh();}};

				materialThemeBlue.Checked = setting.Theme == Resource.Style.LightBlueMaterial;
				materialThemeDarkBlue.Checked = setting.Theme == Resource.Style.DarkBlueMaterial;
				materialThemeBlue.CheckedChange += delegate { if (materialThemeBlue.Checked) { setting.Theme = Resource.Style.LightBlueMaterial; db.Update(setting);  Refresh();}};
				materialThemeDarkBlue.CheckedChange += delegate { if (materialThemeDarkBlue.Checked) { setting.Theme = Resource.Style.DarkBlueMaterial; db.Update(setting);  Refresh();}};

				materialThemeGreen.Checked = setting.Theme == Resource.Style.LightGreenMaterial;
				materialThemeDarkGreen.Checked = setting.Theme == Resource.Style.DarkGreenMaterial;
				materialThemeGreen.CheckedChange += delegate { if (materialThemeGreen.Checked) { setting.Theme = Resource.Style.LightGreenMaterial; db.Update(setting);  Refresh();}};
				materialThemeDarkGreen.CheckedChange += delegate { if (materialThemeDarkGreen.Checked) { setting.Theme = Resource.Style.DarkGreenMaterial; db.Update(setting);  Refresh();}};
			} else {
				materialTheme.Visibility = ViewStates.Gone;
				materialThemeDark.Visibility = ViewStates.Gone;

				materialThemeBlue.Visibility = ViewStates.Gone;
				materialThemeDarkBlue.Visibility = ViewStates.Gone;

				materialThemeGreen.Visibility = ViewStates.Gone;
				materialThemeDarkGreen.Visibility = ViewStates.Gone;
			}

			var readMessages = FindViewById<CheckBox> (Resource.Id.readMessages);
			readMessages.Checked = setting.ReadMessagesEnabled;
			readMessages.CheckedChange += delegate { setting.ReadMessagesEnabled = readMessages.Checked; db.Update(setting); };
			var visibleMessages = FindViewById<EditText> (Resource.Id.visibleMessages);
			visibleMessages.Text = setting.VisibleMessages.ToString();
			visibleMessages.TextChanged += delegate { try { setting.VisibleMessages = int.Parse(visibleMessages.Text); db.Update(setting); } catch (Exception) { setting.VisibleMessages = 0; db.Update(setting); } };

			var notifications = FindViewById<CheckBox> (Resource.Id.notifications);
			var vibrate = FindViewById<CheckBox> (Resource.Id.vibrate);
			var sound = FindViewById<CheckBox> (Resource.Id.sound);
			var led = FindViewById<CheckBox> (Resource.Id.led);

			notifications.Checked = setting.Notifications;
			vibrate.Checked = setting.Vibrate;
			sound.Checked = setting.Sound;
			led.Checked = setting.Led;

			notifications.CheckedChange += delegate { setting.Notifications = notifications.Checked; db.Update(setting); };
			vibrate.CheckedChange += delegate { setting.Vibrate = vibrate.Checked; db.Update(setting); };
			sound.CheckedChange += delegate { setting.Sound = sound.Checked; db.Update(setting); };
			led.CheckedChange += delegate { setting.Led = led.Checked; db.Update(setting); };

			var highSync = FindViewById<RadioButton> (Resource.Id.highsync);
			var normalSync = FindViewById<RadioButton> (Resource.Id.normalsync);
			var lowSync = FindViewById<RadioButton> (Resource.Id.lowsync);
			var wlanSync = FindViewById<RadioButton> (Resource.Id.wlansync);

			highSync.Checked = setting.Synchronisation == Setting.Frequency.often;
			normalSync.Checked = setting.Synchronisation == Setting.Frequency.normal;
			lowSync.Checked = setting.Synchronisation == Setting.Frequency.rare;
			wlanSync.Checked = setting.Synchronisation == Setting.Frequency.wlan;

			highSync.CheckedChange += delegate { if (highSync.Checked) {setting.Synchronisation = Setting.Frequency.often; db.Update(setting); } };
			normalSync.CheckedChange += delegate { if (normalSync.Checked) {setting.Synchronisation = Setting.Frequency.normal; db.Update(setting); } };
			lowSync.CheckedChange += delegate { if (lowSync.Checked) {setting.Synchronisation = Setting.Frequency.rare; db.Update(setting); } };
			wlanSync.CheckedChange += delegate { if (wlanSync.Checked) {setting.Synchronisation = Setting.Frequency.wlan; db.Update(setting); } };

			var server = FindViewById<TextView> (Resource.Id.server);
			var nickname = FindViewById<TextView> (Resource.Id.nickname);
			var changeProfile = FindViewById<ImageButton> (Resource.Id.changeProfile);
			var savechanges = FindViewById<Button> (Resource.Id.save);
			var logout = FindViewById<Button> (Resource.Id.logout);
			var name = FindViewById<EditText> (Resource.Id.name);

			if (user != null) {
				server.Text = user.server;
				nickname.Text = user.user;
				name.Text = user.name;
				logout.Click += delegate {
					db.DropUserSpecificData();
					var intent = new Intent (this, typeof(MainActivity));
					intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
					Finish();
					StartActivity (intent);
				};
				savechanges.Click += delegate {
					Toast.MakeText(this, "Not implemented yet.", ToastLength.Long).Show();
				};
				changeProfile.Click += delegate {
					Toast.MakeText(this, "Not implemented yet.", ToastLength.Long).Show();
				};
				new Thread (async () => {
					try {
						var imageBitmap = await network.GetImageBitmapFromUrl (Resources.GetString (Resource.String.profileUrl) + user.user + ".png");
						RunOnUiThread (() => changeProfile.SetImageBitmap (imageBitmap));
					} catch (Exception e) {
						Log.Error ("BlaChat", e.StackTrace);
					}
				}).Start ();
			} else {
				server.Text = "none";
				nickname.Text = "none";
				name.Enabled = false;
				changeProfile.Visibility = ViewStates.Gone;
				savechanges.Visibility = ViewStates.Gone;
				logout.Visibility = ViewStates.Gone;
			}

			var currentVersion = FindViewById<TextView> (Resource.Id.version);
			var newestVersion = FindViewById<TextView> (Resource.Id.newestVersion);


			currentVersion.Text = Setting.CurrentVersion;
			if (setting.NewestVersion != null && !setting.NewestVersion.StartsWith (Setting.CurrentVersion)) {
				newestVersion.TextFormatted = SpannableTools.GetSmiledText (this, new SpannableString(setting.NewestVersion));
			} else {
				newestVersion.Text = Setting.CurrentVersion;
			}
		}
	}
}

