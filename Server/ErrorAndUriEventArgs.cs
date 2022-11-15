using System ;
using System.IO ;


namespace WebSockets
{
	public class ErrorAndUriEventArgs:ErrorEventArgs
	{
		public Uri uri 
		{
			get ;
			protected set ;
		}
		public ErrorAndUriEventArgs ( Exception exception ):base ( exception )
		{
			uri = null ;
		}
		public ErrorAndUriEventArgs ( Uri uri , Exception exception ):base ( exception )
		{
			this.uri = uri ;
		}
	}
}
