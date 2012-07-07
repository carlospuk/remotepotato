using System;
using System.Text;
using System.IO;
using System.Web.UI;

namespace FatAttitude.HTML
{
    public class HTMLLink
    {
        public string CSSClass { get; set; }
        public string HRef { get; set; }
        public string Content { get; set; }

        public HTMLLink(string href, string strContent)
        {
            HRef = href;
            Content = strContent;
        }
        public HTMLLink(string href, string strContent,  string cssClass)
            : this(href, strContent)
        {
            CSSClass = cssClass;
        }

        public override string ToString()
        {
            using (StringWriter sw = new StringWriter())
            {
                using (HtmlTextWriter writer = new HtmlTextWriter(sw))
                {
                    if (!(string.IsNullOrWhiteSpace(CSSClass))) writer.AddAttribute(HtmlTextWriterAttribute.Class, CSSClass);
                    writer.AddAttribute(HtmlTextWriterAttribute.Href, HRef);
                    writer.RenderBeginTag(HtmlTextWriterTag.A);
                    writer.Write(Content);
                    writer.RenderEndTag();
                }

                return sw.ToString();
            }
        }



    }
}
