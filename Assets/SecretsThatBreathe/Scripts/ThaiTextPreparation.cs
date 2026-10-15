using System.Text;

namespace SecretsThatBreathe
{
    public static class ThaiTextPreparation
    {
        // Sara Am needs a Nikhahit before the tone mark, then Sara Aa.
        // This is display preprocessing only, not a change to the authored sentence.
        public static string Prepare(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('\u0E33') < 0) return text;
            var result = new StringBuilder(text.Length + 8);
            bool inTag = false;
            for (int i = 0; i < text.Length; i++)
            {
                char character = text[i];
                if (character == '<') inTag = true;
                if (!inTag && character == '\u0E33')
                {
                    int insertion = result.Length;
                    while (insertion > 0 && result[insertion - 1] >= '\u0E48' && result[insertion - 1] <= '\u0E4B') insertion--;
                    result.Insert(insertion, '\u0E4D');
                    result.Append('\u0E32');
                }
                else result.Append(character);
                if (character == '>') inTag = false;
            }
            return result.ToString();
        }
    }
}
