using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Ksema.Framework.Diagnostics;

namespace Ksema.Framework.Utils
{
    public class GetRespFromService
    {
        private IPEndPoint _ipEndPoint;
        private IPAddress _ipAddr;
        private int _port;
        private object _locker = new object();
        private Dictionary<string, List<Socket>> _clientSocketDict;
        private Dictionary<string, List<int>> _updatedDimsDict;
        private Dictionary<string, Socket> _exportClientDims;
        //private Dictionary<string, Dictionary<Socket, List<int>>> _changesDict;
        private bool _isListening;
        private bool _isCheck;
        private Socket _sListener;
        private Socket _clientSocket;
        private string _username;

        public GetRespFromService(int port)
        {
            _port = port;
            _isCheck = false;
            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
            _username = Environment.UserName;
            _ipAddr = ipHost.AddressList[1];
            _ipEndPoint = new IPEndPoint(_ipAddr, _port);
            _clientSocketDict = new Dictionary<string, List<Socket>>();
            _updatedDimsDict = new Dictionary<string, List<int>>();
            _exportClientDims = new Dictionary<string, Socket>();
            _clientSocket = new Socket(_ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        public IPAddress ServerIP => _ipAddr;
        public bool IsListening => _isListening;

        public bool IsExistSocket(string tid) => _exportClientDims.ContainsKey(tid);

        // Прослушивание подключений
        void DoWaitResponse()
        {
            try
            {
                _sListener = new Socket(_ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _isListening = true;
                _sListener.Bind(_ipEndPoint);
                _sListener.Listen(10);

                while (true)
                {
                    //StopListening(sListener);
                    Socket clientSocket = _sListener.Accept();

                    BackgroundWorker bg = new BackgroundWorker();
                    bg.DoWork += (x, y) => { HandleClient(clientSocket); };
                    bg.RunWorkerAsync();
                    Thread.Sleep(0);

                    if (!_isCheck)
                        CheckStopListening(_sListener);
                }
            }
            catch (Exception e)
            {
                Tracer.Error(e);
                Trace.WriteLine(e);
            }
        }


        // Обработка подключенных клиентов
        public void HandleClient(Socket clientSocket)
        {
            while (true)
            {
                try
                {
                    byte[] bytes = new byte[1024];
                    int bytesRec = clientSocket.Receive(bytes);
                    if (bytesRec == 0)
                        return;
                    string answer = Encoding.UTF8.GetString(bytes, 0, bytesRec);
                    var dt = answer.Split(new[] {"|"}, StringSplitOptions.None);
                    string operation = dt[0];

                    if (operation == "Export")
                    {
                        var TID = dt[1];
                        lock (_locker)
                        {
                            if (!_exportClientDims.ContainsKey(TID))
                            {
                                _exportClientDims.Add(TID, clientSocket);
                                Thread.Sleep(50);
                            }
                        }
                          
                    }
                    else
                    {
                        string dimName = dt[1];

                        if ((operation == "Send" || operation == "Clear" || operation == "CloseDim" ||
                             operation == "Save") && dt.Length > 3) // Если клиент изменяет справочник
                        {
                            var id = dt[2];
                            var username = dt[3];
                            if (_clientSocketDict.ContainsKey(dimName))
                                UpdateDimsDict(dimName, id, operation);

                            string message = $"{dimName}|{id}|{operation}|{username}";
                            SendMessageToAllClients(dimName, message, clientSocket);
                        }
                        else if (operation == "Connect") // Если клиент подключился к справочнику
                        {
                            var username = dt[2];
                            AddConnectSocket(clientSocket, dimName);
                            SendUpdateDims(dimName, clientSocket, username);
                        }
                        else if (operation == "Disconnect") // Если клиент отключается от справочника
                        {
                            if (_clientSocketDict.ContainsKey(dimName))
                            {
                                if (_clientSocketDict[dimName].Contains(clientSocket))
                                    lock (_locker)
                                        _clientSocketDict[dimName].Remove(clientSocket);
                                if (dt.Length > 2)
                                    UpdateDimsDict(dimName, dt[2], "Disconnect");

                                if (_clientSocketDict[dimName].Count == 0)
                                {
                                    lock (_locker)
                                        _clientSocketDict.Remove(dimName);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Tracer.Error(e);
                    Trace.WriteLine(e);
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                    foreach (var key in _clientSocketDict.Keys)
                    {
                        lock (_locker)
                        {
                            if (_clientSocketDict[key].Contains(clientSocket))
                            {
                                if (_updatedDimsDict.ContainsKey(key))
                                {
                                    _updatedDimsDict.Remove(key);
                                }

                                _clientSocketDict[key].Remove(clientSocket);
                                if (_clientSocketDict[key].Count == 0)
                                {
                                    _clientSocketDict.Remove(key);
                                }
                            }
                        }
                    }

                    break;
                }
            }
        }


        // Отправляем сообщение всем клиентам, кроме отправителя
        private void SendMessageToAllClients(string dimName, string message, Socket senderSocket)
        {
            if (_clientSocketDict.ContainsKey(dimName))
            {
                foreach (Socket socket in _clientSocketDict[dimName])
                {
                    if (socket != senderSocket)
                    {
                        // Проверяем не отключился ли клиент
                        if (socket.Connected)
                        {
                            socket.Send(Encoding.UTF8.GetBytes(message));
                        }
                    }
                }
            }
        }


        // Ожидание подключений клиентов в нескольких потоках
        public void WaitResponse()
        {
            BackgroundWorker bg = new BackgroundWorker();
            bg.DoWork += (x, y) => { DoWaitResponse(); };
            bg.RunWorkerAsync();
            Thread.Sleep(0);
        }

        public delegate void DelegateUpdateNode(string dimName, List<int> updId, string username);

        public event DelegateUpdateNode ObjectUpdate;
        public event DelegateUpdateNode ObjectCloaseUpdate;
        public event DelegateUpdateNode ObjectSaveUpdate;


        public void Send(string dimName, int id)
        {
            try
            {
                string message = string.Format("Send|{0}|{1}|{2}", dimName, id, _username);
                byte[] msg = Encoding.UTF8.GetBytes(message);
                // Отправляем данные через сокет 
                _clientSocket.Send(msg);
            }
            catch (Exception e)
            {
                Tracer.Error(e);
                Trace.WriteLine(e);
            }
        }


        // Обработка подключенных клиентов
        public void DoConnectClient(IPAddress serverIP, string dimName)
        {
            try
            {
                // Подключение клиента если он еще не подключен
                if (!_clientSocket.Connected)
                    _clientSocket.Connect(serverIP, _port);
                // Клиент сообщает серверу к какому справочнику подключился
                string message = string.Format("Connect|{0}|{1}", dimName, _username);
                byte[] msg = Encoding.UTF8.GetBytes(message);
                _clientSocket.Send(msg);

                while (true)
                {
                    byte[] bytes = new byte[1024];
                    int bytesRec = _clientSocket.Receive(bytes);
                    if (bytesRec == 0) return;
                    string answer = Encoding.UTF8.GetString(bytes, 0, bytesRec);
                    var dt = answer.Split(new[] {"|"}, StringSplitOptions.RemoveEmptyEntries);
                    if (dt.Length > 3)
                    {
                        string name = dt[0];
                        string operation = dt[2];
                        string username = dt[3];
                        switch (operation)
                        {
                            case "Send":
                            {
                                int.TryParse(dt[1], out int id);
                                var updId = new List<int> {id};
                                ObjectUpdate?.Invoke(name, updId, username);
                                break;
                            }
                            case "Clear":
                            {
                                int.TryParse(dt[1], out int id);
                                var updId = new List<int> {id};
                                ObjectCloaseUpdate?.Invoke(name, updId, username);
                                break;
                            }
                            case "Save":
                            {
                                var updId = dt[1].Split(new[] {"/"}, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(id => Convert.ToInt32(id)).ToList();
                                ObjectSaveUpdate?.Invoke(name, updId, username);
                                break;
                            }
                            case "CloseDim":
                            {
                                var updId = dt[1].Split(new[] {"/"}, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(id => Convert.ToInt32(id)).ToList();
                                ObjectCloaseUpdate?.Invoke(name, updId, username);
                                break;
                            }
                            case "Reload":
                            {
                                var updId = dt[1].Split(new[] {"/"}, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(id => Convert.ToInt32(id)).ToList();
                                ObjectUpdate?.Invoke(name, updId, username);
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Tracer.Error(e);
                Trace.WriteLine(e);
            }
        }


        public void ClientConnect(IPAddress serverIP, string dimName)
        {
            BackgroundWorker bg = new BackgroundWorker();
            bg.DoWork += (x, y) => { DoConnectClient(serverIP, dimName); };
            bg.RunWorkerAsync();
            Thread.Sleep(0);
        }

        // Клиент отправляет серверу от какой вкладки он отключился
        public void DisconectClient(string dimName, string updId)
        {
            try
            {
                string message = string.Format("Disconnect|{0}|{1}", dimName, updId);
                byte[] msg = Encoding.UTF8.GetBytes(message);
                // Клиент сообщает серверу от какого справочника он отключился
                _clientSocket.Send(msg);
            }
            catch (Exception e)
            {
                Tracer.Error(e);
                Trace.WriteLine(e);
            }
        }

        // Если клиент закрывает справочник без изменений или обновляет его
        public void CloseNodeWithoutSave(string dimName, string updId)
        {
            try
            {
                string message = string.Format("CloseDim|{0}|{1}|{2}", dimName, updId, _username);
                byte[] msg = Encoding.UTF8.GetBytes(message);
                // Отправляем данные через сокет 
                _clientSocket.Send(msg);
            }

            catch (Exception e)
            {
                Tracer.Error(e);
                Trace.WriteLine(e);
            }
        }

        public void SaveUpdateNode(string dimName, string updId)
        {
            try
            {
                string message = string.Format("Save|{0}|{1}|{2}", dimName, updId, _username);
                byte[] msg = Encoding.UTF8.GetBytes(message);
                // Отправляем данные через сокет 
                _clientSocket.Send(msg);
            }
            catch (Exception e)
            {
                Tracer.Error(e);
                Trace.WriteLine(e);
            }
        }

        public void ClearChanges(string dimName, int id)
        {
            try
            {
                string message = string.Format("Clear|{0}|{1}|{2}", dimName, id, _username);
                byte[] msg = Encoding.UTF8.GetBytes(message);
                _clientSocket.Send(msg);
            }
            catch (Exception e)
            {
                Tracer.Error(e);
                Trace.WriteLine(e);
            }
        }


        //Раз в 2 минуты проверяем наличие клиентов и если их нет сервер перестает слушать
        public void CheckStopListening(Socket sListener)
        {
            BackgroundWorker bg = new BackgroundWorker();
            bg.DoWork += (x, y) =>
            {
                while (true)
                {
                    Thread.Sleep(120000);
                    _isCheck = true;
                    lock (_locker)
                        if (_clientSocketDict.Count == 0 && _exportClientDims.Count == 0)
                        {
                            _isListening = false;
                            _isCheck = false;
                            _updatedDimsDict.Clear();
                            //sListener.Shutdown(SocketShutdown.Both);
                            sListener.Close();
                            //sListener.Disconnect(true);
                            break;
                        }
                }
            };
            bg.RunWorkerAsync();
        }

        // Обновление серверного словаря
        private void UpdateDimsDict(string dimName, string updId, string operation)
        {
            var ids = updId.Split(new[] {"/"}, StringSplitOptions.RemoveEmptyEntries).Select(id => Convert.ToInt32(id))
                .ToList();
            lock (_locker)
            {
                if (!_updatedDimsDict.ContainsKey(dimName))
                {
                    _updatedDimsDict[dimName] = new List<int>();
                }
            }

            if (operation == "Send")
            {
                foreach (var id in ids)
                {
                    lock (_locker)
                    {
                        if (!_updatedDimsDict[dimName].Contains(id))
                        {
                            _updatedDimsDict[dimName].Add(id);
                        }
                    }
                }
            }

            else if (operation == "Clear" || operation == "CloseDim" || operation == "Disconnect" ||
                     operation == "Save")
            {
                foreach (var id in ids)
                {
                    lock (_locker)
                    {
                        if (_updatedDimsDict[dimName].Contains(id))
                        {
                            _updatedDimsDict[dimName].Remove(id);
                        }
                    }
                }
            }
        }

        private void SendUpdateDims(string dimName, Socket clientSocket, string username)
        {
            lock (_locker)
            {
                if (_updatedDimsDict.ContainsKey(dimName))
                {
                    string ids = string.Join("/", _updatedDimsDict[dimName]);
                    string message = $"{dimName}|{ids}|Reload|{username}";
                    clientSocket.Send(Encoding.UTF8.GetBytes(message));
                }
            }
        }

        private void AddConnectSocket(Socket clientSocket, string dimName)
        {
            lock (_locker)
            {
                if (!_clientSocketDict.ContainsKey(dimName))
                {
                    _clientSocketDict[dimName] = new List<Socket>();
                }

                if (!_clientSocketDict[dimName].Contains(clientSocket))
                {
                    _clientSocketDict[dimName].Add(clientSocket);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tid">ид</param>
        /// <param name="info">сообщение</param>
        /// <param name="isFileComplite">null - промежуточное сообщение, false - ошибка формирования файла</param>
        public delegate void ExportInfoUpdate(string tid, string info, bool? isFileComplite);

        public event ExportInfoUpdate InfoUpdate;

        public void ExportClientConnect(IPAddress serverIP, string TID)
        {
            BackgroundWorker bg = new BackgroundWorker();
            bg.DoWork += (x, y) =>
            {
                try
                {                  
                    if (!_clientSocket.Connected)
                        _clientSocket.Connect(serverIP, _port);

                    string message = $"Export|{TID}|";
                    byte[] msg = Encoding.UTF8.GetBytes(message);
                    // Отправляем данные через сокет 
                    _clientSocket.Send(msg);
                    while (true)
                    {
                        byte[] bytes = new byte[1024];
                        int bytesRec;
                        lock (_locker)
                        {
                            bytesRec = _clientSocket.Receive(bytes);
                        }

                        if (bytesRec == 0)
                            return;

                        string answer = Encoding.UTF8.GetString(bytes, 0, bytesRec);
                        var dt = answer.Split(new[] {"|"}, StringSplitOptions.None);
                        if (dt.Length == 3)
                        {
                            
                            bool? isFileComplite = null;
                            if (bool.TryParse(dt[2], out var checkFile))
                                isFileComplite = checkFile;

                            lock (_locker)
                            {
                                InfoUpdate?.Invoke(dt[0], dt[1], isFileComplite);
                                Thread.Sleep(50);
                            }
                                
                        }
                    }
                }
                catch (Exception e)
                {
                    Tracer.Error(e);
                    Trace.WriteLine(e);
                }
            };

            bg.RunWorkerAsync();
            Thread.Sleep(0);
        }


        public void SendExportInfo(string tid, string info, bool? isFileComplite)
        {
            lock (_locker)
            {
                if (!_exportClientDims.ContainsKey(tid))
                return;

                var msg = Encoding.UTF8.GetBytes($"{tid}|{info}|{isFileComplite}");
            
                _exportClientDims[tid].Send(msg);

                if (!isFileComplite.HasValue)
                    return;

                //файл экспорта сформирован, удаляем сокет
                _exportClientDims.Remove(tid);
            }
        }
    }
}