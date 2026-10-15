using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

namespace Ksema.Framework.LogsArchive
{
    public interface ILogSource
    {
        /// <summary>
        /// Описание журнала
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Название журнала
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Функция архивирования, возвращает лог в XML и очищает данные
        /// </summary>
		/// <param name="withDelete">Удалять данные из БД после выгрузки</param>
		/// <param name="fromDate">Дата c которой архивировать</param>
		/// <param name="toDate">Дата до которой архивировать</param>
        /// <param name="stream">Поток в который необходимо записывать данные</param>
        /// <returns>Есть ли данные</returns>
        bool Archive(bool withDelete, DateTime fromDate, DateTime toDate, Stream stream);
    }
}
