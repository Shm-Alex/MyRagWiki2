using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Castle.Core.Resource;
using System.Configuration;

namespace Ksema.Framework
{
    public static class ObjectContainer
    {
        const string DEFAULT_CONFIG_NAME = "castle";

        static IWindsorContainer s_instance;
        static readonly object s_lock = new object();
		static bool? s_isAvailable;

    	public static bool IsContainerAvailable
        {
            get
            {
            	Initialize();
            	return s_isAvailable ?? false;
            }
        }

    	private static void Initialize()
    	{
    		if (s_isAvailable == null)
    		{
    			lock (s_lock)
    			{
    				if (s_isAvailable == null)
    					InitFromConfig(DEFAULT_CONFIG_NAME);
    			}
    		}
    	}

    	static void InitFromConfig(string sectionName)
        {
			if (ConfigurationManager.GetSection(sectionName) == null)
			{
                // Ну нету конфига, фиг с ним - мы тестируем
                IWindsorContainer cont =
                    new WindsorContainer();

				s_isAvailable = true;
                s_instance = cont;
			}
			else
			{
				IWindsorContainer cont =
					new WindsorContainer(
						new XmlInterpreter(
							new ConfigResource(sectionName)));

				s_instance = cont;
				s_isAvailable = true;
			}
        }

        public static IWindsorContainer Instance
        {
            get 
            {
				Initialize();
                return s_instance;
            }
        }
    }
}
