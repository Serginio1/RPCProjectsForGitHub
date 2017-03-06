using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Reflection;
using System.Threading.Tasks;

namespace TestDllForCoreClr
{
    public class TestClass
    {
        public static string StaticField = "Static field";
        public string ObjectProperty { get; set; }


        public TestClass(string СвойствоОбъекта)
        {
            this.ObjectProperty = СвойствоОбъекта;
        }

        public string GetString()
        {

            return "Hello from CoreClr";


        }

        public int GetNumber(int value)
        {

            return value;
        }

        public async Task<string> GetStringAsync()
        {

            await Task.Delay(2000);
            return "Закончили через 2 секунды";

        }

        public object GetObjectArray()
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
            list.Add(GetBinaryData());

            return list;
        }

        public byte[] GetBinaryData()
        {

            return UnicodeEncoding.Unicode.GetBytes("String to byte[]");
        }

        public object GetExpandoObject()
        {

            dynamic res = new ExpandoObject();
            res.Name = "Test ExpandoObject";
            res.Number = 456;
            res.toString = (Func<string>)(() => res.Name);
            res.Sum = (Func<int, int, int>)((x, y) => x + y);

            return res;
        }

        public T GenericMethod<V, T>(V param1, T param2, V param3)
        {

            return param2;
        }

        public V GenericMethod<K, V>(Dictionary<K, V> param1, K param2, V param3)
        {

            return param3;
        }

        public string GenericMethodWithDefaulParam<K, V>(Dictionary<K, V> param1, K param2, int param3=4, string param4="Test")
        {

            return $@"param3={param3} param4={param4} ";
        }

        public string GenericMethodWithParams<K, V>(Dictionary<K, V> param1, K param2,params string[] args)
        {
            var sb = new StringBuilder();
            sb.AppendLine(param1.ToString());
            sb.AppendLine(param2.ToString());

            for(int i=0; i< args.Length;i++)
                sb.AppendLine($@"param{i}={args[i]}");

            return sb.ToString();
        }

        public async Task<V> GenericMethodAsync<K, V>(K param, string param4 = "Test")
        {
            await Task.Delay(500);
            if (typeof(String) == typeof(V))
                return (V)((object)(param.ToString()+ param4));

            return default(V);

        }

        static string toString(object value)
        {
            if (value == null)
                return "null";

            return value.ToString();

        }
        public V  GenericMethodWithRefParam<К,V >(К param, V param2, ref string param3)
        {
            param3 = "Изменен в GenericMethodWithRefParam "+ toString(param);
            return param2;

        }

        public IEnumerable<int> GetNumbers(int max)
        {
            for (int i = 0; i < max; i++)
            {
                yield return 1;

            }
            yield break;
        }
      
      
    }
}
