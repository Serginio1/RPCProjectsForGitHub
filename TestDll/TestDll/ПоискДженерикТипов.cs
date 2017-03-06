using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
namespace TestDllForCoreClr
{
   public static class  ПоискДженерикТипов
    {
        public static bool IsGenericTypeOf(this Type t, Type genericDefinition)
        {
            Type[] parameters = null;
            return IsGenericTypeOf(t, genericDefinition, out parameters);
        }

        public static bool IsGenericTypeOf(this Type t, Type genericDefinition, out Type[] genericParameters)
        {
            genericParameters = new Type[] { };
            if (!genericDefinition.GetTypeInfo().IsGenericType)
            {
                return false;
            }

            var isMatch = t.GetTypeInfo().IsGenericType && t.GetGenericTypeDefinition() == genericDefinition.GetGenericTypeDefinition();
            if (!isMatch && t.GetTypeInfo().BaseType != null)
            {
                isMatch = IsGenericTypeOf(t.GetTypeInfo().BaseType, genericDefinition, out genericParameters);
            }
            if (!isMatch && genericDefinition.GetTypeInfo().IsInterface && t.GetInterfaces().Any())
            {
                foreach (var i in t.GetInterfaces())
                {
                    if (i.IsGenericTypeOf(genericDefinition, out genericParameters))
                    {
                        isMatch = true;
                        break;
                    }
                }
            }

            if (isMatch && !genericParameters.Any())
            {
                genericParameters = t.GetGenericArguments();
            }
            return isMatch;
        }

        private static bool IsSimilarType(this Type thisType, Type type)
        {
            // Ignore any 'ref' types
            
            if (type.IsByRef)
                type = type.GetElementType();

            // Handle array types
            if (type.IsArray)
                return thisType.IsSimilarType(type.GetElementType());

            if (thisType == type)
                return true;

            // Handle any generic arguments
                if (type.GetTypeInfo().IsGenericType)
            {
               Type[] arguments = type.GetGenericArguments();
               
                    for (int i = 0; i < arguments.Length; ++i)
                    {
                        if (thisType.IsSimilarType(arguments[i]))
                            return true;
                    }
                 
                
            }

            return false;
        }

        public static bool НайтиПараметрыДляВывода(this MethodInfo  methodInfo,out List<List<int>> res )
            {

            res = new List<List<int>>();
            var genericTypes = methodInfo.GetGenericArguments();
            ParameterInfo[] parameterInfos = methodInfo.GetParameters();

            foreach (Type tParam in genericTypes)
            {
                var matchingParam = false;
                var index = new List<int>();
                res.Add(index);
                int i = 0;
                foreach (var Param in parameterInfos)
                {
                    
                  if (tParam.IsSimilarType(Param.ParameterType))
                    {
                        matchingParam = true;
                        index.Add(i);
 
                    }
                    i++;

                }

                if (!matchingParam)
                {
                    res = null;
                    return false;

                }
            }
            return true;
        }

    }
}
