﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Net;
namespace ClientRPC
{

     public enum EnumVar:byte
    {
        VTYPE_EMPTY = 0,
        VTYPE_NULL,
        VTYPE_I2,                   //int16
        VTYPE_I4,                   //int32
        VTYPE_R4,                   //float
        VTYPE_R8,                   //double
        VTYPE_Decimal,              //Decimal
        VTYPE_DATE,                 //DATE (double)
        VTYPE_BOOL,                 //bool
        VTYPE_I1,                   //int8
        VTYPE_UI1,                  //uint8
        VTYPE_UI2,                  //uint16
        VTYPE_UI4,                  //uint32
        VTYPE_I8,                   //int64
        VTYPE_UI8,                  //uint64
        VTYPE_INT,                  //int   Depends on architecture
        VTYPE_CHAR,                 //char
        VTYPE_PWSTR,                //struct wstr
        VTYPE_BLOB,                 //means in struct str binary data contain
        VTYPE_GUID,                 //Guid
        VTYPE_AutoWrap,             // Net Object
        VTYPE_JSObject



    };


    // Класс WorkVariants осуществляет сериализацию и десериализацию объектов
    public class WorkVariants
    {
        internal static Dictionary<Type, EnumVar> MatchTypes;
        
        static WorkVariants()
        {

            // Напрямую сериализуются byte[],числа, строки,булево,Дата,char,Guid
            //Для AutoWrapClient передается индекс в хранилище
            MatchTypes = new Dictionary<Type, EnumVar>()
            { 
                { typeof(Int16),EnumVar.VTYPE_I2 },
                {typeof(Int32),EnumVar.VTYPE_I4 },
                {typeof(float),EnumVar.VTYPE_R4 },
                {typeof(double),EnumVar.VTYPE_R8 },
                {typeof(decimal),EnumVar.VTYPE_Decimal},
                {typeof(bool),EnumVar.VTYPE_BOOL },
                {typeof(sbyte),EnumVar.VTYPE_I1 },
                {typeof(byte),EnumVar.VTYPE_UI1 },
                {typeof(UInt16),EnumVar.VTYPE_UI2},
                {typeof(UInt32),EnumVar.VTYPE_UI4},
                {typeof(Int64),EnumVar.VTYPE_I8},
                {typeof(UInt64),EnumVar.VTYPE_UI8},
                {typeof(char),EnumVar.VTYPE_CHAR},
                {typeof(string),EnumVar.VTYPE_PWSTR},
                {typeof(byte[]),EnumVar.VTYPE_BLOB},
                {typeof(DateTime),EnumVar.VTYPE_DATE},
                {typeof(AutoWrapClient),EnumVar.VTYPE_AutoWrap},
                {typeof(Guid),EnumVar.VTYPE_GUID}//,
             //   {typeof(AutoWrap),EnumVar.VTYPE_JSObject}
            };


        }

     public static DateTime ReadDateTime(BinaryReader stream)
        {
            long nVal = stream.ReadInt64();
            //get 64bit binary
            return DateTime.FromBinary(nVal);


        }

        public static void WriteDateTime(DateTime value,BinaryWriter stream)
        {
            long nVal = value.ToBinary();
            //get 64bit binary
             stream.Write(nVal);


        }
        public static byte[] ReadByteArray(BinaryReader stream)
        {
            var length = stream.ReadInt32();
            return stream.ReadBytes(length);

        }
      public static  object GetObject(BinaryReader stream, TCPClientConnector Connector)
        {
            
            // Считываем тип объекта
            EnumVar type =(EnumVar)stream.ReadByte();

            // В зависмости от типа считываем и преобразуем данные
            switch (type)
            {
                case EnumVar.VTYPE_EMPTY:
                case EnumVar.VTYPE_NULL: return null;
                case EnumVar.VTYPE_I2: return stream.ReadInt16();
                case EnumVar.VTYPE_I4: return stream.ReadInt32();
                case EnumVar.VTYPE_R4: return stream.ReadSingle();
                case EnumVar.VTYPE_R8: return stream.ReadDouble();
                case EnumVar.VTYPE_Decimal: return stream.ReadDecimal();
                case EnumVar.VTYPE_BOOL: return stream.ReadBoolean();
                case EnumVar.VTYPE_I1: return stream.ReadSByte();
                case EnumVar.VTYPE_UI1: return stream.ReadByte();
                case EnumVar.VTYPE_UI2: return stream.ReadUInt16();

                case EnumVar.VTYPE_UI4: return stream.ReadUInt32();

                case EnumVar.VTYPE_I8: return stream.ReadInt64();
                case EnumVar.VTYPE_UI8: return stream.ReadUInt64();
                case EnumVar.VTYPE_CHAR: return stream.ReadChar();
                case EnumVar.VTYPE_PWSTR: return stream.ReadString();

                case EnumVar.VTYPE_BLOB: return ReadByteArray(stream);
                case EnumVar.VTYPE_DATE: return ReadDateTime(stream);
                case EnumVar.VTYPE_GUID: return new Guid(stream.ReadBytes(16));


                case EnumVar.VTYPE_AutoWrap:
                        var Target= stream.ReadInt32();
                        var AW = new AutoWrapClient(Target, Connector);


                    return AW;
              
            }
            return null;
            }


    
        public static bool WriteObject(object Объект, BinaryWriter stream)
        {


            // Если null то записываем только VTYPE_NULL
            if (Объект == null)
            {

                stream.Write((byte)EnumVar.VTYPE_NULL);
                 return true;

            }

            // Если это RefParam то сериализуем значение из Value
            // Нужен для возвращения out значения в Value 
            if (Объект.GetType() == typeof(RefParam))
            {
                object value= ((RefParam)Объект).Value;
                return WriteObject(value, stream);
            }
            EnumVar type;

            // Ищем тип в словаре MatchTypes
            var res = MatchTypes.TryGetValue(Объект.GetType(), out type);


            // Если тип не поддерживаемый вызываем исключение
            if (!res) {

                throw new Exception("Неверный тип " + Объект.GetType().ToString());
             //   return false;
            }

            // Записываем тип объекта
            stream.Write((byte)type);

            // В зависимости от типа сериализуем объект
            switch (type)
            {
                case EnumVar.VTYPE_I2: stream.Write((Int16)Объект); break;
                case EnumVar.VTYPE_I4: stream.Write((Int32)Объект); break;
                case EnumVar.VTYPE_R4: stream.Write((float)Объект); break;
                case EnumVar.VTYPE_R8: stream.Write((double)Объект); break;
                case EnumVar.VTYPE_Decimal: stream.Write((decimal)Объект); break;
                case EnumVar.VTYPE_BOOL: stream.Write((bool)Объект); break;
                case EnumVar.VTYPE_I1: stream.Write((sbyte)Объект); break;
                case EnumVar.VTYPE_UI1: stream.Write((byte)Объект); break;
                case EnumVar.VTYPE_UI2: stream.Write((UInt16)Объект); break;

                case EnumVar.VTYPE_UI4: stream.Write((UInt32)Объект); break;

                case EnumVar.VTYPE_I8: stream.Write((Int64)Объект); break;
                case EnumVar.VTYPE_UI8: stream.Write((UInt64)Объект); break;
                case EnumVar.VTYPE_CHAR: stream.Write((char)Объект); break;
                case EnumVar.VTYPE_PWSTR: stream.Write((string)Объект); break;

                case EnumVar.VTYPE_BLOB: stream.Write((byte[])Объект); break;
                case EnumVar.VTYPE_DATE: WriteDateTime((DateTime)Объект, stream); break;
                case EnumVar.VTYPE_GUID: stream.Write(((Guid)Объект).ToByteArray()); break;
                case EnumVar.VTYPE_AutoWrap:
                    stream.Write(((AutoWrapClient)Объект).Target);
                    break;
                    
            }
            return true;
        }

    }

   

}

