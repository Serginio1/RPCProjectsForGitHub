using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;
namespace TestDllForCoreClr
{
    class TestDynamicObject : DynamicObject
    {

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {

            return true;
        }
        // получение свойства
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = binder.Name;
            return true;
        }
        // вызов метода
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            var res = new StringBuilder("{0}(");
            var param = new object[args.Length + 1];
            param[0] = binder.Name;
            if (args.Length > 0)
            {
                Array.Copy(args, 0, param, 1, args.Length);
                for (int i = 0; i < args.Length; i++)
                {
                    res.AppendFormat("{{{0}}},", i + 1);

                }

                res.Remove(res.Length - 1, 1);

            }
            res.Append(")");

            result = String.Format(res.ToString(), param);
            return true;


        }




    }

}


