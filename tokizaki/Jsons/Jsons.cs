/*
 * This is a Simple, Slow and Insecure Json tools library. It is only used to
 * learn how to implement some Json tools that can serialize or deserialize 
 * regular C# objects.
 * 
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace tokizaki.Jsons
{
    public class JsonIgnore : System.Attribute
    {

    }

    static class JsonExt
    {
        public static void Change(this ref JsonReadState state, JsonReadState changeTo)
        {
            Console.WriteLine($"{state} -> {changeTo}");
            state = changeTo;
        }



    }

    enum JsonReadState
    {
        None = 0,
        Start,
        ReadSymbol,
        ReadKeyStart,
        ReadKeyEnd,
        ReadyToReadValue,
        ReadValueStart,
        ReadValueEnd,
        Over,
    }

    public abstract class Json
    {
        static class JsonTools
        {
            public static bool IsIgnore(MemberInfo member)
            {
                return member.DeclaringType.GetInterface(Json._jsonIgnoreType.FullName) != null;
            }
        }

        public static string Serialization(object obj)
        {
            var json = Json.Create();
            try
            {
                SerializeToJson(ref json, ref obj);
            }
            catch (Exception e)
            {
                return "";
            }
            return json.ToString();
        }

        public static T Deserialize<T>(string jsonStr)
        {
            var type = typeof(T);
            var obj = type.Assembly.CreateInstance(type.FullName);
            var json = Json.Create();
            try
            {
                ParseFromJson(ref json, jsonStr);
                ParseFromJson(ref obj, ref json);
            }
            catch (Exception e)
            {
                return default;
            }
            return (T)obj;
        }

        public static Json Create()
        {
            return new JsonNode();
        }

        public static Json Parse(string jsonStr)
        {
            var json = Json.Create();

            try
            {
                ParseFromJson(ref json, jsonStr);
            }
            catch (Exception e)
            {
                return JsonData.Null;
            }

            return json;
        }

        public static string Format(string json)
        {
            var result = "";

            return json;
        }



        private static readonly Type _jsonIgnoreType = typeof(JsonIgnore);
        private static readonly Type _iEnumerableType = typeof(System.Collections.IEnumerable);
        private static readonly int _serializeMaxDeep = 32;

        private static readonly Type _intType = typeof(int);
        private static readonly Type _longType = typeof(long);
        private static readonly Type _floatType = typeof(float);
        private static readonly Type _doubleType = typeof(double);
        private static readonly Type _boolType = typeof(bool);
        private static readonly Type _stringType = typeof(string);


        private static string Escape(string str)
        {
            var result = "";
            foreach (var ch in str)
            {
            }
            return result;
        }


        private static void SerializeToJson(ref Json json, ref object obj, int _deep = 0)
        {
            if (_deep > _serializeMaxDeep) return;

            var type = obj.GetType();

            var members = type.GetMembers();

            if (TryConvertToKeyValuePair(obj, members, _deep, out var kv))
            {
                json.Add(kv.Key, kv.Value);
                return;
            }

            foreach (var member in members)
            {
                if (JsonTools.IsIgnore(member)) continue;

                object memberValue = null;

                switch (member.MemberType)
                {
                    case MemberTypes.Field:
                        memberValue = (member as FieldInfo).GetValue(obj);
                        break;
                    case MemberTypes.Property:
                        {
                            var getMethod = (member as PropertyInfo).GetGetMethod();
                            var paramInfos = getMethod.GetParameters();
                            if (paramInfos.Length <= 0)
                                memberValue = getMethod.Invoke(obj, new object[0]);
                            else
                                continue;
                        }
                        break;
                    case MemberTypes.Method:
                    case MemberTypes.Constructor:
                    case MemberTypes.Event:
                    case MemberTypes.TypeInfo:
                    case MemberTypes.Custom:
                    case MemberTypes.NestedType:
                    default:
                        continue;
                }


                var jsonData = ConvetToJson(memberValue, _deep + 1);
                json.Add(member.Name, jsonData);
            }
        }

        private static Json ConvetToJson(object value, int _deep)
        {
            if (value == null)
            {
                return JsonData.Null;
            }

            var type = value.GetType();

            if (type == _boolType)
            {
                return new JsonData((bool)value);
            }

            if (type == _intType)
            {
                return new JsonData((int)value);
            }

            if (type == _longType)
            {
                return new JsonData((long)value);
            }

            if (type == _floatType)
            {
                return new JsonData((float)value);
            }

            if (type == _doubleType)
            {
                return new JsonData((double)value);
            }

            if (type.IsValueType || type.IsEnum || type.IsPrimitive || type == _stringType)
            {
                return new JsonData(value.ToString());
            }

            if (type.GetInterface(typeof(IEnumerable).FullName) != null)
            {
                var nodeArray = new JsonArray();
                var enumer = (value as IEnumerable).GetEnumerator();
                while (enumer.MoveNext())
                {
                    var node = Json.Create();
                    var itemValue = enumer.Current;
                    SerializeToJson(ref node, ref itemValue, _deep + 1);
                    nodeArray.Add(node);
                }
                return nodeArray;
            }


            var finalJsonData = Json.Create();
            SerializeToJson(ref finalJsonData, ref value, _deep + 1);

            return finalJsonData;
        }

        private static bool TryConvertToKeyValuePair(object value, IEnumerable<MemberInfo> memberInfos, int _deep, out KeyValuePair<string, Json> result)
        {
            result = default;
            var keyType = (MemberTypes)(-1);
            var valueType = (MemberTypes)(-1);
            MemberInfo keyInfo = null;
            MemberInfo valueInfo = null;
            var infoEnum = memberInfos.GetEnumerator();
            while (infoEnum.MoveNext())
            {
                if ((int)keyType != -1 && (int)valueType != -1) break;
                if (infoEnum.Current.Name == "Key")
                {
                    keyType = infoEnum.Current.MemberType;
                    keyInfo = infoEnum.Current;
                }
                if (infoEnum.Current.Name == "Value")
                {
                    valueType = infoEnum.Current.MemberType;
                    valueInfo = infoEnum.Current;
                }
            }
            if ((int)keyType != -1 && (int)valueType != -1)
            {
                string keyValue = default;
                object valueValue = default;
                if (keyType == MemberTypes.Field)
                    keyValue = (keyInfo as FieldInfo).GetValue(value).ToString();
                else if (keyType == MemberTypes.Property)
                    keyValue = (keyInfo as PropertyInfo).GetValue(value).ToString();

                if (string.IsNullOrEmpty(keyValue)) return false;

                if (valueType == MemberTypes.Field)
                    valueValue = (valueInfo as FieldInfo).GetValue(value);
                else
                    valueValue = (valueInfo as PropertyInfo).GetValue(value);

                var json = Json.Create();
                SerializeToJson(ref json, ref valueValue, _deep + 1);
                result = KeyValuePair.Create(keyValue, json);
                return true;
            }

            return false;
        }


        private static void ParseFromJson(ref Json json, string str)
        {

        }

        public static void ParseFromJson(ref object obj, ref Json json)
        {

        }

        private static void ParseFromJson(ref object obj, string str)
        {

        }


        public virtual string Value { get => ""; set { } }
        public virtual int Count => 0;

        public virtual Json this[string key] { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
        public virtual Json this[int idx] { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        public virtual Json Add(string key, Json node) { return this; }
        public virtual Json Add(Json node) { return this; }
        public virtual Json Remove(string key) { return this; }
        public virtual Json Remove(Json node) { return this; }
        public virtual Json Get(string key) { throw new NotImplementedException(); }
        public virtual bool ContainsKey(string key) { throw new NotImplementedException(); }
        public virtual bool TryGet(string key, out Json node) { throw new NotImplementedException(); }


        public void SaveToFile(FileStream file)
        {

        }

        public void SaveToFile(StreamWriter streamWriter)
        {

        }

        public void SaveToFile(string path)
        {

        }


        public virtual string AsString
        {
            get => Value;
            set => Value = value;
        }
        public virtual int AsInt
        {
            get
            {
                if (int.TryParse(Value, out var v))
                    return v;
                return 0;
            }
            set => Value = value.ToString();

        }

        public virtual long AsLong
        {
            get
            {
                if (long.TryParse(Value, out var v))
                    return v;
                return 0;
            }
            set => Value = value.ToString();
        }

        public virtual float AsFloat
        {
            get
            {
                if (float.TryParse(Value, out var v))
                    return v;
                return 0f;
            }
            set => Value = value.ToString();
        }
        public virtual double AsDouble
        {
            get
            {
                if (double.TryParse(Value, out var v))
                    return v;
                return 0.0;
            }
            set => Value = value.ToString();
        }
        public virtual bool AsBool
        {
            get
            {
                if (bool.TryParse(Value, out var v))
                    return v;
                return !string.IsNullOrEmpty(Value);
            }
            set => Value = value ? "true" : "false";
        }
        public virtual JsonNode AsJsonNode
        {
            get => this as JsonNode;
        }
        public virtual JsonArray AsJsonArray
        {
            get => this as JsonArray;
        }


        public static implicit operator Json(string str) => new JsonData(str);
        public static implicit operator string(Json node) => (node == null) ? null : node.Value;
        public static bool operator ==(Json a, object b)
        {
            if (a == null && b == null) return true;
            return ReferenceEquals(a, b);
        }
        public static bool operator !=(Json a, object b) => !(a == b);
        public override bool Equals(object obj) => ReferenceEquals(this, obj);
        public override int GetHashCode() => base.GetHashCode();

    }

    public class JsonNode : Json
    {
        private Dictionary<string, Json> _nodes = new Dictionary<string, Json>();

        public override Json this[string key] { get => base[key]; set => base[key] = value; }
        public override Json this[int idx] { get => base[idx]; set => base[idx] = value; }
        public override int Count => _nodes.Count;

        public override Json Add(string key, Json node)
        {
            _nodes.Add(key, node);
            return base.Add(key, node);
        }

        public override string ToString()
        {
            bool first = true;
            var result = "{";
            foreach (var nodeKV in _nodes)
            {
                if (first)
                    first = false;
                else
                    result += ",";

                result += $"\"{nodeKV.Key}\":{nodeKV.Value}";
            }

            return result + "}";
        }

    }

    public class JsonArray : Json
    {
        private List<Json> _items = new List<Json>();

        public override Json this[int idx] { get => base[idx]; set => base[idx] = value; }

        public override int Count => _items.Count;

        public override Json Add(Json node)
        {
            _items.Add(node);
            return base.Add(node);
        }

        public override string ToString()
        {
            var result = "";
            var first = true;
            foreach (var node in _items)
            {
                if (first)
                    first = false;
                else
                    result += ",";
                result += node.ToString();
            }
            if (_items.Count == 1)
                return result;
            return $"[{result}]";
        }
    }

    public class JsonData : Json
    {
        public static readonly JsonData Null = new JsonData("") { isNull = true };

        private string _data;

        private bool isNull;

        private bool isPureString;
        public override string Value { get => _data; set => _data = value; }

        public JsonData(int val) => AsInt = val;
        public JsonData(long val) => AsLong = val;
        public JsonData(float val) => AsFloat = val;
        public JsonData(double val) => AsDouble = val;
        public JsonData(bool val) => AsBool = val;
        public JsonData(string val) { AsString = val; isPureString = true; }

        public override string ToString()
        {
            if (isNull)
                return "null";
            if (isPureString)
                return $"\"{_data}\"";
            return _data;
        }
    }

}
