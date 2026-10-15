using System.Linq;
using System.Text.RegularExpressions;
namespace RegexpForListParcing
{
    internal class Program
    {
        static String BuildPathRegex(IEnumerable<String> listPath)
        {
            if (listPath == null || listPath.Count() == 0)
            {
                return String.Empty;
            }

            // Экранируем каждую часть пути
            var escapedParts = new List<string>();
            foreach (string part in listPath)
            {
                escapedParts.Add(Regex.Escape(part));
            }

            // Шаблон для части: ("part" | part)
            string partPattern = "(\"{0}\"|{0})";

            // Шаблон разделителя: \ с возможными пробелами по бокам
            string separatorPattern = @"(\s*\\\s*)";

            // Шаблон для опциональной группы: (%1)?
            string optionalGroupPattern = "({0})?";

            // Имя объекта (последний элемент пути) — обязательно
            string objectPattern = string.Format(partPattern, escapedParts[0]);

            // Рекурсивно строим опциональные части пути (от конца к началу, сгущая слои)
            string recursiveOptionalPattern = string.Empty;
            ///["a","b"] надор построить 
            ///( ((("a"|a)(\s*\\\s*))?("b"|b)(\s*\\\s*) )?
            ///0 recursiveOptionalPattern0 =""
            ///1  recursiveOptionalPattern1 =(recursiveOptionalPattern0(("a"|a)(\s*\\\s*)))? =
            ///2 recursiveOptionalPattern2= (recursiveOptionalPattern1("b"|b)(\s*\\\s*))?
            // Начинаем с индекса 1 (второй элемент), потому что первый — это объект
            for (int i = escapedParts.Count-1; i >0;i-- )
            {
                //string currentPart = string.Format(optionalGroupPattern,string.Format(partPattern, escapedParts[i])+ separatorPattern); //(("a"|a)(\s*\\\s*))?
                string currentPart = string.Format(partPattern, escapedParts[i]) + separatorPattern; //("a"|a)(\s*\\\s*)
                //
                recursiveOptionalPattern = string.Format(optionalGroupPattern, recursiveOptionalPattern + currentPart);


            }

            // Финальный шаблон: ,\s*'?\s*(%1)\s*'?\s*(,|:|\))
            // Пример: ,' path ', или ,path), или :'path':
            string finalPattern = @",\s*'\s*\\?\s*({0})\s*'\s*(,|:|\))";

            // Собираем: рекурсивный путь + обязательное имя объекта
            string listPattern = recursiveOptionalPattern + objectPattern;
            string finalExpression = string.Format(
                finalPattern,
                listPattern
            );

            // Для отладки — выводим сгенерированное выражение
            return finalExpression;

        }

        static void Main(string[] args)
        {
           // Console.WriteLine(BuildPathRegex("tessy/qwerty/ObjectName".Split('/').Reverse()));
            Console.WriteLine(BuildPathRegex("folder1/folder2/folder3/ObjectName".Split('/').Reverse()));
        }
    }
}
