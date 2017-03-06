using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Linq;
using System.Collections.Concurrent;
namespace ClientRPC
{

    public class TCPClientConnector
    {

        TcpListener Server;
        internal Dictionary<Guid, TaskCompletionSource<object>> AsyncDictionary = new Dictionary<Guid, TaskCompletionSource<object>>();
        internal Dictionary<Guid, WrapperObjectWithEvents> EventDictionary = new Dictionary<Guid, WrapperObjectWithEvents>();
        internal BlockingCollection<NetworkStream> NSQueue = new BlockingCollection<NetworkStream>(5);
        // Будем записывать ошибки в файл
        // Нужно прописать в зависимости "System.Diagnostics.TextWriterTraceListener"
        // Файл будет рядом с этой DLL

        // Устанавливаем флаг при закрытии
        bool IsClosed = false;
        // Клиент для отпраки сообщений на сервер

        IPEndPoint IpEndpoint;

        internal int PortForCallBack;
        internal string LastError;

        bool KeepConnection = true;
        // Список для удаления объектов
        //Для уменьшения затрат на межпроцессное взаимодействие будем отправлть
        //Запрос на удаление из хранилища не по 1 объект а пачками количество указанным  в CountDeletedObjects
        internal List<int> DeletedObjects=new List<int>();

        // Нужен для синхронизации доступа к DeletedObjects
        object syncForDelete =new object();

        // Количество удаляемых объектов в пакете для отправке на сервер
        internal int CountDeletedObjects=50;

        public bool ServerIsClosed { get; private set; }
        public TCPClientConnector(string ServerAdress, int port, bool KeepConnection=true)
        {

            this.KeepConnection = KeepConnection;

            IpEndpoint = new IPEndPoint(IPAddress.Parse(ServerAdress), port);

            for(var i=0; i<5; i++)
            {
                var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    client.Connect(IpEndpoint);
                var ns = new NetworkStream(client);
                NSQueue.Add(ns);

            }
            //Open(portForCallBack, CountListener);
        }


        public static TCPClientConnector LoadAndConnectToLocalServer(string FileName)
        {
            int port = 1025;

            port = GetAvailablePort(port);
            ProcessStartInfo startInfo = new ProcessStartInfo("dotnet.exe");
            startInfo.Arguments = @""""+ FileName+ $@""" { port}";
            Console.WriteLine(startInfo.Arguments);
            var server = Process.Start(startInfo);
            Console.WriteLine(server.Id);

            var connector = new TCPClientConnector("127.0.0.1", port);

            port++;
            port = GetAvailablePort(port);
            connector.Open(port, 2);

            return connector;

        }
        // Отсылаем поток данных и считываем ответ
         BinaryReader SendMessageOne(MemoryStream stream)
        {

            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                client.Connect(IpEndpoint);
                //      client.NoDelay = true;

                using (var ns = new NetworkStream(client))
                {
                    ns.WriteByte(1);// Сервер отрабатывает только 1 запрос
                    stream.Position = 0;
                    ns.Write(BitConverter.GetBytes((Int32)stream.Length), 0, 4);
                    stream.CopyTo(ns);

                    using (var br = new BinaryReader(ns))
                    {
                        var streamSize = br.ReadInt32();

                        var res = br.ReadBytes(streamSize);

                        var ms = new MemoryStream(res);
                        ms.Position = 0;
                        return new BinaryReader(ms);
                    }

                }
            }
        }



         BinaryReader SendMessageKeepConnection(MemoryStream stream, NetworkStream ns)
        {

            stream.Position = 0;
            ns.WriteByte(0); // Признак того, что соединение не разрывать
            ns.Write(BitConverter.GetBytes((int)stream.Length), 0, 4);
            stream.CopyTo(ns);
            ns.Flush();

            var buffer = new byte[4];

            ns.Read(buffer, 0, 4);
            var streamSize = BitConverter.ToInt32(buffer, 0);
            var res = new byte[streamSize];
            ns.Read(res, 0, streamSize);
            var ms = new MemoryStream(res);
            ms.Position = 0;
            return new BinaryReader(ms);
        }


        internal BinaryReader SendMessage(MemoryStream stream)
        {

            if (KeepConnection)
            {
                var ns = NSQueue.Take();
                var res = SendMessageKeepConnection(stream, ns);
                NSQueue.Add(ns);
                return res;
            }

            return SendMessageOne(stream);
        }



        // Откроем порт и количество слушющих задач которое обычно равно подсоединенным устройствам
        // Нужно учитывть, что 1С обрабатывает все события последовательно ставя события в очередь
        public void Open(int Port = 6892, int CountListener = 1)
        {
            IsClosed = false;
            PortForCallBack = Port;
            IPEndPoint ipEndpoint = new IPEndPoint(IPAddress.Any, Port);
            Server = new TcpListener(ipEndpoint);
            Server.Start();

            // Создадим задачи для прослушивания порта
            //При подключении клиента запустим метод ОбработкаСоединения
            // Подсмотрено здесь https://github.com/imatitya/netcorersi/blob/master/src/NETCoreRemoveServices.Core/Hosting/TcpServerListener.cs
            for (int i = 0; i < CountListener; i++)
                Server.AcceptTcpClientAsync().ContinueWith(OnConnect);

        }


        // Метод для обработки сообщения от клиента
        private void OnConnect(Task<TcpClient> task)
        {

            if (task.IsFaulted || task.IsCanceled)
            {
                // Скорее всего вызвано  Server.Stop();
                return;
            }

            // Получим клиента
            TcpClient client = task.Result;

            // И вызовем метод для обработки данных
            // 
            ExecuteMethod(client);

            // Если Server не закрыт то запускаем нового слушателя
            if (!IsClosed)
                Server.AcceptTcpClientAsync().ContinueWith(OnConnect);

        }


        private void ExecuteMethod(TcpClient client)
        {
            //    client.Client.NoDelay = true;
            using (NetworkStream ns = client.GetStream())
            {

                // Получим данные с клиента и на основании этих данных
                //Создадим ДанныеДляКлиета1С котрый кроме данных содержит 
                //TcpClient для отправки ответа
                using (var br = new BinaryReader(ns))
                {
                    var streamSize = br.ReadInt32();

                    var res = br.ReadBytes(streamSize);

                    var ms = new MemoryStream(res);
                    ms.Position = 0;
                    RunMethod(ms);
                }



            }

        }


        private void RunMethod(MemoryStream ms)
        {
            using (BinaryReader br = new BinaryReader(ms))
            {
                var msRes = new MemoryStream();

                var cm = br.ReadByte();

                switch (cm)
                {
                    case 0: SetAsyncResult(br); break;
                    case 1: SetEvent(br); break;



                }


            }
        }


        public void SetAsyncResult(BinaryReader br)
        {


            Guid Key = new Guid(br.ReadBytes(16));
            var res = br.ReadBoolean();
            var resObj = WorkVariants.GetObject(br, this);
            TaskCompletionSource<object> value;
            if (AsyncDictionary.TryGetValue(Key, out value))
            {
                if (res)
                    value.SetResult(resObj);
                else
                    value.TrySetException(new Exception((string)resObj));

            }




        }

        public void SetEvent(BinaryReader br)
        {


            Guid Key = new Guid(br.ReadBytes(16));
            var res = WorkVariants.GetObject(br, this);
            WrapperObjectWithEvents value;
            if (EventDictionary.TryGetValue(Key, out value))
                value.RaiseEvent(Key, res);




        }

        void CloseConnection()
        {
            if (KeepConnection)
            {

                KeepConnection = false;
                NetworkStream ns;
              while (NSQueue.TryTake(out ns))
                {
                    ns.WriteByte(1); // Признак того, что соединение  разрывать
                    ns.Write(BitConverter.GetBytes((int)0), 0, 4);
                    ns.Flush();
                    ns.Dispose();
                }

            }

        }
        // Закроем ресурсы
        public void Close()
        {
            if (Server != null)
            {
                CloseEvents();
                IsClosed = true;
                Server.Stop();
                Server = null;


            }

            CloseConnection();
        }


        // Создадим обертку для использования событий
        public WrapperObjectWithEvents CreateWrapperForEvents(object obj)
        {

            if (typeof(AutoWrapClient) != obj.GetType())
                throw new Exception("Объект должен быть Net объектом");

            int Target = ((AutoWrapClient)obj).Target;

            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            bw.Write((byte)CallMethod.GetWrapperForObjectWithEvents);
            bw.Write(Target);
            bw.Write(PortForCallBack);
            bw.Flush();

            var res = SendMessage(ms);

            object result = null;
            if (!AutoWrapClient.GetResult(res, ref result, this))
                throw new Exception(LastError);

            return new WrapperObjectWithEvents(result, this);

        }

        
        // Отсылаем массив ссылок для удаления их из зранилища объектов на сервере
        void SendDeleteObjects()
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            bw.Write((byte)CallMethod.DeleteObjects);
            bw.Write(DeletedObjects.Count);

            foreach (var i in DeletedObjects)
                bw.Write(i);

            bw.Flush();

            DeletedObjects.Clear();
            var res = SendMessage(ms);

            object result = null;
            if (!AutoWrapClient.GetResult(res, ref result, this))
            {
                throw new Exception(LastError);
            }

        }


        // Отошлем все ссылки для удаления объектов из  хранилища  
    
        public void ClearDeletedObject()
        {
            if (ServerIsClosed) return;

            lock (syncForDelete)
            {
             
                if (DeletedObjects.Count > 0)
                {

                    SendDeleteObjects();
                }

            }

        }

        
        void CloseEvents()
        {

            foreach (var eventWrap in EventDictionary.Values.Distinct().ToArray())
                eventWrap.Close();
        }

        void SendCloseServer()
        {
           
                //      client.NoDelay = true;
                var ms = new MemoryStream();
                var bw = new BinaryWriter(ms);

                bw.Write((byte)CallMethod.CloseServer);
                bw.Flush();
  
              SendMessage(ms);

        }
        public void CloseServer()
        {

            CloseEvents();

            ServerIsClosed = true;
            SendCloseServer();

        }
        //Добавим ссылку на объект на сервере в буффер
        // И если в буфере количество больше заданного
        // То отсылается массив ссылок, а буфуе очищается
        // Сделано, для ускорения действи при межпроцессном взаимодействии
        public void DeleteObject(AutoWrapClient Object)
        {
            if (ServerIsClosed) return;

            lock (syncForDelete)
            {
                DeletedObjects.Add(Object.Target);
                if (DeletedObjects.Count > CountDeletedObjects)
                {

                    SendDeleteObjects();
                }

            }

        }

        // Сериализация десериализация объектов

        // Сериализуем объект на сервере, возвратим строку
        // Десериализуем объект из строки по переданному Generic параметру
        public T CoryFrom<T>(AutoWrapClient value)
        {
            object result;
            var res = AutoWrapClient.TryInvokeMember(0, "ObjectToJson", new object[] { value }, out result, this);

            if (!res)
                throw new Exception(LastError);

            string str = (string)result;

            return JsonConvert.DeserializeObject<T>(str);


        }


        // type может быть ссылкой на Type AutoWrapClient на стороне сервера
        // Или строковым представлением типа
        public dynamic CoryTo(object type, string objToStr)
        {
            object result;
            var res = AutoWrapClient.TryInvokeMember(0, "JsonToObject", new object[] { type, objToStr }, out result, this);

            if (!res)
                throw new Exception(LastError);

            return result;


        }

        public dynamic CoryTo(object type, object obj)
        {
            var str = JsonConvert.SerializeObject(obj);
            return CoryTo(type, str);

        }


        // Сериализуем объект и отправим представление типа ввиде AssemblyQualifiedName
        public dynamic CoryTo(object obj)
        {
            string type = obj.GetType().AssemblyQualifiedName;
            var str = JsonConvert.SerializeObject(obj);
            return CoryTo(type, str);

        }
        public dynamic ArrayCoryTo(object ElementType, string objToStr, int rank = 0)
        {
            object result;
            var res = AutoWrapClient.TryInvokeMember(0, "JsonToArray", new object[] { ElementType, objToStr }, out result, this);

            if (!res)
                throw new Exception(LastError);

            return result;


        }

        public dynamic ArrayCoryTo(object ElementType, object obj, int rank = 0)
        {
            var str = JsonConvert.SerializeObject(obj);
            return ArrayCoryTo(ElementType, str, rank);

        }


        // Конец Сериализации десериализации

        public static int GetAvailablePort(int port)
        {

            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

            var set = new HashSet<int>();
            foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
            {
                set.Add(tcpi.LocalEndPoint.Port);
               
            }

            for(var i=port; i< 49152; i++)
            {
                if (!set.Contains(i))
                    return i;


            }

            return port;


        }


    }
}

