using System;
using System.Collections.Generic;

namespace CommonEPG
{
    public class TVMovie
    {
        public TVMovie(int newId)
        {
            Id = newId;
            Showings = new List<TVProgramme>();
        }
        public TVMovie(int newId, TVProgramme tvp)
            : this(newId)
        {
            Showings.Add(tvp);
            Title = tvp.Title;
        }

        public List<TVProgramme> Showings { get; set; }
        public int Id { get; set; }
        public string Title { get; set; }

        public TVProgramme DefaultShowing
        {
            get
            {
                if (Showings.Count < 1) return null;
                return Showings[0];
            }
        }
    }
}
