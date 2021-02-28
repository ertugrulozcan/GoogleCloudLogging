using System;
using System.Collections.Generic;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json.Linq;

namespace GoogleCloudLogging.Proto
{
	public class JsonObject
	{
		#region Properties

		public string Json { get; }

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="json"></param>
		public JsonObject(string json)
		{
			this.Json = json;
		}

		#endregion
		
		#region Methods

		public Value ToStructValue()
		{
			if (string.IsNullOrEmpty(this.Json))
			{
				return Value.ForNull();
			}

			var obj = Newtonsoft.Json.JsonConvert.DeserializeObject(this.Json);
			if (obj is JObject jObject)
			{
				return ConvertToStructValue(jObject);
			}
			else if (obj is JArray jArray)
			{
				return ConvertToStructValue(jArray);
			}
			else
			{
				return Value.ForNull();
			}
		}

		private static Value ConvertToStructValue(JToken jToken)
		{
			if (jToken == null)
			{
				return Value.ForNull();
			}

			if (jToken is JProperty jProperty)
			{
				return ConvertToStructValue(jProperty.Value);
			}
			
			try
			{
				switch (jToken.Type)
                {
                    case JTokenType.Null:
                    case JTokenType.Undefined:
                        return Value.ForNull();
                    case JTokenType.Integer:
                    case JTokenType.Float:
                        return Value.ForNumber(jToken.Value<double>());
                    case JTokenType.Boolean:
                        return Value.ForBool(jToken.Value<bool>());
                    case JTokenType.String:
                    case JTokenType.None:
                    case JTokenType.Constructor:
                    case JTokenType.Property:
                    case JTokenType.Comment:
                    case JTokenType.Date:
                    case JTokenType.Raw:
                    case JTokenType.Bytes:
                    case JTokenType.Guid:
                    case JTokenType.Uri:
                    case JTokenType.TimeSpan:
                        return Value.ForString(jToken.Value<string>());
                    case JTokenType.Object:
                        var fieldDictionary = new Dictionary<string, Value>();
                        foreach (var prop in jToken)
                        {
							if (prop is JProperty jProp)
							{
								fieldDictionary.Add(jProp.Name, ConvertToStructValue(prop));
							}
							else
							{
								fieldDictionary.Add(prop.Path, ConvertToStructValue(prop));
							}
                        }
                                    
                        return Value.ForStruct(new Struct
                        {
                            Fields = { fieldDictionary }
                        });
                    case JTokenType.Array:
                        if (jToken is JArray jArray)
                		{
                            var values = new List<Value>();
                            foreach (var item in jArray)
                            {
                            	values.Add(ConvertToStructValue(item));
                            }
                                    
                            return Value.ForList(values.ToArray());
                        }
                		else
                		{
                			return Value.ForNull();
                		}
                	default:
                		return Value.ForNull();
                }	
			}
			catch (Exception ex)
			{
				return Value.ForString(ex.ToString());
			}
		}

		#endregion
	}
}