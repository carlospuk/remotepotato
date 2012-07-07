using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Web.UI;

namespace FatAttitude.HTML
{
    public class HTMLTable
    {

        #region Static Table Generators
        static int NumberOfColumnsInFileBrowseTable = 5;
        public static string HTMLTableWithCellContents(string strTableID, int numberOfColumns, List<string> contents)
        {
            NumberOfColumnsInFileBrowseTable = numberOfColumns;

            HTMLTable table = new HTMLTable(strTableID);

            /*HTMLTableRow topRow = new HTMLTableRow();
            HTMLTableCell topCell;
            for (int i = 0; i < numberOfColumns; i++)
            {
                topCell = new HTMLTableCell("&nbsp;", "column" + (i + 1).ToString());
                topRow.AddCell(topCell);
            }
            table.Rows.Add(topRow);
            */

            int currentColumnCounter = 0;
            HTMLTableRow currentRow = new HTMLTableRow();
            foreach (string strContent in contents)
            {
                HTMLTableCell currentCell = new HTMLTableCell(strContent);
                currentRow.AddCell(currentCell);

                if (moveColumn(ref currentColumnCounter) == 0)
                {
                    // Add last row?
                    if (currentRow != null) table.Rows.Add(currentRow);

                    // Create new row
                    currentRow = new HTMLTableRow();
                }
            }

            // Final row in progress?
            if (currentRow.Cells.Count > 0)
            {
                // Pad out row with empty cells if required
                if (currentColumnCounter != 0)
                {
                    while (moveColumn(ref currentColumnCounter) != 0)
                    {
                        currentRow.AddCell("&nbsp;");
                    }
                }

                table.Rows.Add(currentRow);
            }

            // Write table
            return table.ToString();
        }
        static int moveColumn(ref int currentColumn)
        {
            if (++currentColumn >= NumberOfColumnsInFileBrowseTable)
                currentColumn = 0;

            return currentColumn;
        }
        #endregion

        string CSSID;
        public string CSSClass { get; set; }
        public List<HTMLTableRow> Rows { get; set; }

        public HTMLTable(string strID)
        {
            CSSClass = null;
            CSSID = strID;
            Rows = new List<HTMLTableRow>();

            
        }
        public HTMLTable(string strID, string strCssClass) : this(strID)
        {
            CSSClass = strCssClass;
        }


        public override string ToString()
        {
            using (StringWriter sw = new StringWriter())
            {
                using (HtmlTextWriter writer = new HtmlTextWriter(sw))
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Id, CSSID);
                    if (!(string.IsNullOrWhiteSpace(CSSClass))) writer.AddAttribute(HtmlTextWriterAttribute.Class, CSSClass);
                    writer.RenderBeginTag(HtmlTextWriterTag.Table);

                    foreach (HTMLTableRow row in Rows)
                    {
                        writer.WriteLine(row.ToString() );
                    }

                    writer.RenderEndTag();
                }

                return sw.ToString();
            }
        }
    }


    #region Table Row
    /// <summary>
    /// A row, containing cells. 
    /// </summary>
    public class HTMLTableRow
    {
        public string CssClass = null;
        public List<HTMLTableCell> Cells = new List<HTMLTableCell>();

        public HTMLTableRow()
        { 
            Cells = new List<HTMLTableCell>();
        }
        public HTMLTableRow(string cssClass)
            : this()
        {
            CssClass = cssClass;
        }


        public void AddCells(params string[] strColumns)
        {
            foreach (string str in strColumns)
            {
                AddCell(str);
            }
        }
        public void AddCell(string strColumnText, string strColumnCssClass)
        {
            AddCell(new HTMLTableCell(strColumnText, strColumnCssClass));
        }
        public void AddCell(string strColumnText) 
        {
            AddCell(new HTMLTableCell(strColumnText));
        }
        public void AddCell(HTMLTableCell col)
        {
            Cells.Add(col);
        }

        public override string ToString()
        {
 	        using (StringWriter sw = new StringWriter())
            {
                using (HtmlTextWriter writer = new HtmlTextWriter(sw))
                {
                    if (!(string.IsNullOrWhiteSpace(CssClass))) writer.AddAttribute(HtmlTextWriterAttribute.Class, CssClass);
                    writer.RenderBeginTag(HtmlTextWriterTag.Tr); // Start of row

                    foreach (HTMLTableCell col in Cells)
                    {
                        if (!(string.IsNullOrWhiteSpace(col.CssClass))) writer.AddAttribute(HtmlTextWriterAttribute.Class, col.CssClass);
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        writer.Write(col.Content);
                        writer.RenderEndTag(); // End of column
                    }

                    writer.RenderEndTag(); // End of row
                }

                    return sw.ToString();
                }
            }
    }
    #endregion

    #region Table Cell
    /// <summary>
    /// A column, belongs within an HTMLTableRow.  Stored text should be passed in HTML encoded
    /// </summary>
    public class HTMLTableCell
    {
        public int ColSpan { get; set; }
        public string Content {get; set;}
        public string CssClass { get; set; }

        public HTMLTableCell()
        { }

        public HTMLTableCell(string strContent) : this()
        {
            Content = strContent;
        }
        public HTMLTableCell(string strContent, string cssClass)
            : this(strContent)
        {
            CssClass = cssClass;
        }
        public HTMLTableCell(string strContent, int colSpan)
            : this(strContent)
        {
            ColSpan = colSpan;
        }
        public HTMLTableCell(string strContent, string cssClass, int colSpan)
            : this(strContent, cssClass)
        {
            ColSpan = colSpan;
        }
        
    }
    #endregion

}
