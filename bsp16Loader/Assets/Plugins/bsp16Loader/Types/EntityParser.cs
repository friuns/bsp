using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ExitGames.Client.Photon;
using UnityEngine;

namespace bsp
{
    public class EntityParser
    {
        private string entities;
        private int length;
        private int position;

        public EntityParser(string entities)
        {
            this.entities = entities;
            length = entities.Length;
            position = 0;
        }

        private bool IsWhiteSpace(char c)
        {
            return ((c == ' ') || (c == '\t') || (c == '\r') || (c == '\n'));
        }

        private void ReadWhiteSpaces()
        {
            while (position < entities.Length && IsWhiteSpace(entities[position]))
            {
                position++;
            }
        }

        private void Expect(char c)
        {
            if (position >= length)
            {
                throw new Exception(string.Format("Expected {0} but reached end", c));
            }
            else if (entities[position] == c)
            {
                position++;
            }
            else
            {
                throw new Exception(string.Format("Expected {0} at position {1}", c, position));
            }
        }

        private void ReadUntil(char c)
        {
            while (entities[position] != c)
            {
                if (position >= length)
                {
                    throw new Exception(string.Format("Expected {0} but reached end", c));
                }
                position++;
            }
            position++;
        }

        private string ReadValue()
        {
            ReadWhiteSpaces();
            Expect('\"');
            int start = position;
            ReadUntil('\"');
            int length = position - start - 1;

            string ret = entities.Substring(start, length);
            return ret;
        }

        public Entity ReadEntity()
        {
            ReadWhiteSpaces();

            if (position == entities.Length || entities[position] != '{')
            {
                return null;
            }

            var dict = new Entity();

            Expect('{');

            dict.Add(ReadValue(), ReadValue());

            ReadWhiteSpaces();

            while (entities[position] != '}')
            {
                string key = ReadValue();
                string val = ReadValue();
                if (dict.ContainsKey(key))
                {
                    if (dict[key] != val) throw new Exception("Missdefined class");
                }
                else
                {
                    dict.Add(key, val);
                }
                ReadWhiteSpaces();
            }
            position++;

            return dict;
        }

        public Dictionary<string, List<Dictionary<string, string>>> ReadEntities()
        {
            Dictionary<string, List<Dictionary<string, string>>> ret = new Dictionary<string, List<Dictionary<string, string>>>();

            foreach (var entity in Entities)
            {
                if (!ret.ContainsKey(entity["classname"]))
                {
                    ret[entity["classname"]] = new List<Dictionary<string, string>>();
                }
                ret[entity["classname"]].Add(entity);
            }
            return ret;
        }

        public IEnumerable<Dictionary<string, string>> Entities
        {
            get
            {
                Dictionary<string, string> entity = null;
                while ((entity = ReadEntity()) != null)
                {
                    yield return entity;
                }
            }
        }
    }
    public class Entity : Dictionary<string, string>
    {
        public string Print()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var a in this)
                sb.Append(a.Key + ":" + a.Value).AppendLine();
            return sb.ToString();
        }

        public new string this[string key]
        {
            get
            {
                string v;
                if (TryGetValue(key, out v))
                    return v;
                return null;
            }
            set { base[key] = value; }
        }

        public int PropertyInteger(string name, int defaultValue = 0)
        {
            string prop;
            if (TryGetValue(name, out prop))
            {
                int d;
                if (int.TryParse(prop.Trim('*'), NumberStyles.Integer, CultureInfo.InvariantCulture, out d))
                    return d;
            }
            return defaultValue;
        }

        public Vector3 PropertyAngles(string name)
        {
            return PropertyVector3Direct(name);
        }
        public Vector3 PropertyVector3(string name)
        {
            var v = PropertyVector3Direct(name);
            return new Vector3(-v.x, v.z, -v.y);
        }
        public Color PropertyColor(string name)
        {
            string prop;
            var c = Color.white;
            if (TryGetValue(name, out prop))
            {
                var d = prop.Split(' ');
                for (int i = 0; i < d.Length; i++)
                    c[i] = float.Parse(d[i]) / 255f;
            }
            return c;

        }
        public Vector3 PropertyVector3Direct(string name)
        {
            string prop;
            if (TryGetValue(name, out prop))
            {
                if (prop == null || prop.Count(c => c == ' ') != 2) return Vector3.zero;
                var split = prop.Replace("[", "").Replace("]", "").Replace("(", "").Replace(")", "").Split(' ');
                float x, y, z;
                if (float.TryParse(split[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x)
                    && float.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y)
                    && float.TryParse(split[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z))
                {
                    return new Vector3(x, y, z);
                }
            }
            return Vector3.zero;
        }
    }
}
