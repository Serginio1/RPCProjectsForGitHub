using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reflection;
class Program
{

    static dynamic wrap;
    static async Task<object> GetAsyncResult(dynamic wrap)
    {

        object resTask = await wrap.async.GetStringAsync();


        return resTask;
    }

    static async Task<object> GetGenericAsyncResult(dynamic TO)
    {
        //         public async Task<V> GenericMethodAsync<K, V>(K param, string param4 = "Test")
        object resTask = await TO.async.GenericMethodAsync(new object[] { "System.Int32", "System.String" }, 44);
        return resTask;
    }

    //static public void EventWithTwoParameter(dynamic value)
    //{
    //    Console.WriteLine("EventWithTwoParameter" + wrap.toString(value));


    //    value(ClientRPC.AutoWrapClient.FlagDeleteObject);
    //}

    //  параметр value:Анонимный Тип
    // Свойства параметра
    // arg1:System.String
    // arg2:System.Int32

   static public void EventWithTwoParameter(dynamic value)
    {
        Console.WriteLine("EventWithTwoParameter " + wrap.toString(value));
        Console.WriteLine($"EventWithTwoParameter arg1:{value.arg1} arg2:{value.arg2}");
        value(ClientRPC.AutoWrapClient.FlagDeleteObject);
    }


    // параметр value:System.String

    static public void EventWithOneParameter(dynamic value)
    {
        Console.WriteLine("EventWithOneParameter " + wrap.toString(value));
    }


    static public void EventWithOutParameter(dynamic value)
    {
        Console.WriteLine("EventWithOutParameter" + wrap.toString(value));
    }
    static string GetParentDir(string dir, int levelUp)
    {
        int start = dir.Length - 1; ;
        int pos = dir.LastIndexOf(Path.DirectorySeparatorChar, start);

        while (pos > 0 && levelUp > 0)
        {
            start = pos - 1;
            pos = dir.LastIndexOf(Path.DirectorySeparatorChar, start);
            levelUp--;

        }
        if (pos > 0)
            return dir.Substring(0, pos);

        return dir;
    }

    static object[] pp(params object[] Params)
    {
        return Params;
    }


   static void WriteTime(Stopwatch stopWatch)
    {
        var ts = stopWatch.Elapsed;

        var elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds,
        ts.Milliseconds / 10, 0);
        Console.WriteLine(elapsedTime);
    }
    static void RunTestEvent(ClientRPC.TCPClientConnector connector)
    {

        


        var @EventTest = wrap.GetType("TestDllForCoreClr.EventTest", "TestDll");
        // Получим описание методов
        var @DescribeEventMethods = wrap.GetType("NetObjectToNative.DescribeEventMethods", "Server");
        string CodeModule = @DescribeEventMethods.GetCodeModuleForEvents(@EventTest);

        // Получим код метода обертки
        var WrapperModuleCreater = wrap.GetType("NetObjectToNative.WrapperModuleCreater", "Server");
        var CodeModuleWrapEvent = WrapperModuleCreater.GetCodeModuleEventWrapper(@EventTest);
        // var test = wrap.New(EventTest);
        var test = EventTest._new();
        


        Console.WriteLine("Динамическая компиляция класса обертки");
        var stopWatch = new System.Diagnostics.Stopwatch();
        stopWatch.Start();

        // Создадим обертку для событий
         var wrapForEvents = connector.CreateWrapperForEvents(test);
        stopWatch.Stop();
        WriteTime(stopWatch);


        // Подпишемся на события передав имя события и метода типа Action<dynamic>
        wrapForEvents.AddEventHandler("EventWithTwoParameter", new Action<dynamic>(EventWithTwoParameter));
        wrapForEvents.AddEventHandler("EventWithOneParameter", new Action<dynamic>(EventWithOneParameter));
        wrapForEvents.AddEventHandler("EventWithOutParameter", new Action<dynamic>(EventWithOutParameter));
        // Запустим генерацию событий
        test.Run();
    }


    static void TestGenericMethods(ClientRPC.TCPClientConnector connector)
    {
        Console.WriteLine("Тест вызова дженерик методов");

        Dictionary<int, string> ClientDict = new Dictionary<int, string>()
        {
            [1] = "Один",
            [2] = "Второй2",
            [3] = "Один3"
        };

        var @Dictionary2 = wrap.GetType("System.Collections.Generic.Dictionary`2", "System.Collections");
        var @DictionaryIntString = wrap.GetGenericType(@Dictionary2, "System.Int32", "System.String");

        // var dict = connector.CoryTo(@DictionaryIntString, ClientDict);

        // var dict = connector.CoryTo("System.Collections.Generic.Dictionary`2[System.Int32, System.String]", ClientDict);
        var dict = connector.CoryTo(ClientDict);

        Console.WriteLine(wrap.toString(dict));
        Console.WriteLine(dict[2]);

        // Получим ссылку на сборку
        //вызывается метод на сервере
        //public static Assembly GetAssembly(string FileName, bool IsGlabalAssembly = false)
        //Если IsGlabalAssembly == true? то ищется сборка в каталоге typeof(string).GetTypeInfo().Assembly.Location
        //Иначе в каталоге приложения Server
        var assembly = wrap.GetAssembly("TestDll");
        // Получим из неё нужный тип
        var @TestClass = assembly.GetType("TestDllForCoreClr.TestClass");

        // Можно получить тип и так зная имя класса и имя сборки. Удобно когда нужен только один тип
        //Метод на сервере 
        //public static Type GetType(string type, string FileName = "", bool IsGlabalAssembly = false)
        //var @TestClass = wrap.GetType("TestDllForCoreClr.TestClass", "TestDll");


        var TO = @TestClass._new("Property  from Constructor");




        // Вызовем дженерик метод с автовыводом типа
        // public V GenericMethod<K, V>(Dictionary<K, V> param1, K param2, V param3)
        var resGM = TO.GenericMethod(dict, 99, "Hello");
        Console.WriteLine("Вызов дженерик метода с выводом типа " + resGM);




        // Test Generic Method с параметрами по умолчанию 
        //public string GenericMethodWithDefaulParam<K, V>(Dictionary<K, V> param1, K param2, int param3 = 4, string param4 = "Test")
        var @Int32 = wrap.GetType("System.Int32");
        resGM = TO.GenericMethodWithDefaulParam(dict, 99);
        Console.WriteLine("Вызов дженерик метода с автовыводом  типов " + resGM);

        resGM = TO.GenericMethodWithDefaulParam(pp(@Int32, "System.String"), dict, 99);
        Console.WriteLine("Вызов дженерик метода с аргументами типов " + resGM);



        // Test Generic Method с параметрами массивом
        // public string GenericMethodWithParams<K, V>(Dictionary<K, V> param1, K param2, params string[] args)

        resGM = TO.GenericMethodWithParams(dict, 99, "First", "Second");
        Console.WriteLine("Вызов дженерик метода с автовыводом  типов " + resGM);

        resGM = TO.GenericMethodWithParams(pp(@Int32, "System.String"), dict, 99, "First", "Second");
        Console.WriteLine("Вызов дженерик метода с аргументами типов " + resGM);


        // public V  GenericMethodWithRefParam<К,V >(К param, V param2, ref string param3)

        // Не получилось у меня использовать ref параметр. Говрит, что пшлатформа не поддерживает
        var OutParam = new ClientRPC.RefParam("TroLoLo");
        resGM = TO.GenericMethodWithRefParam(5, "GenericMethodWithRefParam", OutParam);
        Console.WriteLine($@"Вызов дженерик метода с автовыводом  типов Ref {resGM}  {OutParam.Value}");
        var GenericArgs = new object[] { "System.String", "System.String" };


        resGM = TO.GenericMethodWithRefParam(GenericArgs, null, "GenericMethodWithRefParam", OutParam);
        Console.WriteLine($@"Вызов дженерик метода с дженерик аргументами Ref {resGM}  {OutParam.Value}");

        // Test return null
        resGM = TO.GenericMethodWithRefParam(GenericArgs, null, null, OutParam);
        Console.WriteLine($@"Вызов дженерик метода с дженерик аргументами Ref {resGM}  {OutParam}");

        // Test асинхронного вызова дженерик метода

          resGM = GetGenericAsyncResult(TO).Result;
          Console.WriteLine("Вызов дженерик метода с аргументами типов async " + resGM);
    }

    static void TestSerializeObject(ClientRPC.TCPClientConnector connector)
    {
        // Создадим объект на стороне клиента
        var obj = new TestDllForCoreClr.TestClass("Объект на стороне Клиента");

        dynamic test = null;
        try
        {
            test = connector.CoryTo(obj);

        }

        // Сборка не загружена
        //Поэтому явно загрузим сборку и повторим операцию CoryTo
        catch (Exception)
        {

            Console.WriteLine("Ошибка " + connector.LastError);
            var assembly = wrap.GetAssembly("TestDll");
            test = connector.CoryTo(obj);

        }
        Console.WriteLine(test.ObjectProperty);

    }

    static Assembly LoadAssembly(string fileName)
    {
        var Dir = AppContext.BaseDirectory;
        string path = Path.Combine(Dir, fileName);
        Assembly assembly = null;
        if (File.Exists(path))
        {

            try
            {
                var asm = System.Runtime.Loader.AssemblyLoadContext.GetAssemblyName(path);
                assembly = Assembly.Load(asm);
            }
            catch (Exception)
            {
                assembly = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
            }

        }
        else
            throw new Exception("Не найдена сборка " + path);

        return assembly;

    }
    static void Main(string[] args)
    {
        var dir = AppContext.BaseDirectory;
        //var logPath = System.IO.Path.
        Console.WriteLine(dir);
        Console.WriteLine(GetParentDir(dir, 4));
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        //https://msdn.microsoft.com/en-us/library/system.diagnostics.processstartinfo(v=vs.110).aspx
        //Process.Start
        //  int port = 6892;

        int port = 6891;
        Console.WriteLine("Hello Client!");
        ProcessStartInfo startInfo = new ProcessStartInfo("dotnet.exe");
       //  startInfo.Arguments = $@"d:\Vs2017\RPC\Server\Server\bin\Release\netcoreapp1.1\Server.dll {port}";
        startInfo.Arguments = GetParentDir(dir, 4) + $@"\Server\Server\bin\Release\netcoreapp1.1\Server.dll {port}";
        //ProcessStartInfo startInfo = new ProcessStartInfo
        //{
        //    FileName = "dotnet",
        //    Arguments = "Server.dll -- 6891 4851",
        //    UseShellExecute = false,
        //    CreateNoWindow = true,
        //    RedirectStandardError = true,
        //    RedirectStandardOutput = true,
        //    WorkingDirectory = @"d:\Vs2017\RPC\Server\Server\bin\Release\netcoreapp1.1\"
        //};


        //   var server=  Process.Start(startInfo);

        bool LoadLocalServer = true;
        ClientRPC.TCPClientConnector connector;




        if (LoadLocalServer)
        {
            connector = ClientRPC.TCPClientConnector.LoadAndConnectToLocalServer(GetParentDir(dir, 4) + $@"\Server\Server\bin\Release\netcoreapp1.1\Server.dll");
          //  connector = ClientRPC.TCPClientConnector.LoadAndConnectToLocalServer(GetParentDir(dir, 4) + $@"\Server\Server\bin\Debug\netcoreapp1.1\Server.dll");
        }
        else
        {
            //3 параметр отвечает за признак  постоянного соединения с сервером
            //Используется пул из 5 соединений
            connector = new ClientRPC.TCPClientConnector("127.0.0.1", port, false);
            port = ClientRPC.TCPClientConnector.GetAvailablePort(6892);
            connector.Open(port, 2);

        }

       

        wrap = ClientRPC.AutoWrapClient.GetProxy(connector);

        // Выведем сообщение в консоли сервера
        string typeStr = typeof(Console).AssemblyQualifiedName;
        var _Console = wrap.GetType(typeStr);// Получим тип на сервере по имени
                                             // "Hello from Client" будет выведено в консоле сервера
        _Console.WriteLine("Hello from Client");

        ClientRPC.Test.TestAsInterfase(wrap, connector);
        // Запустим тест событий с сервера
         RunTestEvent(connector);

        // Тест на пергрузку операторов
        ClientRPC.Test.TestCallOperators(wrap);

        // Загрузим сбору из директории приложения
        LoadAssembly("TestDll.dll");

        // Тест на сериализацию объекта на сервер
        TestSerializeObject(connector);

        // Тест вызова дженерик методов
        TestGenericMethods(connector);



        var Dictionary2 = wrap.GetType("System.Collections.Generic.Dictionary`2", "System.Collections");
        var DictionaryIntString = wrap.GetGenericType(Dictionary2, "System.Int32", "System.String");
        //var dict = wrap.New(DictionaryIS);

        string[] sa = new string[] { "Нулевой", "Первый", "Второй", "Третий", "Четвертый" };
        var ServerSA = connector.ArrayCoryTo("System.String", sa);
        Console.WriteLine("ServerSA[2]  " + ServerSA[2]);
        ServerSA[2] = "Изменен Параметр по индексу 2";
        Console.WriteLine("ServerSA[2]  " + ServerSA[2]);


        Dictionary<int, string> ClientDict = new Dictionary<int, string>()
        {
            [1] = "Один",
            [2] = "Второй2",
            [3] = "Один3"
        };

        foreach (string value in ServerSA)
            Console.WriteLine("Values  " + value);


        Console.WriteLine("new  " + DictionaryIntString._new());

        var dict = connector.CoryTo(DictionaryIntString, ClientDict);
        Console.WriteLine("dict[2]  " + dict[2]);
        dict[2] = "Два";
        Console.WriteLine("dict[2]  " + dict[2]);

        var OutParam = new ClientRPC.RefParam();
        if (dict.TryGetValue(2, OutParam))
        {

            Console.WriteLine("OutParam  " + OutParam.Value);
        }

        var objFromServ = connector.CoryFrom<Dictionary<int, string>>(dict);
        Console.WriteLine("dict[2]  " + objFromServ[2]);

        foreach (string value in dict.Values)
            Console.WriteLine("Dict Values  " + value);

        var obj = new { First = "Первый", Second = 1 };

        var objstr = JsonConvert.SerializeObject(obj);

        dynamic jsObj = JsonConvert.DeserializeObject<dynamic>(objstr);

        Console.WriteLine("jsObj.First " + jsObj.First);
        Console.WriteLine(dict[2]);
        int res = wrap.ReturnParam(3);
        Console.WriteLine(res);


        var g = Guid.NewGuid();
        Console.WriteLine(g);
        Console.WriteLine(wrap.ReturnParam(g));

        Console.WriteLine(wrap.async);
        string str = wrap.ReturnParam("Привет");
        Console.WriteLine(str);
        Console.WriteLine(wrap.ReturnParam(3.14));
        Console.WriteLine(wrap.ReturnParam(DateTime.Now));

        var chr = wrap.ReturnParam('Я');
        Console.WriteLine(chr.GetType() + " " + chr);
        decimal dc = 678.89M;
        var Serverdc = wrap.ReturnParam(dc);
        Console.WriteLine(Serverdc);

        var ba = new byte[10];
        for (int i = 0; i < ba.Length; i++)
        {
            ba[i] = (byte)i;

        }

        var baServer = JsonConvert.DeserializeObject<dynamic>(objstr);
        for (int i = 0; i < ba.Length; i++)
        {
            Console.Write(i + ",");

        }

        int repeatCount = 1000;
        Console.WriteLine("");

        var stopWatch = new System.Diagnostics.Stopwatch();
        int count = 0;
        stopWatch.Start();
        for (int i = 0; i < repeatCount; i++)
        {
            count += wrap.ReturnParam(i);

        }

        stopWatch.Stop();
        var ts = stopWatch.Elapsed;
        var elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds,
   ts.Milliseconds / 10, 0);


        Console.WriteLine(count);
        Console.WriteLine(elapsedTime);

        var @TestClass = wrap.GetType("TestDllForCoreClr.TestClass", "TestDll");
        // var TO = wrap.New(Тестовый,"Свойство из Конструктора");

        var TO = @TestClass._new("Свойство из Конструктора");

        //  resGM = GetGenericAsyncResult(TO).Result;
        //  Console.WriteLine("Вызов дженерик метода с аргументами типов async " + resGM);

        Console.WriteLine("Свойство Объекта " + TO.ObjectProperty);
        TO.ObjectProperty = "Свойство Новое";
        Console.WriteLine("Свойство Объекта " + TO.ObjectProperty);

        var EO = TO.GetExpandoObject();
        Console.WriteLine("Свойство ExpandoObject Имя " + EO.Name);
        Console.WriteLine("Свойство ExpandoObject Число " + EO.Number);
        //  var asres = GetAsyncResult(TO).Result;
        //  Console.WriteLine("GetAsyncResult " + asres);

        try { 
        var Delegate = EO.toString;
        Console.WriteLine("Вызов делегата toString " + Delegate()); ;// Вызовем как делегат
                                                                    // Для ExpandoObject можно вызвать как метод
        Console.WriteLine("Вызов Метода toString " + EO.toString());// Для ExpandoObject

        var DelegateSum = EO.Sum;

        Console.WriteLine("Вызов делегата Sum " + DelegateSum(3,4)); ; // Вызовем как делегат
                                                                       // Для ExpandoObject можно вызвать как метод
        Console.WriteLine("Вызов Метода Sum " + EO.Sum(3,4));          // Для ExpandoObject

        }
        catch(Exception)
        {
            Console.WriteLine("ошибка  " + connector.LastError);// Получим ошибку

        }
        int rs = TO.GetNumber(89);

        ClientRPC.Test.RunTestSpeed(ClientRPC.Test.GetStaticMethod(wrap), "Вызов статического метода");
        ClientRPC.Test.RunTestSpeed(ClientRPC.Test.GetObjetMethod(TO), "Вызов метода объекта");

        ClientRPC.Test.RunTestSpeed(ClientRPC.Test.GetStaticMethod(wrap), "Вызов статического метода");
        ClientRPC.Test.RunTestSpeed(ClientRPC.Test.GetObjetMethod(TO), "Вызов метода объекта");


        @TestClass = null;
        TO = null;

        //  Вызовем финализаторы всех AutoWrapClient ссылок на серверные объекты
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Console.WriteLine("Press any key");
        Console.ReadKey();

        // Удаления из хранилища на стороне сервера происходит пачками по 50 элементов
        // Отрправим оставшиеся
        connector.ClearDeletedObject();

        // Отключимся от сервера, закроем все соединения, Tcp/Ip сервер на клиенте
        connector.Close();

        // Если мы запустили процесс сервера
        // То выгрузим его
        if (LoadLocalServer)  connector.CloseServer();
        Console.WriteLine("Press any key");
        Console.ReadKey();
        //    server.Kill();
    }
}