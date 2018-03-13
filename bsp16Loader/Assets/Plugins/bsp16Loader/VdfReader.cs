using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Rectangle = UnityEngine.Rect;
using Box = UnityEngine.Bounds;
namespace bsp
{
    public class VdfReader
    {
        public string key;

        private class GenericStructureProperty
        {
            public string Key { get; set; }
            public string Value { get; set; }
            public GenericStructureProperty(string key, string value)
            {
                Key = key;
                Value = value;
            }
        }
        public string Name { get; private set; }
        private List<GenericStructureProperty> Properties { get; set; }
        public List<VdfReader> Children { get; private set; }
        public string this[string key]
        {
            get
            {
                var prop = Properties.FirstOrDefault(x => x.Key == key);
                return prop == null ? null : prop.Value;
            }
            set
            {
                var prop = Properties.FirstOrDefault(x => x.Key == key);
                if (prop != null) prop.Value = value;
                else Properties.Add(new GenericStructureProperty(key, value));
            }
        }
        public void AddProperty(string key, string value)
        {
            Properties.Add(new GenericStructureProperty(key, value));
        }
        public void RemoveProperty(string key)
        {
            Properties.RemoveAll(x => x.Key == key);
        }
        public VdfReader(string name)
        {
            Name = name;
            Properties = new List<GenericStructureProperty>();
            Children = new List<VdfReader>();
        }
        public IEnumerable<string> GetPropertyKeys()
        {
            return Properties.Select(x => x.Key).Distinct();
        }
        public IEnumerable<string> GetAllPropertyValues(string key)
        {
            return Properties.Where(x => x.Key == key).Select(x => x.Value);
        }
        public string GetPropertyValue(string name, bool ignoreCase)
        {
            var prop = Properties.FirstOrDefault(x => String.Equals(x.Key, name, ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture));
            return prop == null ? null : prop.Value;
        }
        public bool PropertyBoolean(string name, bool defaultValue = false)
        {
            var prop = this[name];
            if (prop == "1") return true;
            if (prop == "0") return false;
            bool d;
            if (bool.TryParse(prop, out d))
            {
                return d;
            }
            return defaultValue;
        }
        //public T PropertyEnum<T>(string name, T defaultValue = default(T)) where T : struct
        //{
        //    var prop = this[name];
        //    T val;

        //    return Enum.TryParse(prop, true, out val) ? val : defaultValue;
        //}
        public int PropertyInteger(string name, int defaultValue = 0)
        {
            string prop = this[name].Trim('*');
            int d;
            if (int.TryParse(prop, NumberStyles.Integer, CultureInfo.InvariantCulture, out d))
            {
                return d;
            }
            return defaultValue;
        }
        public long PropertyLong(string name, long defaultValue = 0)
        {
            var prop = this[name];
            long d;
            if (long.TryParse(prop, NumberStyles.Integer, CultureInfo.InvariantCulture, out d))
            {
                return d;
            }
            return defaultValue;
        }
        public float Propertyfloat(string name, float defaultValue = 0)
        {
            var prop = this[name];
            float d;
            if (float.TryParse(prop, NumberStyles.Float, CultureInfo.InvariantCulture, out d))
            {
                return d;
            }
            return defaultValue;
        }
        public float[] PropertyfloatArray(string name, int count)
        {
            var prop = this[name];
            var defaultValue = Enumerable.Range(0, count).Select(i => 0f).ToArray();
            if (prop == null || prop.Count(c => c == ' ') != (count - 1)) return defaultValue;
            var split = prop.Split(' ');
            for (var i = 0; i < count; i++)
            {
                float d;
                if (float.TryParse(split[i], NumberStyles.Float, CultureInfo.InvariantCulture, out d))
                {
                    defaultValue[i] = d;
                }
            }
            return defaultValue;
        }
        public Plane PropertyPlane(string name)
        {
            var prop = this[name];
            var defaultValue = new Plane(Vector3.forward, 0);
            if (prop == null || prop.Count(c => c == ' ') != 8) return defaultValue;
            var split = prop.Replace("(", "").Replace(")", "").Split(' ');
            float x1, x2, x3, y1, y2, y3, z1, z2, z3;
            if (float.TryParse(split[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x1)
                && float.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y1)
                && float.TryParse(split[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z1)
                && float.TryParse(split[3], NumberStyles.Float, CultureInfo.InvariantCulture, out x2)
                && float.TryParse(split[4], NumberStyles.Float, CultureInfo.InvariantCulture, out y2)
                && float.TryParse(split[5], NumberStyles.Float, CultureInfo.InvariantCulture, out z2)
                && float.TryParse(split[6], NumberStyles.Float, CultureInfo.InvariantCulture, out x3)
                && float.TryParse(split[7], NumberStyles.Float, CultureInfo.InvariantCulture, out y3)
                && float.TryParse(split[8], NumberStyles.Float, CultureInfo.InvariantCulture, out z3))
            {
                return new Plane(
                    new Vector3(x1, y1, z1),
                    new Vector3(x2, y2, z2),
                    new Vector3(x3, y3, z3));
            }
            return defaultValue;
        }
        public Vector3 PropertyVector3(string name, Vector3 defaultValue = default(Vector3))
        {
            var prop = this[name];

            if (prop == null || prop.Count(c => c == ' ') != 2) return defaultValue;
            var split = prop.Replace("[", "").Replace("]", "").Replace("(", "").Replace(")", "").Split(' ');
            float x, y, z;
            if (float.TryParse(split[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x)
                && float.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y)
                && float.TryParse(split[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z))
            {
                return new Vector3(-x, z, -y) * BspGenerateMapVis.scale;
            }
            return defaultValue;
        }
        public Vector3[] PropertyVector3Array(string name, int count)
        {
            var prop = this[name];
            var defaultValue = Enumerable.Range(0, count).Select(i => Vector3.zero).ToArray();
            if (prop == null || prop.Count(c => c == ' ') != (count * 3 - 1)) return defaultValue;
            var split = prop.Split(' ');
            for (var i = 0; i < count; i++)
            {
                float x, y, z;
                if (float.TryParse(split[i * 3], NumberStyles.Float, CultureInfo.InvariantCulture, out x)
                    && float.TryParse(split[i * 3 + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out y)
                    && float.TryParse(split[i * 3 + 2], NumberStyles.Float, CultureInfo.InvariantCulture, out z))
                {
                    defaultValue[i] = new Vector3(x, y, z);
                }
            }
            return defaultValue;
        }
        //public Tuple<Vector3, float, float> PropertyTextureAxis(string name)
        //{
        //    var prop = this[name];
        //    var defaultValue = Tuple.Create(Vector3.UnitX, 0m, 1m);
        //    if (prop == null || prop.Count(c => c == ' ') != 4) return defaultValue;
        //    var split = prop.Replace("[", "").Replace("]", "").Split(' ');
        //    float x, y, z, sh, sc;
        //    if (float.TryParse(split[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x)
        //        && float.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y)
        //        && float.TryParse(split[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z)
        //        && float.TryParse(split[3], NumberStyles.Float, CultureInfo.InvariantCulture, out sh)
        //        && float.TryParse(split[4], NumberStyles.Float, CultureInfo.InvariantCulture, out sc))
        //    {
        //        return Tuple.Create(new Vector3(x, y, z), sh, sc);
        //    }
        //    return defaultValue;
        //}
        public Color PropertyColour(string name, Color defaultValue)
        {
            var prop = this[name];
            if (prop == null || prop.Count(x => x == ' ') != 2) return defaultValue;
            var split = prop.Split(' ');
            int r, g, b;
            if (int.TryParse(split[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out r)
                && int.TryParse(split[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out g)
                && int.TryParse(split[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out b))
            {
                return new Color(r, g, b);
            }
            return defaultValue;
        }
        public IEnumerable<VdfReader> GetChildren(string name = null)
        {
            return Children.Where(x => name == null || String.Equals(x.Name, name, StringComparison.CurrentCultureIgnoreCase));
        }
        public IEnumerable<VdfReader> GetDescendants(string name = null)
        {
            return Children.Where(x => name == null || String.Equals(x.Name, name, StringComparison.CurrentCultureIgnoreCase))
                .Union(Children.SelectMany(x => x.GetDescendants(name)));
        }
        #region Serialise / Deserialise
        public static VdfReader Serialise(object obj)
        {
            return SerialiseHelper(obj, new List<object>());
        }
        private static VdfReader SerialiseHelper(object obj, List<object> encounteredObjects)
        {
            if (Equals(obj, null)) return new VdfReader("Serialise.Null") { Properties = { new GenericStructureProperty("Serialise.Null.Value", "null") } };
            if (encounteredObjects.Contains(obj))
            {
                var rf = new VdfReader("Serialise.Reference");
                rf.AddProperty("Serialise.Reference.Index", (encounteredObjects.IndexOf(obj) + 1).ToString(CultureInfo.InvariantCulture));
                return rf;
            }
            var ty = obj.GetType();
            if (ty.IsPrimitive || ty == typeof(string) || ty == typeof(float))
            {
                var name = "Primitives.";
                if (ty == typeof(bool)) name += "Boolean";
                else if (ty == typeof(char) || ty == typeof(string)) name += "String";
                else name += "Numeric";
                return new VdfReader(name) { Properties = { new GenericStructureProperty("Primitive.Value", Convert.ToString(obj, CultureInfo.InvariantCulture)) } };
            }
            if (ty == typeof(DateTime))
            {
                return new VdfReader("Primitives.DateTime") { Properties = { new GenericStructureProperty("Primitive.Value", ((DateTime)obj).ToString("u")) } };
            }
            if (ty == typeof(Color))
            {
                var color = (Color)obj;
                var col = String.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", color.r, color.g, color.b, color.a);
                return new VdfReader("Primitives.Colour") { Properties = { new GenericStructureProperty("Primitive.Value", col) } };
            }
            if (ty == typeof(Vector3))
            {
                return new VdfReader("Primitives.Vector3") { Properties = { new GenericStructureProperty("Primitive.Value", obj.ToString()) } };
            }
            if (ty == typeof(Box))
            {
                var b = (Box)obj;
                return new VdfReader("Primitives.Box") { Properties = { new GenericStructureProperty("Primitive.Value", b.min + " " + b.max) } };
            }
            if (ty == typeof(Rectangle))
            {
                var r = (Rectangle)obj;
                return new VdfReader("Primitives.Rectangle") { Properties = { new GenericStructureProperty("Primitive.Value", r.x + " " + r.y + " " + r.width + " " + r.height) } };
            }
            if (ty == typeof(Plane))
            {
                var p = (Plane)obj;
                return new VdfReader("Primitives.Plane") { Properties = { new GenericStructureProperty("Primitive.Value", p.normal + " " + p.distance) } };
            }
            encounteredObjects.Add(obj);
            var index = encounteredObjects.Count;
            var enumerable = obj as IEnumerable;
            if (enumerable != null)
            {
                var children = enumerable.OfType<object>().Select(x => SerialiseHelper(x, encounteredObjects));
                var list = new VdfReader("Serialise.List");
                list.AddProperty("Serialise.Reference", index.ToString(CultureInfo.InvariantCulture));
                list.Children.AddRange(children);
                return list;
            }
            var gs = new VdfReader(ty.FullName);
            gs.AddProperty("Serialise.Reference", index.ToString(CultureInfo.InvariantCulture));
            foreach (var pi in ty.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!pi.CanRead) continue;
                var val = pi.GetValue(obj, null);
                var pv = SerialiseHelper(val, encounteredObjects);
                if (pv.Name.StartsWith("Primitives."))
                {
                    gs.AddProperty(pi.Name, pv["Primitive.Value"]);
                }
                else
                {
                    pv.Name = pi.Name;
                    gs.Children.Add(pv);
                }
            }
            return gs;
        }
        public static T Deserialise<T>(VdfReader structure)
        {
            var obj = DeserialiseHelper(typeof(T), structure, new Dictionary<int, object>());
            if (obj is T) return (T)obj;
            obj = Convert.ChangeType(obj, typeof(T));
            if (obj is T) return (T)obj;
            return default(T);
        }
        private static object DeserialiseHelper(Type bindingType, VdfReader structure, Dictionary<int, object> encounteredObjects)
        {
            if (structure.Name == "Serialise.Null" || structure["Serialise.Null.Value"] == "null")
            {
                return bindingType.IsValueType ? Activator.CreateInstance(bindingType) : null;
            }
            var indexProp = structure.Properties.FirstOrDefault(x => x.Key == "Serialise.Reference.Index");
            if (indexProp != null) return encounteredObjects[int.Parse(indexProp.Value)];
            if (structure.Name.StartsWith("Primitives.")) return ConvertPrimitive(structure);
            var refProp = structure.Properties.FirstOrDefault(x => x.Key == "Serialise.Reference");
            var refVal = refProp != null ? int.Parse(refProp.Value) : -1;
            if (structure.Name == "Serialise.List" || typeof(IEnumerable).IsAssignableFrom(bindingType))
            {
                var list = Activator.CreateInstance(bindingType);
                if (refVal >= 0) encounteredObjects[refVal] = list;
                DeserialiseList(list, bindingType, structure, encounteredObjects);
                return list;
            }
            var ctor = bindingType.GetConstructor(Type.EmptyTypes) ?? bindingType.GetConstructors().First();
            var args = ctor.GetParameters().Select(x => x.ParameterType.IsValueType ? Activator.CreateInstance(x.ParameterType) : null).ToArray();
            var instance = ctor.Invoke(args);
            if (refVal >= 0) encounteredObjects[refVal] = instance;
            foreach (var pi in bindingType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!pi.CanWrite) continue;
                var prop = structure.Properties.FirstOrDefault(x => x.Key == pi.Name);
                var child = structure.Children.FirstOrDefault(x => x.Name == pi.Name);
                if (prop != null)
                {
                    var prim = ConvertPrimitive(pi.PropertyType, prop.Value);
                    pi.SetValue(instance, Convert.ChangeType(prim, pi.PropertyType), null);
                }
                else if (child != null)
                {
                    var obj = DeserialiseHelper(pi.PropertyType, child, encounteredObjects);
                    pi.SetValue(instance, obj, null);
                }
            }
            return instance;
        }
        private static void DeserialiseList(object instance, Type bindingType, VdfReader structure, Dictionary<int, object> encounteredObjects)
        {
            Type listType = null;
            if (bindingType.IsGenericType) listType = bindingType.GetGenericArguments()[0];
            var children = structure.Children.Select(x =>
            {
                var name = x.Name;
                var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).FirstOrDefault(t => t != null) ?? (listType ?? typeof(object));
                var result = DeserialiseHelper(type, x, encounteredObjects);
                return Convert.ChangeType(result, type);
            }).ToList();
            if (typeof(IList).IsAssignableFrom(bindingType))
            {
                foreach (var child in children) ((IList)instance).Add(child);
            }
            else if (typeof(Array).IsAssignableFrom(bindingType))
            {
                var arr = (object[])instance;
                Array.Resize(ref arr, children.Count);
                children.CopyTo(arr);
            }
        }
        private static object ConvertPrimitive(VdfReader structure)
        {
            var prim = structure.Name.Substring("Primitives.".Length);
            var value = structure["Primitive.Value"];
            return ConvertPrimitive(prim, value);
        }
        private static object ConvertPrimitive(Type type, string value)
        {
            return ConvertPrimitive(GetPrimitiveName(type), value);
        }
        private static object ConvertPrimitive(string primitiveType, string value)
        {
            var spl = value.Split(' ');
            switch (primitiveType)
            {
                case "Boolean":
                    return Boolean.Parse(value);
                case "String":
                    return value;
                case "Numeric":
                    return float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
                case "DateTime":
                    return DateTime.ParseExact(value, "u", CultureInfo.InvariantCulture);
                case "Colour":
                    return new Color(int.Parse(spl[3]), int.Parse(spl[0]), int.Parse(spl[1]), int.Parse(spl[2]));
                case "Vector3":
                    return Vector3Parse(spl[0].TrimStart('('), spl[1], spl[2].TrimEnd(')'));
                case "Box":
                    return new Box(
                        Vector3Parse(spl[0].TrimStart('('), spl[1], spl[2].TrimEnd(')')),
                        Vector3Parse(spl[3].TrimStart('('), spl[4], spl[5].TrimEnd(')')));
                case "Plane":
                    return new Plane(Vector3Parse(spl[0].TrimStart('('), spl[1], spl[2].TrimEnd(')')), float.Parse(spl[3]));
                case "Rectangle":
                    return new Rectangle(int.Parse(spl[0]), int.Parse(spl[1]), int.Parse(spl[2]), int.Parse(spl[3]));
                default:
                    throw new ArgumentException();
            }
        }
        private static Vector3 Vector3Parse(string x, string y, string z)
        {
            return new Vector3(float.Parse(x), float.Parse(y), float.Parse(z));
        }
        private static string GetPrimitiveName(Type ty)
        {
            if (ty == typeof(bool)) return "Boolean";
            if (ty == typeof(char) || ty == typeof(string)) return "String";
            if (ty.IsPrimitive || ty == typeof(float)) return "Numeric";
            if (ty == typeof(DateTime)) return "DateTime";
            if (ty == typeof(Color)) return "Colour";
            if (ty == typeof(Vector3)) return "Vector3";
            if (ty == typeof(Box)) return "Box";
            if (ty == typeof(Plane)) return "Plane";
            if (ty == typeof(Rectangle)) return "Rectangle";
            throw new ArgumentException();
        }
        #endregion
        #region Printer
        public override string ToString()
        {
            var sw = new StringWriter();
            PrintToStream(sw);
            return sw.ToString();
        }
        public void PrintToStream(TextWriter tw)
        {
            Print(tw);
        }
        private static string LengthLimit(string str, int limit)
        {
            if (str.Length >= limit) return str.Substring(0, limit - 1);
            return str;
        }
        private void Print(TextWriter tw, int tabs = 0)
        {
            var preTabStr = new string(' ', tabs * 4);
            var postTabStr = new string(' ', (tabs + 1) * 4);
            tw.Write(preTabStr);
            tw.WriteLine(Name);
            tw.Write(preTabStr);
            tw.WriteLine("{");
            foreach (var kv in Properties)
            {
                tw.Write(postTabStr);
                tw.Write('"');
                tw.Write(LengthLimit(kv.Key, 1024));
                tw.Write('"');
                tw.Write(' ');
                tw.Write('"');
                tw.Write(LengthLimit((kv.Value ?? "").Replace('"', '`'), 1024));
                tw.Write('"');
                tw.WriteLine();
            }
            foreach (var child in Children)
            {
                child.Print(tw, tabs + 1);
            }
            tw.Write(preTabStr);
            tw.WriteLine("}");
        }
        #endregion
        #region Parser
        public static IEnumerable<VdfReader> Parse(string filePath)
        {
            using (var reader = new StringReader(filePath))
            {
                return Parse(reader).ToList();
            }
        }
        public static IEnumerable<VdfReader> Parse(TextReader reader)
        {
            string line;
            while ((line = CleanLine(reader.ReadLine())) != null)
            {
                if (ValidStructStartString(line))
                {
                    yield return ParseStructure(reader, line);
                }
            }
        }
        private static string CleanLine(string line)
        {
            if (line == null) return null;
            var ret = line;
            if (ret.Contains("//")) ret = ret.Substring(0, ret.IndexOf("//")); // Comments
            return ret.Trim();
        }
        private static VdfReader ParseStructure(TextReader reader, string name)
        {
            var spl = name.SplitWithQuotes();
            var gs = new VdfReader(spl[0]);
            string line;
            if (spl.Length != 2 || spl[1] != "{")
            {
                do
                {
                    line = CleanLine(reader.ReadLine());
                } while (IsNullOrWhiteSpace(line));
                if (line != "{")
                {
                    return gs;
                }
            }
            while ((line = CleanLine(reader.ReadLine())) != null)
            {
                if (line == "}") break;
                if (ValidStructPropertyString(line)) ParseProperty(gs, line);
                else if (ValidStructStartString(line)) gs.Children.Add(ParseStructure(reader, line));
            }
            return gs;
        }
        private static bool ValidStructStartString(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            var split = s.SplitWithQuotes();
            return split.Length == 1 || (split.Length == 2 && split[1] == "{");
        }
        private static bool ValidStructPropertyString(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            var split = s.SplitWithQuotes();
            return split.Length == 2;
        }
        private static void ParseProperty(VdfReader gs, string prop)
        {
            var split = prop.SplitWithQuotes();
            gs.Properties.Add(new GenericStructureProperty(split[0], (split[1] ?? "").Replace('`', '"')));
        }
        #endregion
        public static bool IsNullOrWhiteSpace(string value)
        {
            if (value == null) return true;
            return string.IsNullOrEmpty(value.Trim());
        }
    }

    public static class StringExtensions
    {
        public static string[] SplitWithQuotes(this string line, char[] splitCharacters = null, char quoteChar = '"')
        {
            if (splitCharacters == null) splitCharacters = new[] { ' ', '\t' };
            //try
            //{
            var result = new List<string>();

            int i;
            for (i = 0; i < line.Length; i++)
            {
                var split = line.IndexOfAny(splitCharacters, i);
                var quote = line.IndexOf(quoteChar, i);

                if (split < 0) split = line.Length;
                if (quote < 0) quote = line.Length;

                if (quote < split)
                {

                    if (quote > i) result.Add(line.Substring(i, quote));
                    var nextQuote = line.IndexOf(quoteChar, quote + 1);
                    if (nextQuote < 0) nextQuote = line.Length;
                    result.Add(line.Substring(quote + 1, nextQuote - quote - 1));
                    i = nextQuote;
                }
                else
                {
                    if (split > i) result.Add(line.Substring(i, split - i));
                    i = split;
                }
            }
            return result.ToArray();
            //}catch (Exception)
            //{
            //    return new string[0];
            //}
        }

    }
}