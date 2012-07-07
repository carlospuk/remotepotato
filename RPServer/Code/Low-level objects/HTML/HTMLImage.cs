using System;
using System.Text;
using System.IO;
using System.Web.UI;

namespace FatAttitude.HTML
{
    public class HTMLImage
    {
        public string CSSClass { get; set; }
        public string SourcePath { get; set; }

        public HTMLImage(string sourcePath)
        {
            SourcePath = sourcePath;
        }
        public HTMLImage(string sourcePath, string cssClass)
            : this(sourcePath)
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
                    writer.AddAttribute(HtmlTextWriterAttribute.Src, SourcePath);
                    writer.RenderBeginTag(HtmlTextWriterTag.Img);
                    writer.RenderEndTag();
                }

                return sw.ToString();
            }
        }



    }
}
