using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Analytics;

// for auto serialization
public partial class Bursted
{
  
    public static void ns_generic<T>(NativeList<byte> buffer, T val) where T : unmanaged
    {
        unsafe
        {
            int offset = buffer.Length;
            buffer.AddReplicate(0, sizeof(T));
#if ASM_UNSAFE_DEBUG
            if (offset + sizeof(T) != buffer.Length) throw_exception();
#endif

            UnsafeUtility.CopyStructureToPtr(ref val, buffer.GetUnsafePtr() + offset);//protected
        }
    }
    public static void ns_generic<T>(NativeList<byte> target_buffer, NativeArray<T> db) where T : unmanaged
    {
        unsafe
        {
            if (db.IsCreated == false)
            {
                ns_generic(target_buffer, 0);
                return;
            }
            ns_generic(target_buffer, db.Length);
            int offset = target_buffer.Length;
            int stride = sizeof(T);
            target_buffer.AddReplicate(0, stride * db.Length);
            for (int i = 0; i < db.Length; ++i)
            {
                var tmp = db[i];
#if ASM_UNSAFE_DEBUG
                if (offset + stride > target_buffer.Length) throw_exception();
#endif
                UnsafeUtility.CopyStructureToPtr(ref tmp, target_buffer.GetUnsafePtr() + offset);//protected
                offset += stride;
            }
        }
    }
    // NativeList<T>
    public static void ns_generic<T>(NativeList<byte> target_buffer, NativeList<T> db) where T : unmanaged
    {
        unsafe
        {
            if (db.IsCreated == false)
            {
                ns_generic(target_buffer, 0);
                return;
            }
            ns_generic(target_buffer, db.Length);
            int offset = target_buffer.Length;
            int stride = sizeof(T);
            target_buffer.AddReplicate(0, stride * db.Length);
            for (int i = 0; i < db.Length; ++i)
            {
                var tmp = db[i];
                UnsafeUtility.CopyStructureToPtr(ref tmp, target_buffer.GetUnsafePtr() + offset);
                offset += stride;
            }
        }
    }

    [BurstDiscard]
    public static void ns_generic<T>(NativeList<byte> buffer, T[] val) where T:unmanaged
    {
        if (val == null)
        {
            ns_generic(buffer, 0);
        }
        else
        {
            ns_generic(buffer, val.Length);
            unsafe
            {

                int offset = buffer.Length;
                int stride = sizeof(T);
                buffer.AddReplicate(0, val.Length * stride);
#if ASM_UNSAFE_DEBUG
                if (offset + val.Length * stride > buffer.Length) throw_exception();
#endif
                fixed (void* tmp = val)
                {
                    UnsafeUtility.MemCpy(buffer.GetUnsafePtr() + offset, tmp, val.Length * stride);//protected
                }
            }
        }
    }
    [BurstDiscard]
    public static void ns_generic(NativeList<byte> buffer, string strVal)
    {
        if (string.IsNullOrEmpty(strVal))
        {
            ns_generic(buffer, 0);
        }
        else
        {
            // string length
            byte[] stringBytes = Encoding.Unicode.GetBytes(strVal);
            ns_generic(buffer, stringBytes.Length);

            // string content
            unsafe
            {
                
                int offset = buffer.Length;
                buffer.AddReplicate(0, stringBytes.Length);
#if ASM_UNSAFE_DEBUG
                if (offset + stringBytes.Length > buffer.Length) throw_exception();
#endif
                fixed (void* tmp = stringBytes)
                {
                    UnsafeUtility.MemCpy(buffer.GetUnsafePtr() + offset, tmp, stringBytes.Length); // protected
                }
            }
        }
    }

    public static void ns_unsafelist<T>(NativeList<byte> buffer, UnsafeList<T> val) where T : unmanaged
    {
        if (val.IsCreated == false)
        {
            ns_generic(buffer, 0);
            return;
        }
        unsafe
        {
            ns_generic(buffer, val.Length);
            int offset = buffer.Length;
            int total = sizeof(T) * val.Length;
            buffer.AddReplicate(default, total);
#if ASM_UNSAFE_DEBUG
            if (offset + total != buffer.Length) throw_exception();
#endif
            UnsafeUtility.MemCpy(buffer.GetUnsafePtr() + offset, val.Ptr, total);//protected
        }
    }

    // deserializations
    public static void nd_generic<T>(NativeList<byte> buffer, out T val, ref int offset, Allocator alloc) where T : unmanaged
    {
        unsafe
        {
#if ASM_UNSAFE_DEBUG
            if (offset + sizeof(T) > buffer.Length) throw_exception();
#endif
            UnsafeUtility.CopyPtrToStructure(buffer.GetUnsafePtr() + offset, out val);//protected
            offset += sizeof(T);
        }
    }

    public static void nd_generic<T>(NativeList<byte> buffer, out NativeArray<T> db, ref int offset, Allocator alloc) where T : unmanaged
    {
        unsafe
        {
            nd_generic(buffer, out int na_length, ref offset, alloc);
            int stride = sizeof(T);
            db = new NativeArray<T>(na_length, alloc, NativeArrayOptions.ClearMemory);
#if ASM_UNSAFE_DEBUG
            if (na_length != db.Length) throw_exception();
#endif
            for (int i = 0; i < na_length; ++i)
            {
#if ASM_UNSAFE_DEBUG
                if (offset + sizeof(T) > buffer.Length) throw_exception();
#endif
                UnsafeUtility.CopyPtrToStructure(buffer.GetUnsafePtr() + offset, out T tmp);//protected
                db[i] = tmp;
                offset += stride;
            }
        }
    }
    public static void nd_generic<T>(NativeList<byte> buffer, out NativeList<T> db, ref int offset, Allocator alloc) where T : unmanaged
    {
        unsafe
        {
            nd_generic(buffer, out int na_length, ref offset, alloc);
            int stride = sizeof(T);
            db = new NativeList<T>(na_length, alloc);
            db.AddReplicate(default, na_length);

#if ASM_UNSAFE_DEBUG
            if (na_length != db.Length) throw_exception();
#endif
            for (int i = 0; i < na_length; ++i)
            {
#if ASM_UNSAFE_DEBUG
                if (offset + sizeof(T) > buffer.Length) throw_exception();
#endif
                UnsafeUtility.CopyPtrToStructure(buffer.GetUnsafePtr() + offset, out T tmp);//protected
                db[i] = tmp;
                offset += stride;
            }
        }
    }

    public static void nd_generic<T>(NativeList<byte> buffer, out T[] db, ref int offset, Allocator alloc) where T:unmanaged
    {
        unsafe
        {
            nd_generic(buffer, out int na_length, ref offset, alloc);
            //db = new NativeList<T>(na_length, alloc);
            db = new T[na_length];

#if ASM_UNSAFE_DEBUG
            if (na_length != db.Length) throw_exception();
#endif
            int total_size = sizeof(T) * na_length;

#if ASM_UNSAFE_DEBUG
            if (offset + total_size > buffer.Length) throw_exception();
#endif

            fixed (void* ptr = db)
            {
                UnsafeUtility.MemCpy(ptr, buffer.GetUnsafePtr() + offset, total_size);//protected
            }
            offset += total_size;
        }
    }
    public static void nd_generic(NativeList<byte> buffer, out string str, ref int offset, Allocator alloc)
    {
        unsafe
        {

            nd_generic(buffer, out int length, ref offset, alloc);

            str = "";

            if (length > 0)
            {
                byte[] src = new byte[length];

#if ASM_UNSAFE_DEBUG
                if (offset + length > buffer.Length) throw_exception();
#endif

                fixed (void* ptr = src)
                {
                    UnsafeUtility.MemCpy(ptr, buffer.GetUnsafePtr() + offset, length);//protected
                }
                str = Encoding.Unicode.GetString(src, 0, length);
                offset += length;
            }

        }
    }

    public static void nd_unsafelist<T>(NativeList<byte> buffer, out UnsafeList<T> val, ref int offset, Allocator alloc) where T : unmanaged
    {
        unsafe
        {

            nd_generic(buffer, out int length, ref offset, alloc);
            int total = sizeof(T) * length;
            val = new UnsafeList<T>(length, alloc);
            val.AddReplicate(default, length);

#if ASM_UNSAFE_DEBUG
            if (length != val.Length) throw_exception();
#endif

#if ASM_UNSAFE_DEBUG
            if (offset + total > buffer.Length) throw_exception();
#endif

            UnsafeUtility.MemCpy(val.Ptr, buffer.GetUnsafePtr() + offset, total);//protected
            offset += total;

        }
    }

    //unsafe public static void write_check0<T>(UnsafeList<T> buffer, int total) where T:unmanaged
    //{
    //    if (total >= buffer.Length * sizeof(T))
    //    {
    //        throw_exception();
    //    }
    //}
    //public static void read_check0(NativeList<byte> buffer, int offset)
    //{
    //    if(offset >= buffer.Length)
    //    {
    //        throw_exception();
    //    }
    //}
    public static void throw_exception()
    {
        Debug.LogError("unsafe i/o error!");
        Debug.DebugBreak();
        throw new Exception("unsafe read out of bounds");
    }
}
