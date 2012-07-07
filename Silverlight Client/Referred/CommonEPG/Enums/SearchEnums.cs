using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonEPG
{
    public enum EPGSearchTextType
    {
        Title,
        TitleAndEpisodeTitle,
        TitlesAndDescription,
        Credits,
        AllTextFields
    }
    public enum EPGSearchMatchType
    {
        ExactMatch,
        StartsWith,
        Contains
    }
}
