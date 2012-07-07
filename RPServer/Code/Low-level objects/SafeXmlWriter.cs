using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;

namespace RemotePotatoServer
{
    public class SafeXmlWriter : XmlTextWriter
    {
        public override void WriteString(string text)
        {
            var sb = new StringBuilder(text.Length);

            foreach (var c in text)
            {
                var current = (int)c;
                if (current == 0x9 ||
                    current == 0xA ||
                    current == 0xD ||
                    (current >= 0x20 && current <= 0xD7FF) ||
                    (current >= 0xE000 && current <= 0xFFFD) ||
                    (current >= 0x10000 && current <= 0x10FFFF))
                {
                    sb.Append(c);
                }
            }

            base.WriteString(sb.ToString());
        }

        public SafeXmlWriter(System.IO.Stream stream, Encoding enc) : base(stream, enc) { }
        public SafeXmlWriter(System.IO.TextWriter w) : base(w) { }
        public SafeXmlWriter(string file, Encoding enc) : base(file, enc) { }
    }
}
