using System ;
using System.Collections ;
using System.Collections.Generic ;
using Newtonsoft.Json.Linq ;
using Newtonsoft.Json ;
namespace WebSockets
{
	public abstract class ConfigData
	{
		// <summary>
		/// Loads real object with data from json string
		/// </summary>
		/// <param name="json">JSON string</param>
		public abstract void loadFromJSON ( string json ) ;
		/// <summary>
		/// Saves object to json string
		/// </summary>
		/// <param name="json">JSON string</param>
		public abstract void saveToJSON ( out string json ) ;
		/// <summary>
		/// Loads FileHttpService.FileHttpServiceData object with data from json string
		/// </summary>
		/// <param name="json">JSON string</param>
		public static string getJSON ( object obj ) 
		{ 
			return JsonConvert.SerializeObject ( obj ) ;
		}
		///// <summary>
		///// Serialize any object to JSON.
		///// <br/>This actually access to JsonConvert.SerializeObject method.
		///// </summary>
		///// <param name="obj">Object to serialize</param>
		///// <returns>Returns JsonConvert.SerializeObject ( obj ) </returns>
		//public static string SerializeObject ( object obj ) 
		//{
		//	return JsonConvert.SerializeObject ( obj ) ;
		//}
		///// <summary>
		///// Deserilaize object from JSON.
		///// <br/>This actually access to JsonConvert.DeserializeObject method.
		///// </summary>
		///// <param name="json">JSON string</param>
		///// <returns>Returns whatever JsonConvert.DeserializeObject returns, usually JObject instance.</returns>
		//public static JObject DeserializeObject ( string json ) 
		//{
		//	return ( JObject ) JsonConvert.DeserializeObject ( json ) ;
		//}
		//public enum PrimitiveType
		//{
		//	stringType = 0 ,
		//	boolType = 1 ,
		//	integerType = 2 ,
		//	floatType = 4 ,
		//	arrayType = 8 ,
		//	objectType = 16
		//}
		//public void nesto()
		//{
		//	JArray array = new JArray ();
		//	array [ 1 ] = 2 ;
		//}
		///// <summary>
		///// Saves FileHttpService.FileHttpServiceData object to json string
		///// </summary>
		///// <param name="json">JSON string</param>
		//public static dynamic object fromJSON ( string json ) 
		//{ 
		//	foreach ( KeyValuePair <string,JToken> pair in ( JObject ) JsonConvert.DeserializeObject ( json ) )
		//	{

		//	}
		//	json = "{ \"webroot\":" + ( webroot == null ? "" : JsonConvert.SerializeObject ( webroot ) ) + " }" ;
		//}
	}
}
