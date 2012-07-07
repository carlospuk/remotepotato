namespace ZetaLibWeb
{
	#region Using directives.
	// ----------------------------------------------------------------------

	using System;
	using System.Web;
	using System.Collections.Specialized;

	// ----------------------------------------------------------------------
	#endregion

	/////////////////////////////////////////////////////////////////////////

	/// <summary>
	/// Class for parsing URL parameters (parameters).
	/// </summary>
	/// <remarks>For comments and questions, please contact Uwe Keim
	/// (mailto:uwe.keim@zeta-software.de). 
	/// Last modified: 2004-12-14.</remarks>
	public class QueryString : 
		ICloneable
	{
		#region Construction.
		// ------------------------------------------------------------------

		/// <summary>
		/// Constructor.
		/// </summary>
		public QueryString()
		{
			/*if ( HttpContext.Current!=null &&
				HttpContext.Current.Handler!=null &&
				HttpContext.Current.Handler is System.Web.UI.Page )
			{
				InternalCurrentPage = HttpContext.Current.Handler as System.Web.UI.Page;
				FromUrl( InternalCurrentPage );
			}
             */
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public QueryString( 
			System.Web.UI.Page currentPage )
		{
			FromUrl( currentPage );
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public QueryString( 
			string url )
		{
			FromUrl( url );
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public QueryString( 
			Uri uri )
		{
			FromUrl( uri.AbsoluteUri );
		}

		// ------------------------------------------------------------------
		#endregion

		#region Public properties.
		// ------------------------------------------------------------------

		/// <summary>
		/// Access an parameter value by the parameter name.
		/// </summary>
		public string this [string index]
		{
			get
			{
				return InternalQS[index];
			}
			set
			{
				InternalQS[index] = value;
			}
		}

		/// <summary>
		/// Get the complete string including the BeforeUrl and
		/// all current parameters.
		/// </summary>
		public string All
		{
			get
			{
				return BeforeUrl + Make();
			}
			set
			{
				FromUrl( value );
			}
		}

		/// <summary>
		/// The URL that comes before the actual name-value pair parameters.
		/// </summary>
		public string BeforeUrl
		{
			get
			{
				return InternalBeforeUrl;
			}
			set
			{
				InternalBeforeUrl = value;
			}
		}

        public int Count
        {
            get
            {
                if (InternalQS != null)
                  {return InternalQS.Count;}

                if (InternalCurrentPage != null &&
                    InternalCurrentPage.Request != null &&
                    InternalCurrentPage.Request.Form != null)
                {
                    return InternalCurrentPage.Request.Form.Count;
                }
                else
                {
                    if (InternalCurrentPage.Session != null)
                    {
                        
                        return InternalCurrentPage.Session.Count;
                    }
                }

                return 0;
            }
        }

		// ------------------------------------------------------------------
		#endregion

		#region Public operations.
		// ------------------------------------------------------------------

		/// <summary>
		/// Appends a query string onto an existing URL. Takes care 
		/// of worrying about whether to add "&..." or "?...".
		/// </summary>
		/// <param name="URL">The URL to be extended.</param>
		/// <param name="QS">The query string to add.</param>
		/// <returns>Returns the complete URL.</returns>
		static public string AppendQueryString(
			string url,
			string qs )
		{ 
			string result = url.TrimEnd( '?', '&' );

			if ( result.IndexOf("?")>=0 )
			{
				return url + "&" + qs;
			}
			else
			{
				return url + "?" + qs;
			}
		}

		/// <summary>
		/// Check whether an parameter with a given name exists.
		/// </summary>
		/// <param name="parameterName">The name of the parameter
		/// to check for.</param>
		/// <returns>Returns TRUE if the parameter is present and
		/// has a non-empty value, returns FALSE otherwise.</returns>
		public bool HasParameter(
			string parameterName )
		{
			if ( parameterName==null ||
				parameterName.Trim().Length<=0 )
			{
				return false;
			}
			else
			{
				parameterName = parameterName.Trim();
				string v = this[parameterName];

				if ( v==null || v.Trim().Length<=0 )
				{
					return false;
				}
				else
				{
					return true;
				}
			}
		}

		/// <summary>
		/// Set or replace a single parameter.
		/// </summary>
		/// <param name="name">The name of the parameter to set.</param>
		/// <param name="val">The value of the parameter to set.</param>
		public void SetParameter( 
			string name, 
			string val )
		{
			InternalQS[name] = val;
		}

		/// <summary>
		/// Removes an parameter (if exists) with the given name.
		/// </summary>
		/// <param name="name">The name of the parameter to remove.</param>
		public void RemoveParameter( 
			string name )
		{
			InternalQS.Remove( name );
		}

		/// <summary>
		/// Removes all parameters.
		/// </summary>
		public void RemoveAllParameters()
		{
			InternalQS.Clear();
		}

		/// <summary>
		/// Get an parameter value by a given name.
		/// </summary>
		/// <param name="name">The name of the parameter value to retrieve.</param>
		/// <returns>Returns an empty string (NOT null) if the parameter
		/// is not found.</returns>
		public string GetParameter( 
			string name )
		{
			string result = InternalQS[name];

			if ( result==null || result.Length==0 )
			{
				if ( InternalCurrentPage!=null &&
					InternalCurrentPage.Request!=null &&
					InternalCurrentPage.Request.Form!=null )
				{
					result = InternalCurrentPage.Request.Form[name];
				}

				// try session, also.
				if ( result==null || result.Length==0 )
				{
					if ( InternalCurrentPage!=null &&
						InternalCurrentPage.Session!=null  )
					{
						object o = InternalCurrentPage.Session[name];
						if ( o!=null )
						{
							result = o.ToString();
						}	 
					}								  
				}

				/* Try cookies, also.
				if ( result==null || result.Length==0 ) 
				{
					if ( InternalCurrentPage!=null &&
						InternalCurrentPage.Request!=null &&
						InternalCurrentPage.Request.Cookies!=null )
					{
						HttpCookie c = InternalCurrentPage.Request.Cookies[name];
						if ( c!=null )
						{
							result = c.Value;
						}	 
					}								   
				} */
			}

			if ( result==null ) 
			{
				result = string.Empty;
			}

			return result;
		}

		// ------------------------------------------------------------------
		#endregion

		#region Reading from an URL.
		// ------------------------------------------------------------------
		
		/// <summary>
		/// Parse a query string and insert the found parameters
		/// into the collection of this class.
		/// </summary>
		public void FromUrl( 
			System.Web.UI.Page page )
		{
			if ( page!=null )
			{
				InternalCurrentPage = page;
				FromUrl( InternalCurrentPage.Request.RawUrl );
			}	 
		}

		/// <summary>
		/// Parse a query string and insert the found parameters
		/// into the collection of this class.
		/// </summary>
		public void FromUrl( 
			Uri uri )
		{
			if ( uri!=null )
			{
				FromUrl( uri.AbsoluteUri );
			}	 
		}

		/// <summary>
		/// Parse a query string and insert the found parameters
		/// into the collection of this class.
		/// </summary>
		public void FromUrl( 
			string url )
		{
			if ( url!=null )
			{
				InternalQS.Clear();

				// store the part before, too.
				int qPos = url.IndexOf( "?" );
				if ( qPos>=0 )
				{
					BeforeUrl = url.Substring( 0, qPos-0 );
					url = url.Substring( qPos+1 );
				}
				else
				{
					BeforeUrl = url;
				}

				if ( url.Length>0 && url.Substring( 0, 1 )=="?" )
				{
					url = url.Substring( 1 );
				}

				// break the values.
				string[] pairs = url.Split( '&' );
				foreach ( string pair in pairs )
				{
					string a = string.Empty;
					string b = string.Empty;

					string[] singular = pair.Split( '=' );
				
					int j = 0;
					foreach ( string one in singular )
					{
						if ( j==0 )
						{
							a = one;
						}
						else
						{
							b = one;
						}

						j++;
					}

					// store.
					SetParameter( a, System.Web.HttpUtility.UrlDecode( b ) );
				}
			}
		}

		// ------------------------------------------------------------------
		#endregion

		#region Making a string from the parameters.
		// ------------------------------------------------------------------

		/// <summary>
		/// Build a single string from the current name-value pairs inside
		/// this class.
		/// </summary>
		/// <returns>Returns the complete string.</returns>
		public string Make()
		{
			string result = "?";

			foreach( string name in InternalQS )
			{
				string val = InternalQS[name];

				if ( val!=null && val.Length>0 )
					result += name + "=" + HttpUtility.UrlEncode( val ) + "&";
			}

			//return result;
			return result.TrimEnd( '?', '&' );
		}

		/// <summary>
		/// Build a single string from the current name-value pairs inside
		/// this class. Replace/add the name-value pairs passed as 
		/// parameters to this function.
		/// </summary>
		/// <returns>Returns the complete string.</returns>
		public string Make( 
			string name1,
			string value1 )
		{
			return Make(
				name1,value1, 
				string.Empty,string.Empty, 
				string.Empty,string.Empty, 
				string.Empty,string.Empty, 
				string.Empty,string.Empty );
		}

		/// <summary>
		/// Build a single string from the current name-value pairs inside
		/// this class. Replace/add the name-value pair(s) passed as 
		/// parameters to this function.
		/// </summary>
		/// <returns>Returns the complete string.</returns>
		public string Make( 
			string name1,
			string value1, 
			string name2,
			string value2 )
		{
			return Make(
				name1,value1, 
				name2,value2, 
				string.Empty,string.Empty, 
				string.Empty,string.Empty, 
				string.Empty,string.Empty );
		}

		/// <summary>
		/// Build a single string from the current name-value pairs inside
		/// this class. Replace/add the name-value pair(s) passed as 
		/// parameters to this function.
		/// </summary>
		/// <returns>Returns the complete string.</returns>
		public string Make( 
			string name1,
			string value1, 
			string name2,
			string value2, 
			string name3,
			string value3 )
		{
			return Make(
				name1,value1, 
				name2,value2, 
				name3,value3, 
				string.Empty,string.Empty, 
				string.Empty,string.Empty );
		}

		/// <summary>
		/// Build a single string from the current name-value pairs inside
		/// this class. Replace/add the name-value pair(s) passed as 
		/// parameters to this function.
		/// </summary>
		/// <returns>Returns the complete string.</returns>
		public string Make( 
			string name1,
			string value1, 
			string name2,
			string value2, 
			string name3,
			string value3, 
			string name4,
			string value4 )
		{
			return Make(
				name1,value1, 
				name2,value2, 
				name3,value3, 
				name4,value4,
				string.Empty,string.Empty );
		}

		/// <summary>
		/// Build a single string from the current name-value pairs inside
		/// this class. Replace/add the name-value pair(s) passed as 
		/// parameters to this function.
		/// </summary>
		/// <returns>Returns the complete string.</returns>
		public string Make( 
			string name1,
			string value1, 
			string name2,
			string value2, 
			string name3,
			string value3, 
			string name4,
			string value4, 
			string name5,
			string value5 )
		{
			string old5 = GetParameter(name5);
			string old4 = GetParameter(name4);
			string old3 = GetParameter(name3);
			string old2 = GetParameter(name2);
			string old1 = GetParameter(name1);

			SetParameter(name5, value5);
			SetParameter(name4, value4);
			SetParameter(name3, value3);
			SetParameter(name2, value2);
			SetParameter(name1, value1);

			string result = Make();

			SetParameter(name5, old5);
			SetParameter(name4, old4);
			SetParameter(name3, old3);
			SetParameter(name2, old2);
			SetParameter(name1, old1);

			return result;
		}

		// ------------------------------------------------------------------
		#endregion

		#region ICloneable member.
		// ------------------------------------------------------------------

		/// <summary>
		/// Makes a deep copy.
		/// </summary>
		public object Clone()
		{
			QueryString dst = new QueryString();

			dst.InternalCurrentPage = this.InternalCurrentPage;
			dst.BeforeUrl = this.BeforeUrl;

			// Clone.
			foreach ( string key in this.InternalQS.Keys )
			{
				dst.InternalQS[key] = this.InternalQS[key];
			}

			return dst;
		}

		// ------------------------------------------------------------------
		#endregion

		#region Private helper.
		// ------------------------------------------------------------------

		/// <summary>
		/// The URL that comes before the actual name-value pair parameters.
		/// </summary>
		private string InternalBeforeUrl = string.Empty;

		/// <summary>
		/// The page that is currently loaded.
		/// </summary>
		private System.Web.UI.Page InternalCurrentPage = null;

		/// <summary>
		/// The collection to store the name-value pairs.
		/// </summary>
		private NameValueCollection	InternalQS = new NameValueCollection();

		// ------------------------------------------------------------------
		#endregion
	}

	/////////////////////////////////////////////////////////////////////////
}