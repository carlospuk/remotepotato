using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Diagnostics;
using Microsoft.MediaCenter.Guide;
using Microsoft.MediaCenter.Store;
using Microsoft.MediaCenter.Pvr;

/*
 * Classes to convert between native WMC objects, e.g. Channel
 * and Remote Potato representations, e.g. TVService.
 * 
 * Note that WMC's representations are far more detailed so a necessary amount of simplification
 * has to be done, e.g. there is no concept within Remote Potato of a duplicate TVService 
 * across several tuners, etc.
 */

namespace CommonEPG
{

    public static class Conversion
    {

        /* NB: UPDATED 2011-04-10
         * 
         * BE SURE TO COPY STRING VALUES RATHER THAN REFERENCES
         * I.E. STRING.COPY(...) TO AVOID DANGLING REFERENCES TO OBJECTS
         * MANAGED BY MEDIA CENTER ITSELF
         * 
         * */
        public static TVService TVServiceFromChannel(Channel c)
        {
            TVService tvs = new TVService();
            tvs.Callsign = "Null Channel";

            try
            {
                if (
                    (c == null) ||
                    (c.Service == null)
                    )
                {
                    tvs.UniqueId = new Random().Next(999999).ToString();
                    return tvs;
                }

                // Valid channel
                // IDs - story both a unique ID for the service, and the ID of the media center channel
                tvs.UniqueId = string.Copy(c.Service.Id.ToString("G17")); // string copy may be redundant
                tvs.MCChannelID = c.Id;
                
                if (!string.IsNullOrEmpty(c.CallSign))
                    tvs.Callsign = string.Copy( c.CallSign );

                tvs.MCChannelNumber = c.Number;
                tvs.MCSubChannelNumber = c.SubNumber;
                tvs.WatchedDuration = c.WatchedDuration;


                if (c.Service.LogoImage != null)
                {
                    if (!(string.IsNullOrEmpty(c.Service.LogoImage.AbsoluteUri)))
                        tvs.LogoUri = string.Copy(c.Service.LogoImage.AbsoluteUri);
                }

                
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);
            }

            return tvs;
        }
        public static TVProgramme TVProgrammeFromScheduleEntry(ScheduleEntry se)
        {
            return TVProgrammeFromScheduleEntry(se, false);
        }
        public static TVProgramme TVProgrammeFromScheduleEntry(ScheduleEntry se, bool omitDescription)
        {
            TVProgramme tvp = new TVProgramme();
            Program p = se.Program;
            if (p == null) return null;

            tvp.MCProgramID = string.Copy( p.Id.ToString("G17") );

            Service tvs = se.Service;
            if (tvs == null) 
                return null;


            if (! string.IsNullOrEmpty(p.Title))
                tvp.Title = string.Copy( p.Title );

            if (!string.IsNullOrEmpty(p.EpisodeTitle))
            {
                if (p.EpisodeNumber > 0)
                    tvp.EpisodeTitle = string.Copy( p.EpisodeNumber.ToString() ) + ". " + string.Copy( p.EpisodeTitle );
                else
                    tvp.EpisodeTitle = string.Copy( p.EpisodeTitle );
            }

            if (!omitDescription)
            {
                if (!string.IsNullOrEmpty(p.Description))
                    tvp.Description = string.Copy( p.Description );

                if (p.GuideImage != null)
                    if (!string.IsNullOrEmpty(p.GuideImage.AbsoluteUri))
                        tvp.GuideImageUri = string.Copy(p.GuideImage.AbsoluteUri);
            }
            else
            {
                tvp.Description = "";  // OMIT description to keep transfer to client lean
                tvp.GuideImageUri = "";
            }

            //if (!string.IsNullOrEmpty(p.StarRating))
            if (p.HalfStars > 0)
                tvp.StarRating = p.HalfStars;

            if (!string.IsNullOrEmpty(p.MpaaRatingString))
                tvp.MPAARating = string.Copy( p.MpaaRatingString );

            if (!string.IsNullOrEmpty(se.TVRatingString))
                tvp.TVRating = string.Copy ( se.TVRatingString );

            if (p.OriginalAirdate != null)
                tvp.OriginalAirDate = p.OriginalAirdate.Ticks;

            // Flags
            tvp.HasSubtitles = se.IsSubtitled;
            tvp.IsFirstShowing = (!se.IsRepeat);
            tvp.IsHD = (se.IsHdtv);
            tvp.ProgramType = ProgrammeTypeForProgram(p);

            // Series?
            SeriesInfo si = p.Series;
            if (si != null)
            {
                tvp.IsSeries = true;
                tvp.SeriesID = si.Id;
            }
            else
                tvp.IsSeries = false;

            tvp.StartTime = se.StartTime.Ticks;
            tvp.StopTime = se.EndTime.Ticks;

            
            

            tvp.Id = string.Copy ( se.Id.ToString("G17") );

            // CHANNEL
            tvp.ServiceID = string.Copy ( tvs.Id.ToString("G17") );

            return tvp;
        }
        public static TVProgrammeCrew TVProgrammeCrewFromProgram(Program p)
        {
            TVProgrammeCrew crew = new TVProgrammeCrew();
            ProgramExtra pe = p.GetProgramExtra();
            if (pe == null) return crew;

            if (!String.IsNullOrEmpty(pe.Actors))
                crew.Actors = pe.Actors;

            if (!String.IsNullOrEmpty(pe.GuestActors))
                if (!String.IsNullOrEmpty(crew.Actors))
                    crew.Actors += pe.GuestActors;
                else
                    crew.Actors = pe.GuestActors;

            if (!String.IsNullOrEmpty(pe.Directors))
                crew.Directors = pe.Directors;

            if (!String.IsNullOrEmpty(pe.Writers))
                crew.Writers = pe.Writers;

            if (!String.IsNullOrEmpty(pe.Producers))
                crew.Producers = pe.Producers;

            return crew;
        }
        public static bool isProgrammeType(Program p, TVProgrammeType matchType)
        {
            return (matchType == ProgrammeTypeForProgram(p));
        }
        public static TVProgrammeType ProgrammeTypeForProgram(Program p)
        {
            if (p == null) return TVProgrammeType.None;

            if (p.IsMovie)
                return TVProgrammeType.Movie;
            else if (p.IsSports)
                return TVProgrammeType.Sport;
            else if (p.IsNews)
                return TVProgrammeType.News;
            else if (p.IsDocumentary)
                return TVProgrammeType.Documentary;
            else if (p.IsKids)
                return TVProgrammeType.Kids;
            else
                return TVProgrammeType.None;
        }
   
        public static RPRecording RPRecordingFromRecording(Recording mcrec)
        {
            RPRecording rprec = new RPRecording();

            rprec.Id = mcrec.Id;

            if (mcrec.ScheduleEntry != null)
                rprec.TVProgrammeID =  mcrec.ScheduleEntry.Id;

            
            if (mcrec.Request != null)
            {
                // Get the request that generated this - NB currently, this isn't ideal: there may be multiple - e.g. series and oneTime
                Request mcreq = mcrec.Request;
                rprec.RPRequestID = mcreq.Id;

                if (mcreq is SeriesRequest)
                {
                    SeriesRequest sr = (SeriesRequest)mcreq;

                    // Series?
                    SeriesInfo si = sr.Series;
                    if (si != null)
                        rprec.SeriesID = si.Id;                    
                }

                if (mcreq is ManualRequest)
                {
                    ManualRequest mr = (ManualRequest)mcreq;
                    rprec.ManualRecordingDuration = mr.Duration.TotalSeconds;
                    try
                    { rprec.ManualRecordingServiceID = mr.Channel.Service.Id; }
                    catch { }
                    rprec.ManualRecordingStartTime = mr.StartTime;

                }

                // Helpers
                rprec.RequestType = RPRequestTypeForRequest(mcreq);
                if (! string.IsNullOrWhiteSpace( mcreq.Title ))
                    rprec.Title = string.Copy ( mcreq.Title );
            }


            rprec.State = (RPRecordingStates)Enum.Parse(typeof(RPRecordingStates), mcrec.State.ToString());

            rprec.KeepUntil = (int)mcrec.KeepLength;
            rprec.Partial = mcrec.IsPartial;
            rprec.Quality = mcrec.Quality;
            
            return rprec;
        }
        public static RPRequest RPRequestFromRequest(Request rq)
        {
            RPRequest rpr = new RPRequest();

            rpr.ID = rq.Id;
            rpr.RequestType = RPRequestTypeForRequest(rq);
            rpr.Priority = rq.Priority;

            if (!String.IsNullOrEmpty(rq.Title))
                rpr.Title = string.Copy ( rq.Title );

            if (rq is SeriesRequest)
            {
                SeriesRequest srq = (SeriesRequest)rq;
                if (srq.Series != null)
                    rpr.SeriesID = srq.Series.Id;
            }


            Channel ch = rq.Channel;
            if (ch != null)
            {
                Service svc = ch.Service;
                if (svc != null)
                    rpr.ServiceID = svc.Id;
            }

            return rpr;
        }


        // Helpers
        private static RPRequestTypes RPRequestTypeForRequest(Request rq)
        {
            if (rq is OneTimeRequest)
                return RPRequestTypes.OneTime;
            else if (rq is SeriesRequest)
                return RPRequestTypes.Series;
            else if (rq is ManualRequest)
                return RPRequestTypes.Manual;
            else if (rq is WishListRequest)
                return RPRequestTypes.Keyword;
            else
                return RPRequestTypes.Unknown;
        }

        // Useless
        static void PopulateIfNotNull(ref string destString, string srcString)
        {
            if (!(string.IsNullOrEmpty(srcString)))
                destString = (string)srcString.Clone();
        }

    }


    /*
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(Channel))
                return true;

            return base.CanConvertFrom(context, sourceType);
        }

        // Convert from a Channel to a TVChannel
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is Channel)
            {
                TVChannel tvc = new TVChannel();
                Channel c = (Channel)value;

                tvc.Callsign = c.CallSign;
                tvc.Id = c.UniqueId.ToString();
                tvc.MCChannelNumber = c.ChannelNumber.Number;
                tvc.MCChannelNumber = c.ChannelNumber.SubNumber;

                return tvc;
            }
            return base.ConvertFrom(context, culture, value);
        }

        // // Convert from a TVChannel to a Channel - NOT IMPLEMENTED
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(Channel))
            {
                // Not implemented
                return new Channel();
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

    */


}

