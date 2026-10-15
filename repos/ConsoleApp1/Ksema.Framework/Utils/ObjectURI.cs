using System;

namespace Ksema.Framework
{
    [Serializable]
    public class ObjectURI : IComparable<ObjectURI>, IComparable, IEquatable<ObjectURI>
    {
		public static string Delimiter = "/";
        string[] _uriParts;
        string _uri;

        public ObjectURI(ObjectURI parentUri, string uri) : this(parentUri._uri + Delimiter + uri)
        {
        }

        public ObjectURI(string uri)
        {
            if (string.IsNullOrEmpty(uri))
                throw new ArgumentNullException("uri");
            _uri = uri.Trim().ToLower();
            if (_uri == String.Empty)
                throw new ArgumentException("Uri cannot be empty");

			_uriParts = _uri.Split(new [] {Delimiter}, StringSplitOptions.RemoveEmptyEntries);
        }

        ObjectURI(string[] parts)
        {
            _uriParts = parts;
			_uri = string.Join(Delimiter, parts);
        }

        public ObjectURI GetParentURI()
        {
            if (_uriParts.Length > 1)
            {
                string[] parentParts = new string[_uriParts.Length - 1];
                for (int i = 0; i < parentParts.Length; i++)
                    parentParts[i] = _uriParts[i];
                return new ObjectURI(parentParts);
            }
            else
                return null;
        }

        public bool IsParentOf(ObjectURI uri)
        {
            return Object.Equals(this, uri.GetParentURI());
        }

		public bool IsPredecessor(ObjectURI uri)
		{
			if (_uriParts.Length > uri._uriParts.Length)
				return false;
			for (int i = 0; i < _uriParts.Length; i++)
				if (_uriParts[i] != uri._uriParts[i])
					return false;
			return true;
		}

        public bool IsAscendantOf(ObjectURI uri)
        {
            if (this._uriParts.Length <= uri._uriParts.Length)
                return false;
            for (int i = 0; i < uri._uriParts.Length; i++)
                if (_uriParts[i] != uri._uriParts[i])
                    return false;
            return true;
        }


        public bool Equals(ObjectURI other)
        {
            if (other == null)
                return false;
            return String.Equals(_uri, other._uri);            
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ObjectURI);
        }

        public override int GetHashCode()
        {
            return _uri.GetHashCode();   
        }

        public override string ToString()
        {
            return _uri;
        }

        public int CompareTo(ObjectURI other)
        {
            if (other == null)
                return 1;
            return this._uri.CompareTo(other._uri);
        }

        int IComparable.CompareTo(object obj)
        {
            return CompareTo((ObjectURI)obj);
        }

        public static ObjectURI Create(object p)
        {
            if (p == null)
                return null;
            if (p is string)
                return new ObjectURI((string)p);
			if (p is IObjectURIProvider)
				return ((IObjectURIProvider)p).CreateURI();			
			return (ObjectURI)p;
        }
    }

    public interface IObjectURIProvider
    {
        ObjectURI CreateURI();
    }
}
