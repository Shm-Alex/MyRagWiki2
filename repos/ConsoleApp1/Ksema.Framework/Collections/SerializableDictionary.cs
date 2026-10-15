using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace Ksema.Framework.Collections
{
    [XmlRoot("dictionary")]
    public class SerializableDictionary<TKey, TValue>: Dictionary<TKey, TValue>, IXmlSerializable
    {
        #region IXmlSerializable Members

        public System.Xml.Schema.XmlSchema GetSchema(){return null;}

        public void ReadXml(System.Xml.XmlReader reader)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));


            bool wasEmpty = reader.IsEmptyElement;
            reader.Read();

            if (wasEmpty)
                return;
            while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
            {
                reader.ReadStartElement("item");
                reader.ReadStartElement("key");
                TKey key = (TKey)keySerializer.Deserialize(reader);
                reader.ReadEndElement();

                reader.ReadStartElement("value");
                TValue value = (TValue)valueSerializer.Deserialize(reader);
                reader.ReadEndElement();
                this.Add(key, value);
                reader.ReadEndElement();
                reader.MoveToContent();
            }
            reader.ReadEndElement();
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));

            foreach (TKey key in this.Keys)
            {
                writer.WriteStartElement("item");
                writer.WriteStartElement("key");
                keySerializer.Serialize(writer, key);
                writer.WriteEndElement();
                writer.WriteStartElement("value");
                TValue value = this[key];
                valueSerializer.Serialize(writer, value);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

        }

        #endregion

    }

	public class UntypedSerializableDictionary<TKey, TValue> : IXmlSerializable
    {
        [DataMember( Name = "Items" )]
        private Dictionary<TKey, TValue> _dict = new Dictionary<TKey, TValue>();

        #region IXmlSerializable Members

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }
        
        public void ReadXml( XmlReader reader )
        {
            reader.Read();
            DataContractSerializer serializer = new DataContractSerializer( typeof( Dictionary<TKey, TValue> ) );
            _dict = (Dictionary<TKey, TValue>)serializer.ReadObject( reader );
        }

        public void WriteXml( XmlWriter writer )
        {
            DataContractSerializer serializer = new DataContractSerializer( typeof( Dictionary<TKey, TValue> ) );
            serializer.WriteObject( writer, _dict );
        }

        #endregion

        public IDictionary<TKey, TValue> InternalDictionary
        {
            get { return _dict; }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                return _dict.Keys;
            }
        }

        public void TryGetValue( TKey key, out TValue value )
        {
            _dict.TryGetValue( key, out value );
        }

        public TValue this[ TKey name ]
        {
            get { return _dict[name]; }
            set { _dict[name] = value; }
        }

        public bool ContainsKey( TKey tkey )
        {
            return _dict.ContainsKey( tkey );
        }
    }
}