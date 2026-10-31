// Which clip says a line: a short hash of who says it and what they say, shared by the editor exporter
// (Logs/voice_lines.json), the voice tool (unity/tools/voice.py, MD5 of "sp|text") and the player (Sound.Say).
using System.Security.Cryptography;
using System.Text;

namespace CFDream
{
    public static class Voice
    {
        public static string Key(string sp, string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            using (var md5 = MD5.Create())
            {
                var h = md5.ComputeHash(Encoding.UTF8.GetBytes((sp ?? "") + "|" + text.Replace("\r", "")));
                var sb = new StringBuilder(12); for (int i = 0; i < 6; i++) sb.Append(h[i].ToString("x2")); return sb.ToString();
            }
        }
    }
}
