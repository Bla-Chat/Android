
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

namespace BlaChat
{
	[Activity (Label = "Settings")]			
	public class SettingsActivity : Activity
	{
		DataBaseWrapper db = null;
		User user = null;
		Setting setting = null;

		protected override void OnCreate (Bundle bundle)
		{
			db = new DataBaseWrapper (this.Resources);
			if ((setting = db.Table<Setting> ().FirstOrDefault ()) == null) {
				db.Insert(setting = new Setting ());
			}
			SetTheme (setting.Theme);
			base.OnCreate (bundle);

			db = new DataBaseWrapper (this.Resources);
			user = db.Table<User>().FirstOrDefault ();

			if (setting.FontSize == Setting.Size.large) {
				SetContentView (Resource.Layout.SettingsLarge);
			} else {
				SetContentView (Resource.Layout.Settings);
			}

			InitializeView();
		}

		void InitializeView ()
		{
			var smallFont = FindViewById<RadioButton> (Resource.Id.smallfont);
			var mediumFont = FindViewById<RadioButton> (Resource.Id.mediumfont);
			var largeFont = FindViewById<RadioButton> (Resource.Id.largefont);

			smallFont.Checked = setting.FontSize == Setting.Size.small;
			mediumFont.Checked = setting.FontSize == Setting.Size.medium;
			largeFont.Checked = setting.FontSize == Setting.Size.large;

			largeFont.CheckedChange += delegate { if (largeFont.Checked) { setting.FontSize = Setting.Size.large; db.Update(setting); }};
			mediumFont.CheckedChange += delegate { if (mediumFont.Checked) { setting.FontSize = Setting.Size.medium; db.Update(setting); }};
			smallFont.CheckedChange += delegate { if (smallFont.Checked) { setting.FontSize = Setting.Size.small; db.Update(setting); }};

			var lightTheme = FindViewById<RadioButton> (Resource.Id.lighttheme);
			var darkTheme = FindViewById<RadioButton> (Resource.Id.darktheme);
			var materialTheme = FindViewById<RadioButton> (Resource.Id.materialtheme);
			var materialThemeDark = FindViewById<RadioButton> (Resource.Id.materialthemeDark);

			lightTheme.Checked = setting.Theme == Android.Resource.Style.ThemeHoloLight;
			darkTheme.Checked = setting.Theme == Android.Resource.Style.ThemeHolo;
			materialTheme.Checked = setting.Theme == Android.Resource.Style.ThemeMaterialLight;
			materialThemeDark.Checked = setting.Theme == Android.Resource.Style.ThemeMaterial;

			lightTheme.CheckedChange += delegate { if (lightTheme.Checked) {setting.Theme = Android.Resource.Style.ThemeHoloLight; db.Update(setting); } };
			darkTheme.CheckedChange += delegate { if (darkTheme.Checked) { setting.Theme = Android.Resource.Style.ThemeHolo; db.Update(setting); }};
			materialTheme.CheckedChange += delegate { if (materialTheme.Checked) { setting.Theme = Android.Resource.Style.ThemeMaterialLight; db.Update(setting); }};
			materialThemeDark.CheckedChange += delegate { if (materialThemeDark.Checked) { setting.Theme = Android.Resource.Style.ThemeMaterial; db.Update(setting); }};

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
		}
	}
}

