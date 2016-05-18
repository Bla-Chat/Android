using System.Collections.Generic;
using Android.Content;
using Android.Text;
using Android.Text.Style;
using Java.Lang;

namespace BlaChat
{
	public static class SmileyTools
    {
        private static readonly Dictionary<string, string> EmoticonsUTF8;

        static SmileyTools()
		{
            EmoticonsUTF8 = new Dictionary<string, string>
            {
                {":D", "😁"},
                {":'D", "😂"},
                {":)", "😃"},
                {";D", "😄"},
                {";'D", "😅"},
                {"xD", "😆"},
                {";)", "😉"},
                {"x)", "😊"},
                {";p", "😋"},
                {"zZz", "😌"},
                {":**", "😍"},
                {":/", "😏"},
                {":|", "😒"},
                {":'|", "😓"},
                {"*pensive*", "😔"},
                {":{", "😖"},
                {":*", "😘"},
                {":x", "😚"},
                {":p", "😜"},
                {"xP", "😝"},
                {":(", "😞"},
                {":[", "😠"},
                {"*angry*", "😡"},
                {":'(", "😢"},
                {"x(", "😣"},
                {"*triumph*", "😤"},
                {"*relieved*", "😥"},
                {"*fear*", "😨"},
                {"*weary*", "😩"},
                {"zzZZzz", "😪"},
                {"*tired*", "😫"},
                {"*cry*", "😭"},
                {":'o", "😰"},
                {":o", "😱"},
                {"o.O", "😲"},
                {"*flushed*", "😳"},
                {"*dizzy*", "😵"},
                {"*medical*", "😷"},
                {"*grin_cat*", "😸"},
                {"*tears_of_joy_cat*", "😹"},
                {"*happy_cat*", "😺"},
                {"*love_cat*", "😻"},
                {"*wry_cat*", "😼"},
                {"*kiss_cat*", "😽"},
                {"*pouting_cat*", "😾"},
                {"*crying_cat*", "😿"},
                {"*weary_cat*", "🙀"},
                {"*nope*", "🙅"},
                {"*ok*", "🙆"},
                {"*bow*", "🙇"},
                {"*see_no_evil*", "🙈"},
                {"*hear_no_evil*", "🙉"},
                {"*speak_no_evil*", "🙊"},
                {"*hi*", "🙋"},
                {"*celebrate*", "🙌"},
                {"*frown*", "🙍"},
                {"*pout*", "🙎"},
                {"*evil*", "🙏"},
                {"<3", "💕"}
            };
        }

		public static string GetSmiledTextUTF(string text)
        {
            foreach (var entry in EmoticonsUTF8)
            {
                var smiley = entry.Key;
                var smileyUTF = entry.Value;
                string[] segments = text.Split();
                string res = "";
                foreach (var x in segments)
                {
                    if (x.Equals(smiley))
                    {
                        res += " " + smileyUTF;
                    }
                    else
                    {
                        res += " " + x;
                    }
                }
                text = res.Substring(1);
            }
            return text;
        }
	}

	//Taken from http://stackoverflow.com/a/767788/368379
	public static class StringExtensions
	{
		public static IEnumerable<int> IndexesOf(this string haystack, string needle)
		{
			var lastIndex = 0;
			while (true)
			{
				var index = haystack.IndexOf(needle, lastIndex);
				if (index == -1)
				{
					yield break;
				}
				yield return index;
				lastIndex = index + needle.Length;
			}
		}
	}
}