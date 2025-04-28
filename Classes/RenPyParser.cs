using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ForgeTales.Classes
{
    public class RenPyParser
    {
        public List<DialogueLine> ParseScript(string filePath)
        {
            var lines = new List<DialogueLine>();
            var script = File.ReadAllLines(filePath);

            foreach (var line in script)
            {
                if (IsDialogueLine(line, out var character, out var text))
                {
                    lines.Add(new DialogueLine(character, text));
                }
            }
            return lines;
        }

        private bool IsDialogueLine(string line, out string character, out string text)
        {
            character = null;
            text = null;

            // Пример: `m "Привет, как дела?"`
            var match = Regex.Match(line, @"^\s*(\w+)\s*""(.+)""\s*$");
            if (match.Success)
            {
                character = match.Groups[1].Value;
                text = match.Groups[2].Value;
                return true;
            }
            return false;
        }
    }

    public class DialogueLine
    {
        public string Character { get; }
        public string Text { get; }

        public DialogueLine(string character, string text)
        {
            Character = character;
            Text = text;
        }
    }
}
