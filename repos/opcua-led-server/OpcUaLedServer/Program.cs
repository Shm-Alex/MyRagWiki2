using Microsoft.AspNetCore.Hosting.Server;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace OpcUaLedServer
{
    class LedNodeState : BaseDataVariableState<bool>
    {
        public LedNodeState(NodeState parent) : base(parent)
        {
            SymbolicName = "LED_Control";
            NodeId = new NodeId("LED_Control", 2);
            BrowseName = new QualifiedName("LED_Control", 2);
            DisplayName = new LocalizedText("en", "LED Control");
            Description = new LocalizedText("en", "Controls the LED state (true = ON, false = OFF)");
            WriteMask = AttributeWriteMask.None;
            UserWriteMask = AttributeWriteMask.None;
            DataType = DataTypeIds.Boolean;
            ValueRank = ValueRanks.Scalar;
            AccessLevel = AccessLevels.CurrentRead | AccessLevels.CurrentWrite;
            UserAccessLevel = AccessLevels.CurrentRead | AccessLevels.CurrentWrite;
            MinimumSamplingInterval = 100;
            Historizing = false;
            Value = false; // Начальное состояние: выключено
        }

        // Обработчик записи значения
        public override void OnSimpleWrite(
            ISystemContext context,
            NodeState node,
            ref bool value)
        {
            Console.ForegroundColor = value ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] LED изменён: {value}");
            Console.ResetColor();

            base.OnSimpleWrite(context, node, ref value);
        }
    }

    class LedServer : StandardServer
    {
        protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            return new MasterNodeManager(server, configuration, null, new INodeIdFactory[] { new NodeIdFactory() });
        }

        protected override ServerProperties LoadServerProperties()
        {
            var properties = new ServerProperties
            {
                ManufacturerName = "HomeLab",
                ProductName = "LED Control Server",
                ProductUri = "http://homelab/led-server",
                SoftwareVersion = "1.0",
                BuildNumber = "1",
                BuildDate = DateTime.Now
            };
            return properties;
        }

        protected override void OnServerStarting(ApplicationConfiguration configuration)
        {
            base.OnServerStarting(configuration);

            // Получаем локальный IP
            string localIp = GetLocalIpAddress();
            Console.WriteLine($"🌐 Локальный IP: {localIp}");
            Console.WriteLine();

            // Добавляем узел светодиода в адресное пространство
            var ledNode = new LedNodeState(null);
            ledNode.Create(this.ServerSystemContext, null, ledNode.BrowseName, null, true);

            // Добавляем узел в папку Objects
            Server.SystemContext.NodeManager.AddPredefinedNode(SystemContext, ledNode);
        }

        private string GetLocalIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    return ip.ToString();
            }
            return "127.0.0.1";
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("╔════════════════════════════════════════════════╗");
            Console.WriteLine("║   OPC UA LED Server (Official Stack)          ║");
            Console.WriteLine("╚════════════════════════════════════════════════╝");
            Console.WriteLine();

            try
            {
                // Загружаем конфигурацию
                var appDescription = new ApplicationDescription
                {
                    ApplicationName = "LEDServer",
                    ApplicationUri = "urn:LEDServer",
                    ApplicationType = ApplicationType.Server
                };

                var certificateValidator = new CertificateValidator();
                certificateValidator.CertificateValidation += (sender, e) =>
                {
                    // Для учебного проекта принимаем все сертификаты
                    e.Accept = true;
                };

                var config = new ApplicationConfiguration
                {
                    ApplicationName = "LEDServer",
                    ApplicationType = ApplicationType.Server,
                    SecurityConfiguration = new SecurityConfiguration
                    {
                        ApplicationCertificate = new CertificateIdentifier { StoreType = CertificateStoreType.Directory, StorePath = "pki/own", SubjectName = "CN=LEDServer" },
                        TrustedPeerCertificates = new CertificateTrustList { StoreType = CertificateStoreType.Directory, StorePath = "pki/trusted" },
                        TrustedIssuerCertificates = new CertificateTrustList { StoreType = CertificateStoreType.Directory, StorePath = "pki/issuers" },
                        RejectedCertificateStore = new CertificateTrustList { StoreType = CertificateStoreType.Directory, StorePath = "pki/rejected" },
                        AutoAcceptUntrustedCertificates = true,
                        MinimumCertificateKeySize = 1024
                    },
                    TransportConfigurations = new TransportConfigurationCollection(),
                    TransportQuotas = new TransportQuotas { OperationTimeout = 120000, MaxStringLength = 1048576, MaxByteStringLength = 1048576, MaxArrayLength = 65535, MaxMessageSize = 4194304, MaxBufferSize = 65535, ChannelLifetime = 300000, SecurityTokenLifetime = 3600000 },
                    ServerConfiguration = new ServerConfiguration
                    {
                        ServerAddresses = new StringCollection { "opc.tcp://0.0.0.0:62541/LEDServer" },
                        SecurityPolicies = new ServerSecurityPolicyCollection
                        {
                            new ServerSecurityPolicy { SecurityMode = MessageSecurityMode.None, SecurityPolicyUri = SecurityPolicies.None }
                        },
                        UserTokenPolicies = new UserTokenPolicyCollection
                        {
                            new UserTokenPolicy { TokenType = UserTokenType.Anonymous }
                        },
                        DiagnosticsEnabled = true,
                        MaxSessionCount = 100,
                        MaxSubscriptionCount = 100,
                        MinSessionTimeout = 10000,
                        MaxSessionTimeout = 3600000
                    },
                    CertificateValidator = certificateValidator,
                    DisableHiResClock = true
                };

                config.Validate(ApplicationType.Server).GetAwaiter().GetResult();

                // Создаём и запускаем сервер
                var server = new LedServer();
                server.Start(config).GetAwaiter().GetResult();

                Console.WriteLine($"✅ Сервер запущен: opc.tcp://{GetLocalIp()}:62541/LEDServer");
                Console.WriteLine($"   Узел: ns=2;s=LED_Control");
                Console.WriteLine();
                Console.WriteLine("💡 Подключайтесь с другого устройства:");
                Console.WriteLine($"   opc.tcp://{GetLocalIp()}:62541/LEDServer");
                Console.WriteLine();
                Console.WriteLine("⚠️  Разрешите порт 62541 в брандмауэре!");
                Console.WriteLine();
                Console.WriteLine("Нажмите 'q' для выхода...");
                Console.WriteLine("──────────────────────────────────────────────────");
                Console.WriteLine();

                // Ждём команды выхода
                while (true)
                {
                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q)
                        break;
                    Thread.Sleep(100);
                }

                server.Stop();
                Console.WriteLine("Сервер остановлен. До свидания!");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ошибка: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
                Console.ReadKey();
            }
        }

        private static string GetLocalIp()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    return ip.ToString();
            }
            return "127.0.0.1";
        }
    }
}