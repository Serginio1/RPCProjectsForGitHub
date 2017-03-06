using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Reflection;
using System.Threading.Tasks;
namespace TestDllForCoreClr
{

    public class EventTest
    {
        public event Action<string, int> EventWithTwoParameter;

        public event Action<string> EventWithOneParameter;
        public event Action EventWithOutParameter;
        public bool IsRun = false;
        public void Test()
        {
            EventWithTwoParameter?.Invoke(DateTime.Now.ToString(), 1);
            EventWithOneParameter?.Invoke(DateTime.UtcNow.ToString());
            EventWithOutParameter?.Invoke();
        }

        public async void Run()
        {
            if (IsRun) return;
            IsRun = true;

            while (IsRun)
            {

                await Task.Delay(2000);
                Test();

            }

        }
    }

    public class Тестовый
    {
        public static string Поле = "Статическое поле";
        public string СвойствоОбъекта { get; set; }

       
        public  Тестовый(string СвойствоОбъекта)
            {
            this.СвойствоОбъекта = СвойствоОбъекта;
            }

        public string ПолучитьСтроку()
        {

            return "Привет из CoreClr";
      
           
        }

        public int ПолучитьЧисло(int число)
        {

            return число;
        }

        public async Task<string> GetStringAsync()
        {

            await Task.Delay(2000);
            return "Закончили через 2 секунды";

        }

        public object ПолучитьМассивТипов()
        {
            var list = new List<object>();
            list.Add("System.String");
            list.Add(DateTime.Now);
            list.Add(true);
            list.Add((System.Byte)45);
            list.Add((System.Decimal)48.789);
            list.Add((System.Double)51.51);
            list.Add((System.Single)11.11);
            list.Add((System.Int32)11);
            list.Add((System.Int64)789988778899);
            list.Add((System.SByte)45);
            list.Add((System.Int16)66);
            list.Add((System.UInt32)77);
            list.Add((System.UInt64)88888888888888);
            list.Add((System.UInt16)102);
            list.Add(ПолучитьДвоичныеДанные());

            return list;
        }

        public byte[] ПолучитьДвоичныеДанные()
        {

            return UnicodeEncoding.Unicode.GetBytes("Строка в Двоичные Данные");
        }

        public object ПолучитьExpandoObject()
        {

            dynamic res = new ExpandoObject();
            res.Имя = "Тест ExpandoObject";
            res.Число = 456;
            res.ВСтроку = (Func<string>)(() => res.Имя);
            res.Сумма = (Func<int, int, int>)((x, y) => x + y);

            return res;
        }

        public T ДженерикМетод<V, T>(V param1, T param2,V param3)
        {

            return param2;
        }

        public V ДженерикМетод2<K,V>(Dictionary<K,V> param1, K param2, V param3)
        {

            return param3;
        }

        public async Task<V> ДженерикМетодAsync<K, V>(K param)
        {
                await Task.Delay(500);
                if (typeof(String) == typeof(V))
                    return (V) ((object)param.ToString());

                return default(V);
            
        }

        public IEnumerable<int> GetNumbers(int max)
        {
            for (int i = 0; i < max; i++)
            {
                  yield return 1;
               
            }
            yield break;
        }
        public void TestGenericParam(StringBuilder sb)
        {

            var param = typeof(Тестовый).GetMethod("ДженерикМетод2").GetParameters()[0];

            sb.AppendFormat("Dictionary<int,string> is {0}", typeof(Dictionary<int, string>).IsGenericTypeOf(param.ParameterType)).AppendLine();

            List<List<int>> res = null;
            var rs = typeof(Тестовый).GetMethod("ДженерикМетод2").НайтиПараметрыДляВывода(out res);
            sb.AppendFormat("Можно вывести метод из параметров {0}", rs).AppendLine();
            if (rs)
            {
                int i = 0;
                foreach (var arg in res)
                {
                    sb.AppendFormat("Аргумент {0} параметы ", i);
                  foreach (var par in arg)
                    {
                        sb.AppendFormat(" {0} ", par);
           

                    }
                    i++;
                    sb.AppendLine();
                }
                
            }
        }
        public string ПолучитьИнформациюОЖденерикМетоде()
        {

            //   var mi = typeof(Тестовый).GetMethod("ДженерикМетод");
               var mi = typeof(Тестовый).GetMethod("ДженерикМетод2");
            var res = new StringBuilder();
            if (mi.IsGenericMethod)
            {
                Type[] typeArguments = mi.GetGenericArguments();

                res.AppendFormat("\tList type arguments ({0}):",
                    typeArguments.Length).AppendLine();

                foreach (Type tParam in typeArguments)
                {
                    // IsGenericParameter is true only for generic type
                    // parameters.
                    //
                    if (tParam.IsGenericParameter)
                    {
                        res.AppendFormat("\t\t{0}  parameter position {1}" +
                            "\n\t\t   declaring method: {2}",
                            tParam,
                            tParam.GenericParameterPosition,
                            tParam.IsAssignableFrom(typeof(int))).AppendLine();

                        res.AppendFormat("IsAssignableFrom {0}", typeof(int).IsAssignableFrom(tParam.DeclaringType)).AppendLine();
                    }
                    else
                    {
                        res.AppendFormat("\t\t{0}", tParam).AppendLine();
                    }
                }
            }

            var m = mi.MakeGenericMethod(typeof(int), typeof(string));
            res.AppendLine(m.Invoke(this,new object[] { new Dictionary<int,string>(),4, "Вызов дженерик метода" }).ToString());
            TestGenericParam(res);
            return res.ToString();
        }
    }
}
