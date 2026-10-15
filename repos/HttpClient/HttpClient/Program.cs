using System.IO;
using System.Net.Http;
using System.Text;

namespace HttpClient1
{
    internal class Program
    {
        static HttpClient httpClient = new HttpClient();
        static void Main(string[] args)
        {
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://lib.ru/PROZA/AJTMATOW/verglaz.txt");

            // получаем ответ
            using HttpResponseMessage response =  httpClient.SendAsync(request).Result;

            // просматриваем данные ответа
            // статус
            Console.WriteLine($"Status: {response.StatusCode}\n");
            //заголовки
            Console.WriteLine("Headers");
            foreach (var header in response.Headers)
            {
                Console.Write($"{header.Key}:");
                foreach (var headerValue in header.Value)
                {
                    Console.WriteLine(headerValue);
                }
            }
            // содержимое ответа
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("\nпривет");
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using (var content = response.Content.ReadAsStream()) 
            {
                Encoding win1251 = Encoding.GetEncoding(1251);
                using (StreamReader reader = new StreamReader(content, win1251))
                {
                    //var contentStr =reader.ReadToEnd();
                    //var encodedBytes = Encoding.Convert( win1251, Encoding.UTF8, win1251.GetBytes(contentStr));
                    //Console.WriteLine(Encoding.UTF8.GetString(encodedBytes));
                    Console.WriteLine(reader.ReadToEnd());
                }
            }
            
        }
    }
    
}
