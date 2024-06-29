using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Analytics;

// bursted serializations
[BurstCompile]
public partial class Bursted
{
    //[BurstDiscard]
    //public static void cd_lookup<T>(ref ComponentLookup<T> lookup) where T : unmanaged, IComponentData
    //{
    //    lookup = TileToGridSystem.self.GetComponentLookup<T>();
    //}
    public static void unsafe_bytes2managed(ref byte[] bytes_managed, NativeList<byte> native_buffer)
    {
        bytes_managed = new byte[native_buffer.Length];
        unsafe
        {
            fixed (void* ptr = bytes_managed)
            {
                UnsafeUtility.MemCpy(ptr, native_buffer.GetUnsafePtr(), native_buffer.Length);
            }
        }
    }
    public static void unsafe_bytes2managed(ref byte[] bytes_managed, UnsafeList<byte> unsafe_buffer)
    {
        bytes_managed = new byte[unsafe_buffer.Length];
        unsafe
        {
            fixed (void* ptr = bytes_managed)
            {
                UnsafeUtility.MemCpy(ptr, unsafe_buffer.Ptr, unsafe_buffer.Length);
            }
        }
    }
    public static void managed2unsafe_bytes(ref UnsafeList<byte> unsafe_buffer, byte[] bytes_managed)
    {
        unsafe
        {
            int offset = unsafe_buffer.Length;
            unsafe_buffer.AddReplicate(0, bytes_managed.Length);
            fixed (void* ptr = bytes_managed)
            {
                UnsafeUtility.MemCpy(unsafe_buffer.Ptr + offset, ptr, bytes_managed.Length);
            }
        }
    }
    public static void managed2unsafe_bytes(NativeList<byte> na_buffer, byte[] bytes_managed)
    {
        unsafe
        {
            int offset = na_buffer.Length;
            na_buffer.AddReplicate(0, bytes_managed.Length);
            fixed (void* ptr = bytes_managed)
            {
                UnsafeUtility.MemCpy(na_buffer.GetUnsafePtr() + offset, ptr, bytes_managed.Length);
            }
        }
    }
   
    public static void us_bytes(NativeList<byte> target_buffer, byte[] source_bytes)
    {
        unsafe
        {
            us_struct(target_buffer, source_bytes.Length);
            int offset = target_buffer.Length;
            target_buffer.AddReplicate(0, source_bytes.Length);
            fixed (void* ptr = source_bytes)
            {
                UnsafeUtility.MemCpy(target_buffer.GetUnsafePtr() + offset, ptr, source_bytes.Length);
            }
        }
    }
    public static void us_bytes(ref UnsafeList<byte> target_buffer, byte[] source_bytes)
    {
        unsafe
        {
            us_struct(ref target_buffer, source_bytes.Length);
            int offset = target_buffer.Length;
            target_buffer.AddReplicate(0, source_bytes.Length);
            fixed (void* ptr = source_bytes)
            {
                UnsafeUtility.MemCpy(target_buffer.Ptr + offset, ptr, source_bytes.Length);
            }
        }
        //unsafe
        //{
        //    UnsafeList<byte> source_buffer = new UnsafeList<byte>(source_bytes.Length, Allocator.Temp);
        //    int offset = target_buffer.Length;
        //    target_buffer.AddReplicate(0, source_buffer.Length);
        //    UnsafeUtility.MemCpy(target_buffer.Ptr + offset, source_buffer.Ptr, source_buffer.Length);
        //}
    }
    //public static void us_bytes(ref UnsafeList<byte> target_buffer, UnsafeList<byte> source_buffer)
    //{
        //unsafe
        //{
        //    int offset = target_buffer.Length;
        //    target_buffer.AddReplicate(0, source_buffer.Length);
        //    UnsafeUtility.MemCpy(target_buffer.Ptr + offset, source_buffer.Ptr, source_buffer.Length);
        //}
    //}
    public static void us_native_list(NativeList<byte> target_buffer, NativeList<byte> source_buffer)
    {
        unsafe
        {
            us_struct(target_buffer, source_buffer.Length);
            int offset = target_buffer.Length;
            target_buffer.AddReplicate(0, source_buffer.Length);
            UnsafeUtility.MemCpy(target_buffer.GetUnsafePtr() + offset, source_buffer.GetUnsafePtr(), source_buffer.Length);
        }
    }
    //public static void us_struct<T>(ref UnsafeList<byte> buffer, ref T val) where T : unmanaged
    //{
    //    unsafe
    //    {
    //        int offset = buffer.Length;
    //        buffer.AddReplicate(0, sizeof(T));
    //        UnsafeUtility.CopyStructureToPtr(ref val, buffer.Ptr + offset);
    //    }
    //}
    public static void us_struct<T>(ref UnsafeList<byte> buffer, T val) where T : unmanaged
    {
        unsafe
        {
            int offset = buffer.Length;
            buffer.AddReplicate(0, sizeof(T));
            UnsafeUtility.CopyStructureToPtr(ref val, buffer.Ptr + offset);
        }
    }
    public static void us_struct<T>(NativeList<byte> buffer, T val) where T : unmanaged
    {
        unsafe
        {
            int offset = buffer.Length;
            buffer.AddReplicate(0, sizeof(T));
            UnsafeUtility.CopyStructureToPtr(ref val, buffer.GetUnsafePtr() + offset);
        }
    }
    public static void us_struct_partial<T>(NativeList<byte> buffer, ref T val, int partial_length) where T : unmanaged
    {
        unsafe
        {
            int offset = buffer.Length;
            buffer.AddReplicate(0, partial_length);
            fixed (void* ptr = &val)
            {
                UnsafeUtility.MemCpy(buffer.GetUnsafePtr() + offset, ptr, partial_length);
            }
            //UnsafeUtility.CopyStructureToPtr(ref val, buffer.GetUnsafePtr() + offset);
        }
    }
    public static void us_struct<T>(NativeList<byte> buffer, T val, int offset) where T : unmanaged
    {
        unsafe
        {
            UnsafeUtility.CopyStructureToPtr(ref val, buffer.GetUnsafePtr() + offset);
        }
    }
    public static void us_struct_and_length<T>(NativeList<byte> buffer, T val) where T : unmanaged
    {
        unsafe
        {
            int length = sizeof(T);
            us_struct(buffer, length);
            int offset = buffer.Length;
            buffer.AddReplicate(0, length);
            UnsafeUtility.CopyStructureToPtr(ref val, buffer.GetUnsafePtr() + offset);
        }
    }

    public static void us_na<T>(ref UnsafeList<byte> buffer, NativeArray<T> db) where T : unmanaged
    {
        unsafe
        {
            if (db.IsCreated == false)
            {
                us_struct(ref buffer, 0);
                return;
            }
            us_struct(ref buffer, db.Length);
            int offset = buffer.Length;
            int stride = sizeof(T);
            buffer.AddReplicate(0, stride * db.Length);
            for (int i = 0; i < db.Length; ++i)
            {
                var tmp = db[i];
                UnsafeUtility.CopyStructureToPtr(ref tmp, buffer.Ptr + offset);
                offset += stride;
            }
        }
    }
    public static void us_na<T>(NativeList<byte> target_buffer, NativeArray<T> db) where T : unmanaged
    {
        unsafe
        {
            if (db.IsCreated == false)
            {
                us_struct(target_buffer, 0);
                return;
            }
            us_struct(target_buffer, db.Length);
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
    public static void us_na<T>(NativeList<byte> target_buffer, NativeList<T> db) where T : unmanaged
    {
        unsafe
        {
            if(db.IsCreated == false)
            {
                us_struct(target_buffer, 0);
                return;
            }
            us_struct(target_buffer, db.Length);
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

    public static void us_db<T>(NativeList<byte> target_buffer, NativeArray<T> db) where T : unmanaged
    {
        unsafe
        {
            int stride = sizeof(T);
            int db_stride = stride * db.Length;
            us_struct(target_buffer, db_stride);
            int offset = target_buffer.Length;
            target_buffer.AddReplicate(0, db_stride);
            for (int i = 0; i < db.Length; ++i)
            {
                var tmp = db[i];
                UnsafeUtility.CopyStructureToPtr(ref tmp, target_buffer.GetUnsafePtr() + offset);
                offset += stride;
            }
        }
    }
  

    public static void ud_unsafe_list(UnsafeList<byte> buffer, out UnsafeList<byte> out_buffer, ref int offset)
    {
        unsafe
        {
            ud_struct(buffer, out int length, ref offset);
            out_buffer = new UnsafeList<byte>(length, Allocator.TempJob);
            UnsafeUtility.MemCpy(buffer.Ptr + offset, out_buffer.Ptr, length);
        }
    }

    public static void ud_struct<T>(UnsafeList<byte> buffer, out T val, ref int offset) where T : unmanaged
    {
        unsafe
        {
            UnsafeUtility.CopyPtrToStructure(buffer.Ptr + offset, out val);
            offset += sizeof(T);
        }
    }

    public static void ud_struct_partial<T>(NativeList<byte> buffer, ref T val, int partial_length, ref int offset) where T : unmanaged
    {
        unsafe
        {
            fixed (void* ptr = &val)
            {
                UnsafeUtility.MemCpy(ptr, buffer.GetUnsafePtr() + offset, partial_length);
            }
            //UnsafeUtility.CopyPtrToStructure(buffer.Ptr + offset, out val);
            offset += partial_length;
        }
    }
    // array index out of bound
    public static bool ud_check<T>(NativeList<byte> buffer, int offset) where T : unmanaged
    {
        unsafe
        {
            return offset + sizeof(T) > buffer.Length;
        }
    }
    public static bool ud_check(NativeList<byte> buffer, int sizeofT, int offset)
    {
        unsafe
        {
            return offset + sizeofT > buffer.Length;
        }
    }
    public static void ud_struct<T>(NativeList<byte> buffer, out T val, ref int offset) where T : unmanaged
    {
        unsafe
        {
            UnsafeUtility.CopyPtrToStructure(buffer.GetUnsafePtr() + offset, out val);
            offset += sizeof(T);
        }
    }
    public static void ud_na<T>(UnsafeList<byte> buffer, out NativeArray<T> db, ref int offset, Allocator alloc) where T : unmanaged
    {
        unsafe
        {
            ud_struct(buffer, out int na_length, ref offset);
            int stride = sizeof(T);
            db = new NativeArray<T>(na_length, alloc);
            for (int i = 0; i < na_length; ++i)
            {
                UnsafeUtility.CopyPtrToStructure(buffer.Ptr + offset, out T tmp);
                db[i] = tmp;
                offset += stride;
            }
        }
    }

    public static void ud_na<T>(NativeList<byte> buffer, out NativeArray<T> db, ref int offset, Allocator alloc) where T : unmanaged
    {
        unsafe
        {
            ud_struct(buffer, out int na_length, ref offset);
            int stride = sizeof(T);
            db = new NativeArray<T>(na_length, alloc);
            for (int i = 0; i < na_length; ++i)
            {
                UnsafeUtility.CopyPtrToStructure(buffer.GetUnsafePtr() + offset, out T tmp);
                db[i] = tmp;
                offset += stride;
            }
        }
    }

    public static void ud_na<T>(NativeList<byte> buffer, out NativeList<T> db, ref int offset, Allocator alloc) where T : unmanaged
    {
        unsafe
        {
            ud_struct(buffer, out int na_length, ref offset);
            int stride = sizeof(T);
            db = new NativeList<T>(na_length, alloc);
            db.AddReplicate(default, na_length);
            for (int i = 0; i < na_length; ++i)
            {
                UnsafeUtility.CopyPtrToStructure(buffer.GetUnsafePtr() + offset, out T tmp);
                db[i] = tmp;
                offset += stride;
            }
        }
    }

}
