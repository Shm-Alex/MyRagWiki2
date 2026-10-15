using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Xml;
using Ksema.ObjectModel;
namespace Serializer
{
	/// <summary>
	/// Типы отображения атрибутов (ICSValue)
	/// </summary>
	public enum ValueStyle { 
		/// <summary>
		/// На xml-атрибуты
		/// </summary>
		Attribute, 
		/// <summary>
		/// На xml-элементы
		/// </summary>
		Element };
	/// <summary>
	/// Определяет настройки сериализации
	/// </summary>
	public class MetaData
	{
		#region CONST
		/// <summary>
		/// Корневой тег по умолчанию
		/// </summary>
		public const string DefaultRootElement = "data";
		/// <summary>
		/// Xml-атрибут для значений атрибутов по умолчанию
		/// </summary>
		public const string DefaultValueAttribute = "value";
		/// <summary>
		/// Значение xml-атрибута, задающее использование текста вместо атрибутов
		/// </summary>
		public const string UseTextInsteadOfAttribute = "@text";
		/// <summary>
		/// Тег для некомпозитных ссылок по умолчанию
		/// </summary>
		public const string DefaultRefElement = "ref";
		/// <summary>
		/// Тег для ссылок на внешние объекты по умолчанию
		/// </summary>
		public const string DefaultExternalObjectsElement = "external";
		/// <summary>
		/// Xml-атрибут для идентификаторов по умолчанию
		/// </summary>
		public const string DefaultIdAttribute = "id";
		/// <summary>
		/// Значение по умолчанию для индексов
		/// </summary>
		public const int DefaultIndex = Int32.MaxValue;
		/// <summary>
		/// Стиль отображения атрибутов по умолчанию
		/// </summary>
		public const ValueStyle DefaultValueStyle = ValueStyle.Attribute;
		/// <summary>
		/// Значение по умолчанию для исключения "лишних" элементов
		/// </summary>
		public const bool DefaultOmitTags = false;
		/// <summary>
		/// Значение по умолчанию для атрибута объекта "получен по ссылке"
		/// </summary>
		public const string DefaultByRefAttribute = "by";
		/// <summary>
		/// Значение по умолчанию для сохранения всех атрибутов (даже пустых)
		/// </summary>
		public const bool DefaultWriteEmptyValues = false;
		/// <summary>
		/// Значение по умолчанию для сохранения всех ссылок (даже пустых)
		/// </summary>
		public const bool DefaultWriteEmptyLinks = false;
		/// <summary>
		/// Значение по умолчанию для форматирования xml
		/// </summary>
		public const bool DefaultPrettyPrint = true;
		/// <summary>
		/// Имя элемента с настройками сериализации
		/// </summary>
		public const string METADATA = "META";
		/// <summary>
		/// Имя элемента, контейнера настроек для класса (объекта)
		/// </summary>
		public const string OBJECT = "class";
		/// <summary>
		/// Атрибут со значением PretyyPrint в контейнере настроек
		/// </summary>
		public const string PRETTYPRINT = "prettyprint";
		/// <summary>
		/// Атрибут с именем класса в контейнере настроек
		/// </summary>
		public const string TYPE = "type";
		/// <summary>
		/// Атрибут с сохраняемым именем класса (свойства класса) в контейнере настроек
		/// </summary>
		public const string ALIAS = "alias";
		/// <summary>
		/// Атрибут с сохраняемым именем класса для второго конца ассоциации в контейнере настроек
		/// </summary>
		public const string REFTYPEALIAS = "refalias";
		/// <summary>
		/// Атрибут с индексом свойства класса в контейнере настроек
		/// </summary>
		public const string INDEX = "index";
		/// <summary>
		/// Xml-атрибут тега METADATA для сохраниния значения ValueAttribute
		/// </summary>
		public const string VALUE = "value";
		/// <summary>
		/// Xml-атрибут тега METADATA для сохраниния значения RefElement
		/// </summary>
		public const string REF = "ref";
		/// <summary>
		/// Xml-атрибут тега METADATA для сохраниния значения IdAttribute
		/// </summary>
		public const string ID = "id";
		/// <summary>
		/// Xml-атрибут тега METADATA для сохраниния значения ValueStyle
		/// </summary>
		public const string STYLE = "attstyle";
		/// <summary>
		/// Xml-атрибут тега METADATA для сохраниния значения OmitTags
		/// </summary>
		public const string OMIT = "omittags";
		/// <summary>
		/// Xml-атрибут тега METADATA для сохраниния значения ByRefAttribute
		/// </summary>
		public const string BYREF = "byref";
		/// <summary>
		/// Xml-атрибут тега METADATA для сохраниния значения ExternalObjectsElement
		/// </summary>
		public const string EXTERNAL = "external";
		/// <summary>
		/// Xml-атрибут тега METADATA для сохраниния значения WriteEmptyValues
		/// </summary>
		public const string EMPTYVALUES = "emptyvalues";
		/// <summary>
		/// Xml-атрибут тега METADATA для сохраниния значения WriteEmptyLinks
		/// </summary>
		public const string EMPTYLINKS = "emptylinks";
		#endregion
		private IDictionary names = new ListDictionary(); // Type - AliasStorage
		private bool prettyPrint = DefaultPrettyPrint;
		private string rootElement = DefaultRootElement;
		private string valueAttribute = DefaultValueAttribute;
		private string externalObjectsElement = DefaultExternalObjectsElement;
		private string refElement = DefaultRefElement;
		private string idAttribute = DefaultIdAttribute;
		private ValueStyle valueStyle = DefaultValueStyle;
		private bool omitTags = DefaultOmitTags;
		private string byRefAttribute = DefaultByRefAttribute;
		private bool writeEmptyValues = DefaultWriteEmptyValues;
		private bool writeEmptyLinks = DefaultWriteEmptyLinks;
		private bool registerProperties = false;
		private string namespaceURI = null;
		private bool writeMeta = true;
		private CSSerializer serializer;
		/// <summary>
		/// Конструктор контейнера настроек (не предполагается, что он может понадобиться)
		/// Используйте свойство MetaData класса CSSerializer
		/// </summary>
		public MetaData()
		{
		}
		/// <summary>
		/// Конструктор контейнера настроек (не предполагается, что он может понадобиться)
		/// Используйте свойство MetaData класса CSSerializer
		/// </summary>
		/// <param name="_serializer">Экземпляр класса CSSerializer</param>
		public MetaData(CSSerializer _serializer)
		{
			serializer = _serializer;
		}
		/// <summary>
		/// Экземпляр CSSerializer (необходим для получения прочих настроек)
		/// </summary>
		public CSSerializer Serializer
		{
			get
			{
				return serializer;
			}
			set
			{
				serializer = value;
			}
		}

		#region Сохранение/загрузка настроек
		public MetaData Clone()
		{
			MetaData result = (MetaData)this.MemberwiseClone();
			result.names = new ListDictionary();
			foreach (DictionaryEntry entry in this.names)
				result.names.Add(entry.Key, ((AliasStorage)entry.Value).Clone());
			return result;
		}

		/// <summary>
		/// Сохранить все настройки в xml-документе
		/// </summary>
		/// <param name="document">Xml-документ для сохранения настроек</param>
		public void SerializeTo(XmlDocument document)
		{
			if (!writeMeta)
				return;
			XmlElement meta = document.CreateElement(METADATA, NamespaceURI);
			document.DocumentElement.InsertBefore(meta, document.DocumentElement.FirstChild);
			if (namespaceURI != null)
				meta.SetAttribute("nsuri", namespaceURI);
			if (valueAttribute != DefaultValueAttribute)
				meta.SetAttribute(VALUE, valueAttribute);
			if (refElement != DefaultRefElement)
				meta.SetAttribute(REF, refElement);
			if (idAttribute != DefaultIdAttribute)
				meta.SetAttribute(ID, idAttribute);
			if (valueStyle != DefaultValueStyle)
				meta.SetAttribute(STYLE, valueStyle.ToString());
			if (omitTags != DefaultOmitTags)
				meta.SetAttribute(OMIT, omitTags.ToString());
			if (byRefAttribute != DefaultByRefAttribute)
				meta.SetAttribute(BYREF, byRefAttribute);
			if (writeEmptyLinks != DefaultWriteEmptyLinks)
				meta.SetAttribute(EMPTYLINKS, writeEmptyLinks.ToString());
			if (writeEmptyValues != DefaultWriteEmptyValues)
				meta.SetAttribute(EMPTYVALUES, writeEmptyValues.ToString());
			if (externalObjectsElement != DefaultExternalObjectsElement)
				meta.SetAttribute(EXTERNAL, externalObjectsElement);
			if (prettyPrint != DefaultPrettyPrint)
				meta.SetAttribute(PRETTYPRINT, prettyPrint.ToString());
			foreach (DictionaryEntry itemPair in names)
			{
				XmlElement itemNode = document.CreateElement(OBJECT, NamespaceURI);
				meta.AppendChild(itemNode);
				string name = (itemPair.Key as Type).AssemblyQualifiedName;
				itemNode.SetAttribute(TYPE, name);
				AliasStorage storage = itemPair.Value as AliasStorage;
				itemNode.SetAttribute(ALIAS, storage.Alias);
				foreach (DictionaryEntry aliasPair in storage.PropertyAlias)
				{
					XmlElement aliasNode = document.CreateElement(aliasPair.Key as string, NamespaceURI);
					AliasItem aliasItem = aliasPair.Value as AliasItem;
					aliasNode.SetAttribute(ALIAS, aliasItem.Alias);
					if (!Framework.Utils.Utils.isEmpty(aliasItem.RefTypeAlias))
						aliasNode.SetAttribute(REFTYPEALIAS, aliasItem.RefTypeAlias);
					if (aliasItem.Index != DefaultIndex)
						aliasNode.SetAttribute(INDEX, aliasItem.Index.ToString());
					itemNode.AppendChild(aliasNode);
				}
			}
		}
		/// <summary>
		/// Сохранить все настройки используя XmlWriter
		/// </summary>
		/// <param name="writer">XmlWriter для сохранения настроек</param>
		public void SerializeTo(XmlWriter writer)
		{
			XmlDocument document = new XmlDocument();
			document.AppendChild(document.CreateElement(RootElement, NamespaceURI));
			SerializeTo(document);
			document.WriteTo(writer);
		}
		/// <summary>
		/// Сохранить все настройки используя TextWriter
		/// </summary>
		/// <param name="writer">TextWriter для сохранения настроек</param>
		public void SerializeTo(TextWriter writer)
		{
			SerializeTo(new XmlTextWriter(writer));
		}
		/// <summary>
		/// Сохранить все настройки в поток используя определенную кодировку
		/// </summary>
		/// <param name="stream">Поток для записи</param>
		/// <param name="encoding">Используемая кодировка</param>
		public void SerializeTo(Stream stream, Encoding encoding)
		{
			SerializeTo(new XmlTextWriter(stream, encoding));
		}
		/// <summary>
		/// Сохранить все настройки в файл используя определенную кодировку
		/// </summary>
		/// <param name="fileName">Имя файла</param>
		/// <param name="encoding">Используемая кодировка</param>
		public void SerializeTo(string fileName, Encoding encoding)
		{
			XmlTextWriter writer = new XmlTextWriter(fileName, encoding);
			try
			{
				SerializeTo(writer);
			}
			finally
			{
				writer.Close();
			}
		}
		/// <summary>
		/// Сохранить все настройки в файл используя кодировку по умолчанию
		/// </summary>
		/// <param name="fileName">Имя файла</param>
		public void SerializeTo(string fileName)
		{
			SerializeTo(fileName, null);
		}

		/// <summary>
		/// Загрузить все настройки из xml-документа
		/// </summary>
		/// <param name="document">Xml-документ для загрузки настроек</param>
		public void DeserializeFrom(XmlDocument document)
		{
			XmlElement meta = document.DocumentElement.SelectSingleNode(METADATA) as XmlElement;
			if (meta == null)
				return;
			rootElement = document.DocumentElement.Name;
			if (meta.HasAttribute("nsuri"))
				namespaceURI = meta.GetAttribute("nsuri");
			if (meta.HasAttribute("writemeta"))
				writeMeta = Boolean.Parse(meta.GetAttribute("writemeta"));
			if (meta.HasAttribute(VALUE))
				valueAttribute = meta.GetAttribute(VALUE);
			if (meta.HasAttribute(REF))
				refElement = meta.GetAttribute(REF);
			if (meta.HasAttribute(ID))
				idAttribute = meta.GetAttribute(ID);
			if (meta.HasAttribute(STYLE))
				valueStyle = (ValueStyle) Enum.Parse(typeof(ValueStyle), meta.GetAttribute(STYLE));
			if (meta.HasAttribute(OMIT))
				omitTags = Boolean.Parse(meta.GetAttribute(OMIT));
			if (meta.HasAttribute(BYREF))
				byRefAttribute = meta.GetAttribute(BYREF);
			if (meta.HasAttribute(EMPTYLINKS))
				writeEmptyLinks = Boolean.Parse(meta.GetAttribute(EMPTYLINKS));
			if (meta.HasAttribute(EMPTYVALUES))
				writeEmptyValues = Boolean.Parse(meta.GetAttribute(EMPTYVALUES));
			if (meta.HasAttribute(EXTERNAL))
				externalObjectsElement = meta.GetAttribute(EXTERNAL);
			if (meta.HasAttribute(PRETTYPRINT))
				prettyPrint = Boolean.Parse(meta.GetAttribute(PRETTYPRINT));
			foreach (XmlElement itemNode in meta.SelectNodes("*"))
			{
				Type type = AliasStorage.GetTypeFromName(itemNode.Name == OBJECT ? itemNode.GetAttribute(TYPE) : itemNode.Name, itemNode.Name != OBJECT);
				AliasStorage storage = GetStorage(type);
				storage.Alias = itemNode.GetAttribute(ALIAS);
				foreach (XmlElement aliasNode in itemNode.SelectNodes("*"))
				{
					AliasItem aliasItem = storage.GetAliasItem(aliasNode.Name);
					aliasItem.Alias = aliasNode.GetAttribute(ALIAS);
					if (aliasNode.HasAttribute(REFTYPEALIAS))
						aliasItem.RefTypeAlias = aliasNode.GetAttribute(REFTYPEALIAS);
					if (aliasNode.HasAttribute(INDEX))
						aliasItem.Index = Int32.Parse(aliasNode.GetAttribute(INDEX));
				}
			}
		}
		/// <summary>
		/// Загрузить все настройки используя XmlReader
		/// </summary>
		/// <param name="reader">XmlReader для загрузки настроек</param>
		public void DeserializeFrom(XmlReader reader)
		{
			XmlDocument document = new XmlDocument();
			document.Load(reader);
			DeserializeFrom(document);
		}
		/// <summary>
		/// Загрузить все настройки используя TextReader
		/// </summary>
		/// <param name="reader">TextReader для загрузки настроек</param>
		public void DeserializeFrom(TextReader reader)
		{
			XmlDocument document = new XmlDocument();
			document.Load(reader);
			DeserializeFrom(document);
		}
		/// <summary>
		/// Загрузить все настройки из потока
		/// </summary>
		/// <param name="stream">Поток для загрузки настроек</param>
		public void DeserializeFrom(Stream stream)
		{
			XmlDocument document = new XmlDocument();
			document.Load(stream);
			DeserializeFrom(document);
		}
		/// <summary>
		/// Загрузить все настройки из файла
		/// </summary>
		/// <param name="fileName">Имя файла для загрузки настроек</param>
		public void DeserializeFrom(string fileName)
		{
			XmlDocument document = new XmlDocument();
			document.Load(fileName);
			DeserializeFrom(document);
		}
		#endregion
		internal IDictionary Storage
		{
			get
			{
				return names;
			}
		}
		private AliasStorage GetStorage(Type type)
		{
			AliasStorage storage;
			if (names.Contains(type))
				storage = names[type] as AliasStorage;
			else
				names.Add(type, storage = new AliasStorage(type));
			return storage;
		}
		/// <summary>
		/// Получить алиас (сохраняемое имя) для объекта
		/// </summary>
		/// <param name="item">Объект, для которого получается алиас</param>
		/// <returns>Алиас (сохраняемое имя) объекта</returns>
		public string GetName(object item)
		{
			return GetStorage(item.GetType()).Alias;
		}
		/// <summary>
		/// Задать алиас (сохраняемое имя) для класса
		/// </summary>
		/// <param name="type">Класс, для которого задается алиас</param>
		/// <param name="alias">Алиас (сохраняемое имя) класса</param>
		public void SetName(Type type, string alias)
		{
			GetStorage(type).Alias = alias;
		}
		/// <summary>
		/// Определить класс объекта по алиасу (сохраняемому имени)
		/// </summary>
		/// <param name="alias">Алиас (сохраняемое имя)</param>
		/// <returns>Класс объекта</returns>
		public Type GetType(string alias)
		{
			foreach (DictionaryEntry pair in names)
				if ((pair.Value as AliasStorage).Alias == alias)
					return pair.Key as Type;
			return AliasStorage.GetTypeFromName(alias, true);
		}
		private CSPropType GetPropType(ICSPropertyInfo property)
		{
			if (property is ICSValue)
				return CSPropType.Attribute;
			else if (property is ICSLink)
				return CSPropType.Reference;
			else
				throw new SerializationException("Unknown property type - " + property != null ? property.GetType().FullName : "null");
		}
		private AliasItem GetAliasItem(object item, ICSPropertyInfo property)
		{
			string name = property.Name;
			CSPropType propType = GetPropType(property);

			Type type = null;
			AliasStorage storage;
			do
			{
				type = type == null ? item.GetType() : type.BaseType;
				storage = null;
				if (names.Contains(type))
				{
					storage = names[type] as AliasStorage;
					if (storage.PropertyAlias.Contains(name))
						return storage.GetAliasItem(name);
				}
			}
			while (type.BaseType != null && serializer != null && serializer.PropManager.containsProperty(name, propType, type.BaseType));

			if (registerProperties)
			{
				if (storage == null)
					storage = GetStorage(type);
				return storage.GetAliasItem(name);
			}
			return new AliasItem(name);
		}
		/// <summary>
		/// Получить алиас (сохраняемое имя) для свойства (атрибута или ссылки) объекта
		/// с учетом иерархии наследования класса объекта
		/// </summary>
		/// <param name="item">Объект, владелец свойства</param>
		/// <param name="property"> Свойство объекта (атрибут или ссылка)</param>
		/// <returns>Алиас (сохраняемое имя) свойства</returns>
		public string GetName(object item, ICSPropertyInfo property)
		{
			return GetAliasItem(item, property).Alias;
		}
		/// <summary>
		/// Задать алиас (сохраняемое имя) для свойства (атрибута или ссылки) класса
		/// </summary>
		/// <param name="type">Класс, владелец свойства</param>
		/// <param name="name">Имя свойства класса (атрибута или ссылки)</param>
		/// <param name="alias">Алиас (сохраняемое имя) свойства</param>
		public void SetName(Type type, string name, string alias)
		{
			GetStorage(type).GetAliasItem(name).Alias = alias;
		}
		/// <summary>
		/// Получить алиас (сохраняемое имя) для объекта по ссылке
		/// с учетом иерархии наследования класса объекта
		/// </summary>
		/// <param name="item">Объект, владелец свойства</param>
		/// <param name="property"> Ссылка объекта</param>
		/// <returns>Алиас (сохраняемое имя) объекта по ссылке</returns>
		public string GetRefTypeName(object item, ICSLink property)
		{
			return GetAliasItem(item, property).RefTypeAlias;
		}
		/// <summary>
		/// Задать алиас (сохраняемое имя) для объекта по ссылке
		/// </summary>
		/// <param name="type">Класс, владелец свойства</param>
		/// <param name="name">Имя ссылки</param>
		/// <param name="refTypeAlias">Алиас (сохраняемое имя) объекта по ссылке</param>
		public void SetRefTypeName(Type type, string name, string refTypeAlias)
		{
			GetStorage(type).GetAliasItem(name).RefTypeAlias = refTypeAlias;
		}
		/// <summary>
		/// Получить индекс для свойства (атрибута или ссылки) объекта
		/// с учетом иерархии наследования класса объекта
		/// </summary>
		/// <param name="item">Объект, владелец свойства</param>
		/// <param name="property"> Свойство объекта (атрибут или ссылка)</param>
		/// <returns>Индекс свойства</returns>
		public int GetIndex(object item, ICSPropertyInfo property)
		{
			return GetAliasItem(item, property).Index;
		}
		/// <summary>
		/// Задать индекс для свойства (атрибута или ссылки) класса
		/// </summary>
		/// <param name="type">Класс, владелец свойства</param>
		/// <param name="name">Имя свойства класса (атрибута или ссылки)</param>
		/// <param name="index">Индекс свойства</param>
		public void SetIndex(Type type, string name, int index)
		{
			GetStorage(type).GetAliasItem(name).Index = index;
		}
		/// <summary>
		/// При установке этого свойства в True,
		/// в процессе сериализации будут сохранены алиасы по умолчанию
		/// для всех обработанных ссылок и атрибутов (если эти алиасы не заданы явно).
		/// Используйте это свойство, если не хотите набирать вручную имена
		/// ссылок и атрибутов для дальнейшего редактирования.
		/// Обратите внимание, что алиасы по умолчанию для классов
		/// сохраняются в любом случае.
		/// </summary>
		public bool RegisterProperties
		{
			get
			{
				return registerProperties;
			}
			set
			{
				registerProperties = value;
			}
		}

		/// <summary>
		/// Форматировать получающийся XML
		/// </summary>
		public bool PrettyPrint
		{
			get
			{
				return prettyPrint;
			}
			set
			{
				prettyPrint = value;
			}
		}
		/// <summary>
		/// Корневой тег документа
		/// </summary>
		public string RootElement
		{
			get
			{
				return rootElement;
			}
			set
			{
				rootElement = value;
			}
		}
		/// <summary>
		/// Имя тега для ссылок на внешние объекты
		/// </summary>
		public string ExternalObjectsElement
		{
			get
			{
				return externalObjectsElement;
			}
			set
			{
				externalObjectsElement = value;
			}
		}
		/// <summary>
		/// Имя xml-атрибута, сохраняющего значение атрибута ICSValue (при отображении атрибутов на xml-элементы)
		/// </summary>
		public string ValueAttribute
		{
			get
			{
				return valueAttribute;
			}
			set
			{
				valueAttribute = value;
			}
		}
		/// <summary>
		/// Имя тега для некомпозитных ссылок
		/// </summary>
		public string RefElement
		{
			get
			{
				return refElement;
			}
			set
			{
				refElement = value;
			}
		}
		/// <summary>
		/// Xml-атрибут для идентификаторов объектов
		/// </summary>
		public string IdAttribute
		{
			get
			{
				return idAttribute;
			}
			set
			{
				idAttribute = value;
			}
		}
		/// <summary>
		/// Отображать атрибуты (ICSValue) на xml-атрибуты (Attribute) или xml-элементы (Element)
		/// </summary>
		public ValueStyle ValueStyle
		{
			get
			{
				return valueStyle;
			}
			set
			{
				valueStyle = value;
			}
		}
		/// <summary>
		/// Исключать "лишние" элементы
		/// </summary>
		public bool OmitTags
		{
			get
			{
				return omitTags;
			}
			set
			{
				omitTags = value;
			}
		}
		/// <summary>
		/// Xml-атрибут для признака объекта "получен по ссылке"
		/// </summary>
		public string ByRefAttribute
		{
			get
			{
				return byRefAttribute;
			}
			set
			{
				byRefAttribute = value;
			}
		}
		/// <summary>
		/// Сохранять атрибуты (ICSValue), даже если они пустые
		/// </summary>
		public bool WriteEmptyValues
		{
			get
			{
				return writeEmptyValues;
			}
			set
			{
				writeEmptyValues = value;
			}
		}
		/// <summary>
		/// Сохранять ссылки (ICSLink), даже если они пустые
		/// </summary>
		public bool WriteEmptyLinks
		{
			get
			{
				return writeEmptyLinks;
			}
			set
			{
				writeEmptyLinks = value;
			}
		}
		public string NamespaceURI
		{
			get
			{
				return namespaceURI;
			}
			set
			{
				namespaceURI = value;
			}
		}
		public bool WriteMeta
		{
			get
			{
				return writeMeta;
			}
			set
			{
				writeMeta = value;
			}
		}
	}
	class AliasStorage
	{
		private string classAlias;
		private IDictionary propertyAlias;
		public AliasStorage(Type type)
		{
			classAlias = GetNameFromType(type);
			propertyAlias = new ListDictionary();
		}

		public AliasStorage Clone()
		{
			AliasStorage result = (AliasStorage)this.MemberwiseClone();
			result.propertyAlias = new ListDictionary();
			foreach (DictionaryEntry entry in this.propertyAlias)
				result.propertyAlias.Add(entry.Key, ((AliasItem)entry.Value).Clone());
			return result;
		}

		public static string GetNameFromType(Type type)
		{
			return type.FullName + "_" + type.Assembly.GetName().Name;
		}
		public static Type GetTypeFromName(string name, bool replace)
		{
			return Type.GetType(replace ? name.Replace("_", ",") : name, true, true);
		}

		public string Alias
		{
			get
			{
				return classAlias;
			}
			set
			{
				classAlias = value;
			}
		}
		public IDictionary PropertyAlias
		{
			get
			{
				return propertyAlias;
			}
		}
		public AliasItem GetAliasItem(string name)
		{
			AliasItem result;
			if (propertyAlias.Contains(name))
				result = propertyAlias[name] as AliasItem;
			else
				propertyAlias.Add(name, result = new AliasItem(name));
			return result;
		}
	}
	class AliasItem
	{
		private string alias;
		private string refTypeAlias;
		private int index;
		private AliasItem() : this(null)
		{
		}
		public AliasItem(string _alias) : this(_alias, MetaData.DefaultIndex)
		{
		}
		private AliasItem(string _alias, int _index)
		{
			alias = _alias;
			index = _index;
		}

		public AliasItem Clone()
		{
			AliasItem result = (AliasItem)this.MemberwiseClone();
			return result;
		}

		public string Alias
		{
			get
			{
				return alias;
			}
			set
			{
				alias = value;
			}
		}
		public string RefTypeAlias
		{
			get
			{
				return refTypeAlias;
			}
			set
			{
				refTypeAlias = value;
			}
		}
		public int Index
		{
			get
			{
				return index;
			}
			set
			{
				index = value;
			}
		}
	}
}
