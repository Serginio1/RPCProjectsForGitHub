using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Dynamic;
using System.Threading.Tasks;
using System.Collections;

namespace ClientRPC
{

    public enum CallMethod : byte
    {
        CallFunc = 0,
        GetMember,
        SetMember,
        CallFuncAsync,
        CallDelegate,
        CallGenericFunc,
        GetWrapperForObjectWithEvents,
        SetIndex,
        GetIndex,
        CallBinaryOperation,
        CallUnaryOperation,
        IteratorNext,
        DeleteObjects,
        CloseServer

    }

    public class RefParam
    {
      public  dynamic Value;
       public RefParam(object Value)
        {

            this.Value = Value;
        }
       public RefParam()
        {

            this.Value = null;
        }

        public override string ToString()
        {

            return Value?.ToString();
        }

    }
    public class Enumerator : IEnumerator
    {
        AutoWrapClient enumerator=null;
        internal TCPClientConnector Connector;

        public object Current { get; set;}
        internal Enumerator(AutoWrapClient target, TCPClientConnector Connector)
        {

            this.Connector = Connector;
            object result = null;
          if (! AutoWrapClient.TryInvokeMember(0, "GetIterator", new object[] { target }, out result, Connector))
                throw new Exception(Connector.LastError);

            enumerator = (AutoWrapClient)result;

        }

        

        public bool MoveNext()
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            bw.Write((byte)CallMethod.IteratorNext);
            bw.Write(enumerator.Target);
            bw.Flush();

            var res = Connector.SendMessage(ms);
            var resCall = res.ReadBoolean();

            if (!resCall)
            {
                string Error = res.ReadString();
                throw new Exception(Error);

            }

            var resNext= res.ReadBoolean();

            if (!resNext)
            {
                GC.SuppressFinalize(enumerator);
                return false;

            }

              Current = WorkVariants.GetObject(res, Connector);
            return true;

        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }
    public class AutoWrapClient : DynamicObject, System.Collections.IEnumerable, IDisposable
    {

        public static readonly object FlagDeleteObject= new object();
        public int Target;
        internal TCPClientConnector Connector;

       


    public AutoWrapClient(int Target, TCPClientConnector Connector)
        {
            //  IPEndPoint ipEndpoint = new IPEndPoint(IPAddress.Parse(АдресСервера), порт);
            this.Target = Target;
            this.Connector = Connector;


        }
        // вызов метода


        public static dynamic GetProxy(TCPClientConnector Connector)
        {

            return new AutoWrapClient(0, Connector);

        }



        internal static bool GetResult(BinaryReader res, ref object result, TCPClientConnector Connector)
        {
            var resRun = res.ReadBoolean();
            var returnValue = WorkVariants.GetObject(res, Connector);
            if (!resRun)
            {
                if (returnValue != null && returnValue.GetType() == typeof(string))
                    Connector.LastError = (string)returnValue;


                return false;
            }

            result = returnValue;
            return true;
        }

        internal static bool GetResultWithChangeParams(BinaryReader res, ref object result, TCPClientConnector Connector, object[] args, int offset=0)
        {
            if (!GetResult(res, ref result, Connector))
                return false;

            int count = res.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                var index = res.ReadInt32();// Получим индекс измененного параметра
                object value = WorkVariants.GetObject(res, Connector);// Получим значение измененного параметра

               // args[index + offset]= value;// Установим нужный параметр, для Generic методов с 0 индексом идет тип аргументов

                // Вариант с  RefParam 
                  object param = args[index+ offset];
                  if (param != null && param.GetType() == typeof(RefParam))
                      ((RefParam)param).Value = value;


            }

            return true;
        }
        static internal void GetAsyncResult(BinaryReader res, TaskCompletionSource<object> result, TCPClientConnector Connector)
        {
            object Asyncres = null;
            if (!GetResult(res, ref Asyncres, Connector))
            {
                result.SetException(new Exception(Connector.LastError));

            }

        }

        internal static bool TryInvokeMember(int Target, string MethodName, object[] args, out object result, TCPClientConnector Connector)
        {

            result = null;

            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            bw.Write((byte)CallMethod.CallFunc);
            bw.Write(Target);
            bw.Write(MethodName);
            bw.Write(args.Length);

            foreach (var arg in args)
                WorkVariants.WriteObject(arg, bw);

            bw.Flush();

            var res = Connector.SendMessage(ms);
            // return GetResult(res, ref result, Connector);
            return GetResultWithChangeParams(res, ref result, Connector, args);
        }

        internal static bool TryInvokeGenericMethod(int Target, string MethodName, object[] args, out object result, TCPClientConnector Connector)
        {

            result = null;

            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            bw.Write((byte)CallMethod.CallGenericFunc);
            bw.Write(Target);
            bw.Write(MethodName);

            var arguments = (object[])args[0];
            bw.Write(arguments.Length);

            foreach (var arg in arguments)
                WorkVariants.WriteObject(arg, bw);

            bw.Write(args.Length - 1);

            for (var i = 1; i < args.Length; i++)
                WorkVariants.WriteObject(args[i], bw);



            bw.Flush();

            var res = Connector.SendMessage(ms);
            // return GetResult(res, ref result, Connector);
            return GetResultWithChangeParams(res, ref result, Connector, args,1);
        }
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
           
            result = null;
            if (Connector.ServerIsClosed) return false;

            string MethodName = binder.Name;
            if (MethodName == "_new")
            {
                object[] newArgs = new object[args.Length + 1];
                args.CopyTo(newArgs, 1);
                newArgs[0] = this;
                return TryInvokeMember(0, "New", newArgs, out result, Connector); ;

            }
            if (args.Length > 0 && args[0] != null && args[0].GetType() == typeof(object[]))
                return TryInvokeGenericMethod(Target, MethodName, args, out result, Connector);

            return TryInvokeMember(Target, MethodName, args, out result, Connector);


        }

        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            result = null;
            if (Connector.ServerIsClosed) return false;

            if (args.Length == 1 && object.ReferenceEquals(args[0], FlagDeleteObject))
            {
                Dispose(true);
                result = null;
                return true;

            }

            
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            bw.Write((byte)CallMethod.CallDelegate);
            bw.Write(Target);

            bw.Write(args.Length);
            foreach (var arg in args)
                WorkVariants.WriteObject(arg, bw);

            bw.Flush();

            var res = Connector.SendMessage(ms);
            return GetResult(res, ref result, Connector);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = null;
            if (Connector.ServerIsClosed) return false;

            var MemberName = binder.Name;

            if (MemberName == "async")
            {

                result = new AsyncAutoWrapClient(Target, Connector);
                return true;

            }
            
            // binder.Name);
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            bw.Write((byte)CallMethod.GetMember);
            bw.Write(Target);
            bw.Write(MemberName);
            var res = Connector.SendMessage(ms);

            return GetResult(res, ref result, Connector);

        }


        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (Connector.ServerIsClosed) return false;

            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            bw.Write((byte)CallMethod.SetMember);
            bw.Write(Target);
            bw.Write(binder.Name);
            WorkVariants.WriteObject(value, bw);

            var res = Connector.SendMessage(ms);
            object result = null;
            return GetResult(res, ref result, Connector);
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            if (Connector.ServerIsClosed) return false;

            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            bw.Write((byte)CallMethod.SetIndex);
            bw.Write(Target);
            bw.Write(indexes.Length);
            foreach (var arg in indexes)
                WorkVariants.WriteObject(arg, bw);

            WorkVariants.WriteObject(value, bw);

            bw.Flush();

            var res = Connector.SendMessage(ms);
            object result = null;
            return GetResult(res, ref result, Connector);
        }

        // Get the property value by index.
        public override bool TryGetIndex(
            GetIndexBinder binder, object[] indexes, out object result)
        {
            result = null;
            if (Connector.ServerIsClosed) return false;

            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            bw.Write((byte)CallMethod.GetIndex);
            bw.Write(Target);
            bw.Write(indexes.Length);
            foreach (var arg in indexes)
                WorkVariants.WriteObject(arg, bw);

            bw.Flush();
            
            var res = Connector.SendMessage(ms);
            return GetResult(res, ref result, Connector);



        }


        public override bool TryBinaryOperation(
       BinaryOperationBinder binder, object arg, out object result)
        {
            result = null;

            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            bw.Write((byte)CallMethod.CallBinaryOperation);
            bw.Write(Target);
            bw.Write((byte)binder.Operation);
            WorkVariants.WriteObject(arg, bw);
        
            bw.Flush();
            var res = Connector.SendMessage(ms);
            return GetResult(res, ref result, Connector);

        }

        public override bool TryUnaryOperation(
       UnaryOperationBinder binder, out object result)
        {
            result = null;

            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            bw.Write((byte)CallMethod.CallUnaryOperation);
            bw.Write(Target);
            bw.Write((byte)binder.Operation);
   
            bw.Flush();
            var res = Connector.SendMessage(ms);
            return GetResult(res, ref result, Connector);

        }

    IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this, Connector);
        }

        #region IDisposable Support
        private bool disposedValue = false; // Для определения избыточных вызовов

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: освободить управляемое состояние (управляемые объекты).
                    GC.SuppressFinalize(this);
                }

                Connector.DeleteObject(this);
                // TODO: освободить неуправляемые ресурсы (неуправляемые объекты) и переопределить ниже метод завершения.
                // TODO: задать большим полям значение NULL.

                disposedValue = true;
            }
        }

        // TODO: переопределить метод завершения, только если Dispose(bool disposing) выше включает код для освобождения неуправляемых ресурсов.
        // ~AutoWrapClient() {
        //   // Не изменяйте этот код. Разместите код очистки выше, в методе Dispose(bool disposing).
        //   Dispose(false);
        // }

        // Этот код добавлен для правильной реализации шаблона высвобождаемого класса.
        void IDisposable.Dispose()
        {
            // Не изменяйте этот код. Разместите код очистки выше, в методе Dispose(bool disposing).
            Dispose(true);
            // TODO: раскомментировать следующую строку, если метод завершения переопределен выше.
            // GC.SuppressFinalize(this);
        }
        #endregion

        ~AutoWrapClient()
        {
            Dispose(false);
        }

        public override bool Equals(object obj)
        {
            object[] args = new object[] { obj};
            object result;
    
            if (TryInvokeMember(Target, "Equals", args, out result, Connector))
                return (bool)result;

            throw new Exception(Connector.LastError);
        }

        public override string ToString()
        {
            object[] args = new object[0];
            object result;

            if (TryInvokeMember(Target, "ToString", args, out result, Connector))
                return (string)result;

            throw new Exception(Connector.LastError);
        }

        public override int GetHashCode()
        {
            object[] args = new object[0];
            object result;

            if (TryInvokeMember(Target, "GetHashCode", args, out result, Connector))
                return (int)result;

            throw new Exception(Connector.LastError);

        }

    }
    public class AsyncAutoWrapClient : AutoWrapClient
        {
            public AsyncAutoWrapClient(int Target, TCPClientConnector Connector) : base(Target, Connector)
            {
                GC.SuppressFinalize(this);
            }


            static bool TryAsyncInvokeMember(int Target, string MethodName, object[] args, out object result, TCPClientConnector Connector)
            {
                var tcs = new TaskCompletionSource<object>();

                var ms = new MemoryStream();
                var bw = new BinaryWriter(ms);

                bw.Write((byte)CallMethod.CallFuncAsync);
                bw.Write(Target);
                bw.Write(MethodName);
                bw.Write(args.Length);

                foreach (var arg in args)
                    WorkVariants.WriteObject(arg, bw);

                var g = Guid.NewGuid();

                Connector.AsyncDictionary.Add(g, tcs);
                bw.Write(g.ToByteArray());
                bw.Write(Connector.PortForCallBack);

                bw.Flush();

                var res = Connector.SendMessage(ms);
                GetAsyncResult(res, tcs, Connector);
                result = tcs.Task;
                return true;

            }


            internal object SetAsyncError()
            {
                var tcs = new TaskCompletionSource<object>();
                tcs.SetException(new Exception(Connector.LastError));
                return tcs.Task;

            }
            public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
            {
            result = null;
            if (Connector.ServerIsClosed) return false;

            if (args.Length > 0 && args[0] != null && args[0].GetType() == typeof(object[]))
                {
                    object resAsync = null;


                    if (!AutoWrapClient.TryInvokeGenericMethod(Target, binder.Name, args, out resAsync, Connector))
                    {
                        result = SetAsyncError();
                        return true;

                    }

                    return TryAsyncInvokeMember(0, "ReturnParam", new object[] { resAsync }, out result, Connector);
                }
                return TryAsyncInvokeMember(Target, binder.Name, args, out result, Connector);

            }

            public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
            {

                object resAsync = null;
                if (!base.TryInvoke(binder, args, out resAsync))
                    {
                    result = SetAsyncError();
                    return true;

                }

                return TryAsyncInvokeMember(0, "ReturnParam", new object[] { resAsync }, out result, Connector);

            }

            public override bool TryGetIndex(
           GetIndexBinder binder, object[] indexes, out object result)
            {


                object resAsync = null;
                if (!base.TryGetIndex(binder, indexes, out resAsync))
                {
                    result = SetAsyncError();
                    return true;

                }

                return TryAsyncInvokeMember(0, "ReturnParam", new object[] { resAsync }, out result, Connector);

            }
        protected override void Dispose(bool disposing)
        {
           
        }
    }

    
    
}
