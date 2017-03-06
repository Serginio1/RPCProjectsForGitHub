using System;
using System.Collections.Generic;
using System.Text;

namespace ClientRPC
{
    public class Test
    {


        // Test поддерки перегрузки операторов.
        //Взят отсюда https://professorweb.ru/my/csharp/charp_theory/level6/6_4.php
        public static void TestCallOperators(dynamic wrap)
        {
            // получим тип по строкому представлению TestDllForCoreClr.MyArr
            // Из сборки TestDll
            var _MyArr = wrap.GetType("TestDllForCoreClr.MyArr", "TestDll");

            // Создадим объекты на стороне сервера
            // и получим ссылки на них
            var Point1 = _MyArr._new(1, 12, -4);
            var Point2 = _MyArr._new(0, -3, 18);


            // Все операции происходят на стороне сервера
            Console.WriteLine("Координаты первой точки: " +
                Point1.x + " " + Point1.y + " " + Point1.z);
            Console.WriteLine("Координаты второй точки: " +
                Point2.x + " " + Point2.y + " " + Point2.z + "\n");

            var Point3 = Point1 + Point2;
            Console.WriteLine("\nPoint1 + Point2 = "
                + Point3.x + " " + Point3.y + " " + Point3.z);
            Point3 = Point1 - Point2;
            Console.WriteLine("Point1 - Point2 = "
                + Point3.x + " " + Point3.y + " " + Point3.z);
            Point3 = -Point1;
            Console.WriteLine("-Point1 = "
                + Point3.x + " " + Point3.y + " " + Point3.z);
            Point2++;
            Console.WriteLine("Point2++ = "
                + Point2.x + " " + Point2.y + " " + Point2.z);
            Point2--;
            Console.WriteLine("Point2-- = "
                + Point2.x + " " + Point2.y + " " + Point2.z);

            dynamic myObject1 = _MyArr._new(4,5,12);
            dynamic myObject2 = _MyArr._new(4,5,12);

            if (myObject1 == myObject2)
                Console.WriteLine("Объекты равны перегрузка оператора ==");

            if (myObject1.Equals(myObject2))
                Console.WriteLine("Объекты равны Equals");

            if (_MyArr.op_Equality(myObject1,myObject2))
                Console.WriteLine("Объекты равны op_Equality");


            Console.WriteLine("MyArr "+ myObject1.ToString());
            Console.WriteLine("MyArr GetHashCode " + myObject1.GetHashCode());
        }

        static int repeatCount = 100000;
        public static void  RunTestSpeed(Func<int> Method,string NameTest)
        {

           

                var stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();

                var res = Method();
                stopWatch.Stop();
                var ts = stopWatch.Elapsed;

                var elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10, 0);

                Console.WriteLine(NameTest);
                Console.WriteLine(res);
                Console.WriteLine(elapsedTime);
                double speed = repeatCount / ((double)stopWatch.ElapsedMilliseconds / 1000);
                speed = Math.Round(speed, 0);
                Console.WriteLine(speed + "  Вызовов в секунду");

        }

        public static Func<int> GetStaticMethod(dynamic wrap)
        {
            return () =>
            {

                int count = 0;
                for (int i = 0; i < repeatCount; i++)
                {
                    count += wrap.ReturnParam(i);

                }
                return count;

            };

        }


        public static Func<int> GetObjetMethod(dynamic TO)
        {
            return () =>
            {

                int count = 0;
                for (int i = 0; i < repeatCount; i++)
                {
                    count += TO.GetNumber(i);

                }
                return count;

            };

        }

        public static void TestAsInterfase(dynamic wrap, TCPClientConnector Connector)
        {

            Console.WriteLine("Тест приведения к интерфейсу");
            string[] sa = new string[] { "Нулевой", "Первый", "Второй", "Третий", "Четвертый" };
            var ServerSa = Connector.CoryTo(sa);
            var en = ServerSa._as("IEnumerable");
            var Enumerator = en.GetEnumerator();

            while(Enumerator.MoveNext())
                Console.WriteLine(Enumerator.Current);

            var @IEnumerable = wrap.GetType("System.Collections.IEnumerable");
            var @IEnumerator = wrap.GetType("System.Collections.IEnumerator");

            en = ServerSa._as(@IEnumerable);
            Enumerator = en.GetEnumerator();
            // На всякий случай приведем к Интерфейсу IEnumerator
            Enumerator = Enumerator._as(@IEnumerator);

            while (Enumerator.MoveNext())
                Console.WriteLine(Enumerator.Current);
        }

    }
}
