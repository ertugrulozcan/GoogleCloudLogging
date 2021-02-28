using System;
using System.Collections;
using System.Collections.Generic;
using Google.Protobuf.WellKnownTypes;
using GoogleCloudLogging.Proto;

namespace GoogleCloudLogging.Helpers
{
	public static class ProtobufHelper
	{
		#region Methods

		public static Value ConvertToStructValue(object obj)
		{
			if (obj == null)
			{
				return Value.ForNull();
			}
			
			var type = obj.GetType();
			if (type.IsPrimitive)
			{
				if (IsNumericType(type) && double.TryParse(obj.ToString(), out double number))
				{
					return Value.ForNumber(number);
				}
				else if (System.Type.GetTypeCode(type) == TypeCode.Boolean)
				{
					return Value.ForBool((bool) obj);
				}
				else
				{
					return Value.ForString(obj.ToString());
				}
			}
			else
			{
				if (System.Type.GetTypeCode(type) == TypeCode.String)
				{
					return Value.ForString(obj.ToString());
				}
				else if (obj is ICollection collection)
				{
					var values = new List<Value>();
					foreach (var item in collection)
					{
						values.Add(ConvertToStructValue(item));
					}

					return Value.ForList(values.ToArray());
				}
				else if (obj is JsonObject jsonObject)
				{
					return jsonObject.ToStructValue();
				}
				else
				{
					var fieldDictionary = new Dictionary<string, Value>();
					var properties = type.GetProperties();
					foreach (var propertyInfo in properties)
					{
						var value = propertyInfo.GetValue(obj);
						fieldDictionary.Add(propertyInfo.Name, ConvertToStructValue(value));
					}

					return Value.ForStruct(new Struct
					{
						Fields = { fieldDictionary }
					});
				}
			}
		}
		
		private static bool IsNumericType(object obj)
		{
			return IsNumericType(obj.GetType());
		}
		
		private static bool IsNumericType(System.Type type)
		{   
			// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
			switch (System.Type.GetTypeCode(type))
			{
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.Decimal:
				case TypeCode.Double:
				case TypeCode.Single:
					return true;
				default:
					return false;
			}
		}

		#endregion
	}
}