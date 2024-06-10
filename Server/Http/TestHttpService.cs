using System ;
using System.Collections.Generic ;
using System.IO; 
using System.Reflection ;
using System.Text ;
using System.Text.RegularExpressions ;
using System.Net.Sockets ;
using System.Net ;
using System.Diagnostics ;
using System.Runtime.Remoting.Messaging ;
using Newtonsoft.Json.Linq ;
using Newtonsoft.Json ;
namespace WebSockets
{
	/// <summary>
	/// Retrurns simple html document with given message in it.
	/// </summary>
	public class TestHttpService:HttpServiceBase
	{
		/// <summary>
		/// Config data for TestHttpService class
		/// </summary>
		public class TestHttpServiceData:JObject
		{
			/// <summary>
			/// Auxiliary variable for the message property
			/// </summary>
			protected string _message ;
			/// <summary>
			/// Message to be present on html file
			/// </summary>
			public string message 
			{
				get => _message ;
			}
			/// <summary>
			/// Creates new instance of FileHttpServiceData class 
			/// </summary>
			/// <param name="webroot">Webroot folder </param>
			public TestHttpServiceData ()
			{
				_message = "<default message>" ;
			}
			/// <summary>
			/// Creates new instance of FileHttpServiceData class 
			/// </summary>
			/// <param name="webroot">Webroot folder </param>
			public TestHttpServiceData ( string message )
			{
				_message = message ;
				Add ( "message" , message ) ;
			}
			/// <summary>
			/// Creates new instance of FileHttpServiceData class 
			/// </summary>
			/// <param name="webroot">Webroot folder </param>
			public TestHttpServiceData ( JObject obj )
			{
				loadFromJSON ( obj ) ;
			}
			/// <summary>
			/// Loads TestHttpService.TestHttpServiceData object with data from json string
			/// </summary>
			/// <param name="json">JSON string</param>
			public virtual void loadFromJSON ( JObject obj ) 
			{ 
				//JObject jo = ( JObject ) JsonConvert.DeserializeObject ( json ) ;
				JToken token = obj [ "message" ] ;
				if ( token == null )
					throw new InvalidDataException ( "Key \"message\" not found in JSON data" ) ;
				
				if ( token.Type == JTokenType.String )
					_message = token.ToObject<string>() ;
				else throw new InvalidDataException ( "Invalid JSON value \"" + token.ToString() + "\" for \"message\"" ) ;
			}
			///// <summary>
			///// Saves TestHttpService.TestHttpServiceData object to json string
			///// </summary>
			///// <param name="json">JSON string</param>
			//public override void saveToJSON ( out string json ) 
			//{ 
			//	json = "{ \"message\":" + JsonConvert.SerializeObject ( message ) + " }" ;
			//}
		}

		/// <summary>
		/// Auxiliary variable for the fileConfigData 
		/// </summary>
		protected TestHttpServiceData _testHttpConfigData ;
		/// <summary>
		/// Config data (message)
		/// </summary>
		public virtual TestHttpServiceData testHttpConfigData
		{
			get => _testHttpConfigData ;
		}
		/// <summary>
		/// Init new instance 
		/// </summary>
		/// <param name="server">WebServer instance</param>
		/// <param name="connection">Connection data(HttpConnectionDetails)</param>
		/// <param name="configData">(TestHttpServiceData)</param>
		public override void init ( WebServer server , HttpConnectionDetails connection , JObject configData )
		{
			if ( configData == null )
				_testHttpConfigData = new TestHttpServiceData ( ) ;
			else 
			{
				_testHttpConfigData = configData as TestHttpServiceData ;
				if ( _testHttpConfigData == null ) _testHttpConfigData = new TestHttpServiceData ( configData ) ;
			}
			
			base.init ( server , connection , configData ) ;
		}
		/// <summary>
		/// Returns entire html with message encoded in body as byte[] array
		/// </summary>
		public byte [] getHtmlBytes ()
		{
			return Encoding.UTF8.GetBytes ( getHtml () ) ;
		}
		/// <summary>
		/// Returns entire html with message encoded in body
		/// </summary>
		public string getHtml ()
		{
			stringBuilder.Clear () ;
			stringBuilder.Append ( "<html>\r\n\t<body>\r\n\t\t" ) ;
			stringBuilder.Append ( WebUtility.HtmlEncode ( testHttpConfigData.message ) ) ;
			stringBuilder.Append ( "\r\n\t</body>\r\n<html>" ) ;
			return stringBuilder.ToString() ;
		}
		/// <summary>
		/// This method sends data back to client
        /// </summary>
		/// </summary>
		/// <param name="responseHeader">Resonse header</param>
		/// <param name="codeError">Code execution error(if any)</param>
		/// <returns>Returns true if response is 400 and everything OK</returns>
		public override bool Respond ( MimeTypeDictionary mimeTypesByFolder , out string responseHeader , out Exception codeError ) 
		{
			responseHeader = "" ;
			codeError = null ;
			try
			{

				byte [] buffer = getHtmlBytes() ;
				responseHeader = RespondSuccess ( MimeTypes.html , buffer.Length , "utf-8" ) ;
				connection.stream.Write ( buffer , 0 , buffer.Length ) ;

				return true ;
			}
			catch ( Exception x )
			{
				codeError = x ;
			}
			return false ;
        }
		/// <summary>
		/// Returns resource stream for target uri
		/// </summary>
		/// <param name="uri">Target uri</param>
		public override Stream GetResourceStream ( Uri uri ) 
		{
			MemoryStream ms = new MemoryStream ( getHtmlBytes() ) ;
			ms.Position = 0 ;
			return ms ;
		}		
	}
}
