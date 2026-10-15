using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Xml;
using Ksema.ObjectModel;
using Framework.Utils;
namespace Serializer
{
	/// <summary>
	/// CS-сериализация/десериализация объектов 
	/// (используется реализация ICSValue и ICSLink 
	/// для определения сохраняемых атрибутов и ссылок соответственно)
	/// </summary>
	public class CSSerializer
	{
		private ICSObjectPropertyManager propManager;
		private MetaData metaData;
		private IIdentityProvider defaultProvider;
		private ILinkWriter linkWriter;
		private IExternalObjectsProvider objectsProvider;
		/// <summary>
		/// Конструктор без параметров
		/// </summary>
		public CSSerializer()
		{
			propManager = CSPropMgr.Instance;
			metaData = new MetaData(this);
		}

		/// <summary>
		/// Сериализовать один объект или набор объектов (реализующий интерфейс ICollection)
		/// в xml-документ
		/// </summary>
		/// <param name="item">Объект/набор объектов</param>
		/// <returns>Xml-документ</returns>
		public XmlDocument Serialize(object item)
		{
			CSWriter writer = new CSWriter(this);
			XmlDocument document = null;
			if (item is ICollection)
			{
				foreach (object element in item as ICollection)
					document = writer.Process(element);
			}
			else
				document = writer.Process(item);
			return document;
		}
		/// <summary>
		/// Сериализовать один объект или набор объектов (реализующий интерфейс ICollection)
		/// используя XmlWriter
		/// </summary>
		/// <param name="item">Объект/набор объектов</param>
		/// <param name="writer">XmlWriter</param>
		public void Serialize(object item, XmlWriter writer)
		{
			XmlDocument document = Serialize(item);
			document.WriteTo(writer);
		}
		/// <summary>
		/// Сериализовать один объект или набор объектов (реализующий интерфейс ICollection)
		/// используя TextWriter
		/// </summary>
		/// <param name="item">Объект/набор объектов</param>
		/// <param name="writer">TextWriter</param>
		public void Serialize(object item, TextWriter writer)
		{
			XmlTextWriter xmlWriter = new XmlTextWriter(writer);
			SerializerUtils.InitIdentation(metaData, xmlWriter);
			Serialize(item, xmlWriter);
		}
		/// <summary>
		/// Сериализовать один объект или набор объектов (реализующий интерфейс ICollection)
		/// в поток используя определенную кодировку
		/// </summary>
		/// <param name="item">Объект/набор объектов</param>
		/// <param name="stream">Поток для записи</param>
		/// <param name="encoding">Используемая кодировка</param>
		public void Serialize(object item, Stream stream, Encoding encoding)
		{
			XmlTextWriter writer = new XmlTextWriter(stream, encoding);
			SerializerUtils.InitIdentation(metaData, writer);
			Serialize(item, writer);
		}
		/// <summary>
		/// Сериализовать один объект или набор объектов (реализующий интерфейс ICollection)
		/// в файл используя определенную кодировку
		/// </summary>
		/// <param name="item">Объект/набор объектов</param>
		/// <param name="fileName">Имя файла</param>
		/// <param name="encoding">Используемая кодировка</param>
		public void Serialize(object item, string fileName, Encoding encoding)
		{
			XmlTextWriter writer = new XmlTextWriter(fileName, encoding);
			SerializerUtils.InitIdentation(metaData, writer);
			try
			{
				Serialize(item, writer);
			}
			finally
			{
				writer.Close();
			}
		}
		/// <summary>
		/// Сериализовать один объект или набор объектов (реализующий интерфейс ICollection)
		/// в файл используя кодировку по умолчанию
		/// </summary>
		/// <param name="item">Объект/набор объектов</param>
		/// <param name="fileName">Имя файла</param>
		public void Serialize(object item, string fileName)
		{
			Serialize(item, fileName, null);
		}

		/// <summary>
		/// Десериализовать из xml-документа используя готовые экземпляры для объектов первого уровня
		/// </summary>
		/// <param name="document">xml-документ</param>
		/// <param name="templateObjects">Экземпляры объектов для заполнения</param>
		/// <returns>Восстановленные объекты</returns>
		public ICollection Deserialize(XmlDocument document, ICollection templateObjects)
		{
			//using (new MetaDataCloner(this))
			return new CSReader(this, templateObjects).Process(document);
		}

        /// <summary>
        /// Десериализовать из xml-документа используя готовые экземпляры для объектов первого уровня
        /// </summary>
        /// <returns>Восстановленные объекты</returns>
        public ICollection Deserialize(TextReader reader, ICollection templateObjects)
        {
            //using (new MetaDataCloner(this))
            XmlDocument document = new XmlDocument();
            document.Load(reader);

            return new CSReader(this, templateObjects).Process(document);
        }

        /// <summary>
        /// Десериализовать из xml-документа используя готовые экземпляры для объектов первого уровня
        /// </summary>
        /// <returns>Восстановленные объекты</returns>
        public ICollection Deserialize(XmlReader reader, ICollection templateObjects)
        {
            //using (new MetaDataCloner(this))
            XmlDocument document = new XmlDocument();
            document.Load(reader);

            return new CSReader(this, templateObjects).Process(document);
        }

		/// <summary>
		/// Десериализовать из xml-документа
		/// </summary>
		/// <param name="document">xml-документ</param>
		/// <returns>Восстановленные объекты</returns>
		public ICollection Deserialize(XmlDocument document)
		{
			return Deserialize(document, null);
		}
		/// <summary>
		/// Десериализовать используя XmlReader
		/// </summary>
		/// <param name="reader">XmlReader</param>
		/// <returns>Восстановленные объекты</returns>
		public ICollection Deserialize(XmlReader reader)
		{
			XmlDocument document = new XmlDocument();
			document.Load(reader);
			return Deserialize(document);
		}
		/// <summary>
		/// Десериализовать используя TextReader
		/// </summary>
		/// <param name="reader">TextReader</param>
		/// <returns>Восстановленные объекты</returns>
		public ICollection Deserialize(TextReader reader)
		{
			XmlDocument document = new XmlDocument();
			document.Load(reader);
			return Deserialize(document);
		}
		/// <summary>
		/// Десериализовать из потока
		/// </summary>
		/// <param name="stream">Поток</param>
		/// <returns>Восстановленные объекты</returns>
		public ICollection Deserialize(Stream stream)
		{
			XmlDocument document = new XmlDocument();
			document.Load(stream);
			return Deserialize(document);
		}
		/// <summary>
		/// Десериализовать из файла используя готовые экземпляры для объектов первого уровня
		/// </summary>
		/// <param name="fileName">Имя файла</param>
		/// <param name="templateObjects">Экземпляры объектов для заполнения</param>
		/// <returns>Восстановленные объекты</returns>
		public ICollection Deserialize(string fileName, ICollection templateObjects)
		{
			XmlDocument document = new XmlDocument();
			document.Load(fileName);
			return Deserialize(document, templateObjects);
		}
		/// <summary>
		/// Десериализовать из файла
		/// </summary>
		/// <param name="fileName">Имя файла</param>
		/// <returns>Восстановленные объекты</returns>
		public ICollection Deserialize(string fileName)
		{
			return Deserialize(fileName, null);
		}


        public static ICollection DeserializeFromResource(string resName, MetaData md)
        {
            CSSerializer ser = new CSSerializer();
            ser.MetaData = md;
            Stream stream = AppUtils.GetResourceStream(resName);
            ICollection result = ser.Deserialize(stream);
            return result;
        }

		#region Настройки, подключаемые алгоритмы и обработчики
		/// <summary>
		/// Контейнер настроек сериализации
		/// </summary>
		public MetaData MetaData
		{
			get
			{
				return metaData;
			}
			set
			{
				metaData = value;
				metaData.Serializer = this;
			}
		}
		/// <summary>
		/// Используемая реализация ICSObjectPropertyManager
		/// </summary>
		public ICSObjectPropertyManager PropManager
		{
			get
			{
				return propManager;
			}
		}
		/// <summary>
		/// Получить или установить поставщика идентификаторов по умолчанию
		/// </summary>
		public IIdentityProvider DefaultProvider
		{
			get
			{
				if (defaultProvider == null)
					defaultProvider = new EnumerateIdProvider();
				return defaultProvider;
			}
			set
			{
				defaultProvider = value;
			}
		}
		/// <summary>
		/// Получить или установить обработчика композитности ссылок
		/// </summary>
		public ILinkWriter LinkWriter
		{
			get
			{
				if (linkWriter == null)
					linkWriter = new AllCompositeLinkWriter();
				return linkWriter;
			}
			set
			{
				linkWriter = value;
			}
		}
		/// <summary>
		/// Получить или заменить обработчика внешних объектов
		/// </summary>
		public IExternalObjectsProvider ExternalObjectsProvider
		{
			get
			{
				if (objectsProvider == null)
					objectsProvider = new FileObjectsProvider();
				return objectsProvider;
			}
			set
			{
				objectsProvider = value;
			}
		}
		/// <summary>
		/// Добавить в начало цепочки нового обработчика внешних обектов
		/// </summary>
		/// <param name="_provider">Новый обработчик внешних обектов</param>
		public void InsertObjectsProvider(IExternalObjectsProvider _provider)
		{
			if (objectsProvider == null)
				objectsProvider = _provider;
			else if (_provider != null)
			{
				IExternalObjectsProvider oldProvider = objectsProvider;
				objectsProvider = _provider;
				while (_provider.NextProvider != null)
					_provider = _provider.NextProvider;
				_provider.NextProvider = oldProvider;
			}
		}
		#endregion

		#region Фабричные методы
		public static CSSerializer CreateSerializerWithRealLinks()
		{
			CSSerializer serializer = new CSSerializer();
			serializer.LinkWriter = new RealLinkWriter();
			return serializer;
		}
		public static CSSerializer CreateSerializerWithAllCompositeLinks()
		{
			CSSerializer serializer = new CSSerializer();
			serializer.LinkWriter = new AllCompositeLinkWriter();
			return serializer;
		}
		public static CSSerializer CreateSerializerWithMaxNestingLinks(int maxNestingLevel)
		{
			CSSerializer serializer = new CSSerializer();
			serializer.LinkWriter = new MaxNestingLinkWriter(maxNestingLevel);
			return serializer;
		}
		#endregion
	}
	public class MetaDataCloner : IDisposable
	{
		private CSSerializer serializer;
		private MetaData oldMetaData;
		public MetaDataCloner(CSSerializer _serializer)
		{
			serializer = _serializer;
			oldMetaData = serializer.MetaData;
			serializer.MetaData = oldMetaData.Clone();
		}
		public void Dispose()
		{
			serializer.MetaData = oldMetaData;
		}
	}
}
