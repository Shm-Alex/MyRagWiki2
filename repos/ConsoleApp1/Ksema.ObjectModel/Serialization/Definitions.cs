using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Ksema.ObjectModel;
namespace Serializer
{
	#region Ошибки
	/// <summary>
	/// Класс ошибок, используемых в сериализации
	/// </summary>
	[Serializable] public class SerializationException : Exception
	{
		/// <summary>
		/// Конструктор, задающий только текст ошибки
		/// </summary>
		/// <param name="message">Текст ошибки</param>
		public SerializationException(string message) : base(message)
		{
		}
	}
	#endregion
	#region Идентификаторы объектов
	/// <summary>
	/// Предоставляет идентификаторы объектов
	/// </summary>
	public interface IIdentityProvider
	{
		/// <summary>
		/// Получить идентификатор объекта
		/// </summary>
		/// <param name="item">Объект, для которого получить идентификатор</param>
		/// <returns>Идентификатор объекта</returns>
		object getIdentity(object item);
	}
	/// <summary>
	/// Фабрика поставщиков идентификаторов
	/// </summary>
	public interface IIdProviderFactory
	{
		/// <summary>
		/// Получить поставщика идентификаторов
		/// </summary>
		/// <param name="item">Объект, для которого получить поставщика</param>
		/// <returns>Поставщик идентификаторов</returns>
		IIdentityProvider getProvider(object item);
	}

	/// <summary>
	/// Задает фабрику поставщиков идентификаторов для класса
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class IdProviderAttribute : Attribute
	{
		private Type type;
		/// <summary>
		/// Задает фабрику поставщиков идентификаторов для класса
		/// </summary>
		/// <param name="_type">Тип фабрики</param>
		public IdProviderAttribute(Type _type)
		{
			if (_type == null)
				throw new ArgumentNullException("type");
			type = _type;
		}
		/// <summary>
		/// Получить экземпляр фабрики
		/// </summary>
		/// <returns>Фабрика поставщиков идентификаторов</returns>
		public IIdProviderFactory getFactory()
		{
			return (IIdProviderFactory) Activator.CreateInstance(type);
		}
	}

	/// <summary>
	/// Фабрика для объектов, реализующих ISimpleObject
	/// </summary>
	public class SimpleObjectIdFactory : IIdProviderFactory
	{
		private static IIdentityProvider provider = new SimpleObjectIdProvider();
		public IIdentityProvider getProvider(object item)
		{
			return item is ISimpleObject ? provider : null;
		}
	}
	/// <summary>
	/// Предоставляет доступ к идентификаторам ISimpleObject
	/// </summary>
	public class SimpleObjectIdProvider : IIdentityProvider
	{
		public object getIdentity(object item)
		{
			if (!(item is ISimpleObject))
				throw new SerializationException("Only for ISimpleObject");
			return (item as ISimpleObject).Key;
		}
	}
	/// <summary>
	/// Выдает целочисленные идентификаторы по порядку начиная с 1
	/// </summary>
	public class EnumerateIdProvider : IIdentityProvider
	{
		private int identity = 1;
		public object getIdentity(object item)
		{
			return identity++;
		}
	}
	/// <summary>
	/// Выдает целочисленные идентификаторы по хэш-функции объекта
	/// </summary>
	public class HashIdProvider : IIdentityProvider
	{
		private int identity = 1;
		private List<int> codes = new List<int>();
		public object getIdentity(object item)
		{
			int code = item == null ? identity++ : item.GetHashCode();
			while (codes.Contains(code))
				code++;
			codes.Add(code);
			return code;
		}
	}
	#endregion
	#region Обработчики композитности ссылок
	/// <summary>
	/// Обработчик композитности ссылок
	/// </summary>
	public interface ILinkWriter
	{
		/// <summary>
		/// Получить тип обработки ссылки - композитно (true) или некомпозитно (false)
		/// </summary>
		/// <param name="link">Ссылка</param>
		/// <param name="backLink">Обратная ссылка</param>
		/// <returns>Тип обработки ссылки - композитно (true) или некомпозитно (false)</returns>
		bool IsComposite(ICSLink link, ICSLink backLink);
		/// <summary>
		/// Обработка ссылка начата
		/// </summary>
		/// <param name="link">Ссылка</param>
		/// <param name="backLink">Обратная ссылка</param>
		void LinkStarted(ICSLink link, ICSLink backLink);
		/// <summary>
		/// Обработка ссылки закончена
		/// </summary>
		/// <param name="link">Ссылка</param>
		/// <param name="backLink">Обратная ссылка</param>
		void LinkFinished(ICSLink link, ICSLink backLink);
	}

	/// <summary>
	/// Обработчик композитности ссылок: по статусу композитности самой ссылки
	/// </summary>
	public class RealLinkWriter : ILinkWriter
	{
		public bool IsComposite(ICSLink link, ICSLink backLink)
		{
			return link.IsComposite;
		}
		public void LinkStarted(ICSLink link, ICSLink backLink)
		{
		}
		public void LinkFinished(ICSLink link, ICSLink backLink)
		{
		}
	}
	/// <summary>
	/// Обработчик композитности ссылок: всё композитно
	/// </summary>
	public class AllCompositeLinkWriter : ILinkWriter
	{
		public bool IsComposite(ICSLink link, ICSLink backLink)
		{
			return true;
		}
		public void LinkStarted(ICSLink link, ICSLink backLink)
		{
		}
		public void LinkFinished(ICSLink link, ICSLink backLink)
		{
		}
	}
	/// <summary>
	/// Обработчик композитности ссылок: 
	/// не более определенного количества некомпозитных
	/// </summary>
	public class MaxNestingLinkWriter : ILinkWriter
	{
		private int max;
		private Stack history;
		public MaxNestingLinkWriter(int _max)
		{
			max = _max;
			history = new Stack();
			history.Push(0);
		}

		public bool IsComposite(ICSLink link, ICSLink backLink)
		{
			if (link.IsComposite)
				return true;
			int value = (int)history.Peek();
			return value < max;
		}
		public void LinkStarted(ICSLink link, ICSLink backLink)
		{
			int value;
			if (!IsComposite(link, backLink) || CSWriter.IsLinkToCompositeParent(link, backLink))
				value = 0;
			else
			{
				value = (int)history.Peek();
				if (!link.IsComposite)
					value += 1;
			}
			history.Push(value);
		}
		public void LinkFinished(ICSLink link, ICSLink backLink)
		{
			history.Pop();
		}
	}
	#endregion
	#region Обработчики внешних объектов
	/// <summary>
	/// Цепочка обработчиков внешних объектов
	/// </summary>
	public interface IExternalObjectsProvider
	{
		/// <summary>
		/// Следующий обработчик в цепочке
		/// </summary>
		IExternalObjectsProvider NextProvider{get; set;}
		/// <summary>
		/// Получить контейнер объектов
		/// </summary>
		/// <param name="node">Xml-элемент, ссылающийся на внешние объекты</param>
		/// <returns>Контейнер внешних объектов</returns>
		IExternalObjects getExternalObjectsInstance(XmlElement node);
	}
	/// <summary>
	/// Контейнер внешних объектов
	/// </summary>
	public interface IExternalObjects
	{
		/// <summary>
		/// Получить объекты
		/// </summary>
		/// <returns>Объекты</returns>
		ICollection getObjects();
	}

	/// <summary>
	/// Базовый класс для цепочки
	/// </summary>
	public abstract class EmptyObjectsProvider : IExternalObjectsProvider
	{
		private IExternalObjectsProvider next;
		public EmptyObjectsProvider()
		{
		}
		public EmptyObjectsProvider(IExternalObjectsProvider _next)
		{
			next = _next;
		}

		public IExternalObjectsProvider NextProvider
		{
			get
			{
				return next;
			}
			set
			{
				next = value;
			}
		}
		public IExternalObjects getExternalObjectsInstance(XmlElement node)
		{
			IExternalObjects instance = getInstance(node);
			return instance != null? instance : next != null ? next.getExternalObjectsInstance(node) : null;
		}
		public abstract IExternalObjects getInstance(XmlElement node);
	}
	/// <summary>
	/// Обработчик внешних объектов из xml-файлов
	/// </summary>
	public class FileObjectsProvider : EmptyObjectsProvider
	{
		public override IExternalObjects getInstance(XmlElement node)
		{
			return node.HasAttribute("path") ? new FileExternalObjects(node.GetAttribute("path")) : null;
		}
	}
	/// <summary>
	/// Контейнер объектов, хранящихся в файле
	/// </summary>
	public class FileExternalObjects : IExternalObjects
	{
		private string file;
		public FileExternalObjects(string _file)
		{
			file = _file;
		}
		public ICollection getObjects()
		{
			return new CSSerializer().Deserialize(file);
		}
	}
	#endregion
}
