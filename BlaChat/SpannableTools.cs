using System.Collections.Generic;
using Android.Content;
using Android.Text;
using Android.Text.Style;
using Java.Lang;

namespace BlaChat
{
	public static class SpannableTools
	{
		private static readonly Dictionary<string, int> Emoticons;

		static SpannableTools()
		{
			Emoticons = new Dictionary<string, int>
			{
				{"O:)", Resource.Drawable.angel},
				{"O:-)", Resource.Drawable.angel},
				{">:|", Resource.Drawable.angry},
				{">:-|", Resource.Drawable.angry},
				{">:/", Resource.Drawable.angry},
				{">:-/", Resource.Drawable.angry},
				{">:(", Resource.Drawable.angry},
				{">:-(", Resource.Drawable.angry},
				{";)", Resource.Drawable.blink},
				{";-)", Resource.Drawable.blink},
				{"B)", Resource.Drawable.cool},
				{"B-)", Resource.Drawable.cool},
				{":'(", Resource.Drawable.cry},
				{":'-(", Resource.Drawable.cry},
				{"8:(", Resource.Drawable.girl},
				{"8:-(", Resource.Drawable.girl},
				{"*hug*", Resource.Drawable.hug},
				{":hug", Resource.Drawable.hug},
				{"*idea*", Resource.Drawable.idea},
				{":idea", Resource.Drawable.idea},
				{"*ill*", Resource.Drawable.ill},
				{":ill", Resource.Drawable.ill},
				{"lol", Resource.Drawable.laugh},
				{"LOL", Resource.Drawable.laugh},
				{"*looser*", Resource.Drawable.looser},
				{":looser", Resource.Drawable.looser},
				{":*", Resource.Drawable.love},
				{":-*", Resource.Drawable.love},
				{"%)", Resource.Drawable.mad},
				{"%-)", Resource.Drawable.mad},
				{"[:)", Resource.Drawable.music},
				{"[:-)", Resource.Drawable.music},
				{":X", Resource.Drawable.mute},
				{":-X", Resource.Drawable.mute},
				{":|", Resource.Drawable.neutral},
				{":-|", Resource.Drawable.neutral},
				{":/", Resource.Drawable.neutral},
				{":-/", Resource.Drawable.neutral},
				{":party", Resource.Drawable.party},
				{"*party*", Resource.Drawable.party},
				{":pew", Resource.Drawable.pew},
				{"*pew*", Resource.Drawable.pew},
				{":scratch", Resource.Drawable.scratch},
				{"*scratch*", Resource.Drawable.scratch},
				{":O", Resource.Drawable.sick},
				{":-O", Resource.Drawable.sick},
				{"*zzz*", Resource.Drawable.sleep},
				{"zzz", Resource.Drawable.sleep},
				{"8)", Resource.Drawable.smart},
				{"8-)", Resource.Drawable.smart},
				{":D", Resource.Drawable.smile},
				{":-D", Resource.Drawable.smile},
				{"xD", Resource.Drawable.smile},
				{"x-D", Resource.Drawable.smile},
				{":thumbs up", Resource.Drawable.thumbs},
				{"*thumbs up*", Resource.Drawable.thumbs},
				{":tired", Resource.Drawable.tired},
				{"*tired*", Resource.Drawable.tired},
				{":p", Resource.Drawable.tongue},
				{":-p", Resource.Drawable.tongue},
				{":P", Resource.Drawable.tongue},
				{":-P", Resource.Drawable.tongue},
				{"o.o", Resource.Drawable.watch},
				{"-.-", Resource.Drawable.watchtired},
				{":(", Resource.Drawable.sad},
				{":-(", Resource.Drawable.sad},
				{"=)", Resource.Drawable.happy},
				{"=-)", Resource.Drawable.happy},
				{":)", Resource.Drawable.happy},
				{":-)", Resource.Drawable.happy}
			};
		}

		// Taken from https://gist.githubusercontent.com/Cheesebaron/5034440/raw/f962c41c95f8d94457ef9a60e19fe7efa2a50d61/SpannableTools.cs
		public static bool AddSmiles(Context context, ISpannable spannable)
		{
			var hasChanges = false;
			foreach (var entry in Emoticons)
			{
				var smiley = entry.Key;
				var smileyImage = entry.Value;
				var indices = spannable.ToString().IndexesOf(smiley);
				foreach (var index in indices)
				{
					var set = true;
					foreach (ImageSpan span in spannable.GetSpans(index, index + smiley.Length, Java.Lang.Class.FromType(typeof(ImageSpan))))
					{
						if (spannable.GetSpanStart(span) >= index && spannable.GetSpanEnd(span) <= index + smiley.Length)
							spannable.RemoveSpan(span);
						else
						{
							set = false;
							break;
						}
					}
					if (set)
					{
						hasChanges = true;
						spannable.SetSpan(new ImageSpan(context, smileyImage), index, index + smiley.Length, SpanTypes.ExclusiveExclusive );
					}
				}
			}
			return hasChanges;
		}

		public static ISpannable GetSmiledText(Context context, ICharSequence text)
		{
			var spannable = SpannableFactory.Instance.NewSpannable(text);
			AddSmiles(context, spannable);
			return spannable;
		}


		public static void AddSmiley(string textSmiley, int smileyResource)
		{
			Emoticons.Add(textSmiley, smileyResource);
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