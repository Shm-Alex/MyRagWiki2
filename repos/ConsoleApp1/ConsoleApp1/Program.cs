using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Program
    {
        private (string rulename, string metaDataName)[] GetRulnamesByDictionary(string dictionaryNam)
        {
            string rulename = "srtubRuleName";
            string metaDataName = "srtubMetaDataName ";
            return new[]
                {
                (rulename, metaDataName),
                (rulename+"1", metaDataName+"1")};
            }
            
        
        static void Main(string[] args)
        {
            string rulename = "srtubRuleName";
            string metaDataName = "srtubMetaDataName ";
            var p = (rulename, metaDataName);
        }
    }
}
