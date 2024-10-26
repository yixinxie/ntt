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
    public static void unmanaged_bytes2managed_copy(byte[] bytes_managed, NativeList<byte> native_buffer)
    {
        unsafe
        {
#if ASM_UNSAFE_DEBUG
            if(native_buffer.Length > bytes_managed.Length)
            {
                throw_exception();
            }
#endif
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
    // managed2nbytes
    public static void managed2unsafe_bytes(ref UnsafeList<byte> unsafe_buffer, byte[] bytes_managed)
    {
        unsafe
        {
            int offset = unsafe_buffer.Length;
            unsafe_buffer.AddReplicate(0, bytes_managed.Length);

#if ASM_UNSAFE_DEBUG
            if (offset + bytes_managed.Length > unsafe_buffer.Length)
            {
                throw_exception();
            }
#endif

            fixed (void* ptr = bytes_managed)
            {
                UnsafeUtility.MemCpy(unsafe_buffer.Ptr + offset, ptr, bytes_managed.Length);//protected
            }
        }
    }
    // managed2ubytes
    public static void managed2unsafe_bytes(NativeList<byte> na_buffer, byte[] bytes_managed)
    {
        unsafe
        {
            int offset = na_buffer.Length;
            na_buffer.AddReplicate(0, bytes_managed.Length);

#if ASM_UNSAFE_DEBUG
            if (offset + bytes_managed.Length > na_buffer.Length)
            {
                throw_exception();
            }
#endif

            fixed (void* ptr = bytes_managed)
            {
                UnsafeUtility.MemCpy(na_buffer.GetUnsafePtr() + offset, ptr, bytes_managed.Length);//protected
            }
        }
    }
   
    public static void us_bytes(NativeList<byte> target_buffer, byte[] source_bytes)
    {
        unsafe
        {
            ns_generic(target_buffer, source_bytes.Length);
            int offset = target_buffer.Length;
            target_buffer.AddReplicate(0, source_bytes.Length);

#if ASM_UNSAFE_DEBUG
            if (offset + source_bytes.Length != target_buffer.Length) throw_exception();
#endif
            fixed (void* ptr = source_bytes)
            {
                UnsafeUtility.MemCpy(target_buffer.GetUnsafePtr() + offset, ptr, source_bytes.Length);//protected
            }
        }
    }
    //public static void us_bytes(ref UnsafeList<byte> target_buffer, byte[] source_bytes)
    //{
    //    unsafe
    //    {
    //        us_struct(ref target_buffer, source_bytes.Length);
    //        int offset = target_buffer.Length;
    //        target_buffer.AddReplicate(0, source_bytes.Length);
    //        fixed (void* ptr = source_bytes)
    //        {
    //            UnsafeUtility.MemCpy(target_buffer.Ptr + offset, ptr, source_bytes.Length);
    //        }
    //    }
    //    //unsafe
    //    //{
    //    //    UnsafeList<byte> source_buffer = new UnsafeList<byte>(source_bytes.Length, Allocator.Temp);
    //    //    int offset = target_buffer.Length;
    //    //    target_buffer.AddReplicate(0, source_buffer.Length);
    //    //    UnsafeUtility.MemCpy(target_buffer.Ptr + offset, source_buffer.Ptr, source_buffer.Length);
    //    //}
    //}
    public static void ubytes2native(out NativeList<byte> target_buffer, UnsafeList<byte> source_buffer, Allocator alloc)
    {
        target_buffer = new NativeList<byte>(source_buffer.Length, alloc);
        target_buffer.AddReplicate(0, source_buffer.Length);

#if ASM_UNSAFE_DEBUG
        if (source_buffer.Length != target_buffer.Length) throw_exception();
#endif
        unsafe
        {
            UnsafeUtility.MemCpy(target_buffer.GetUnsafePtr(), source_buffer.Ptr, source_buffer.Length);//protected
        }
    }
    public static void native2ubytes(out UnsafeList<byte> target_buffer, NativeList<byte> source_buffer, Allocator alloc)
    {
        target_buffer = new UnsafeList<byte>(source_buffer.Length, alloc);
        target_buffer.AddReplicate(0, source_buffer.Length);

#if ASM_UNSAFE_DEBUG
        if (source_buffer.Length != target_buffer.Length) throw_exception();
#endif
        unsafe
        {
            UnsafeUtility.MemCpy(target_buffer.Ptr, source_buffer.GetUnsafePtr(), source_buffer.Length);//protected
        }
    }
    //public static void nbytes2unsafe(out UnsafeList<byte> target_buffer, NativeList<byte> source_buffer, Allocator alloc)
    //{
    //    target_buffer = new UnsafeList<byte>(source_buffer.Length, alloc);
    //    target_buffer.AddReplicate(0, source_buffer.Length);
    //    unsafe
    //    {
    //        UnsafeUtility.MemCpy(target_buffer.Ptr, source_buffer.GetUnsafePtr(), source_buffer.Length);
    //    }
    //}
    public static void us_native_list(NativeList<byte> target_buffer, NativeList<byte> source_buffer)
    {
        unsafe
        {
            ns_generic(target_buffer, source_buffer.Length);
            int offset = target_buffer.Length;
            target_buffer.AddReplicate(0, source_buffer.Length);

#if ASM_UNSAFE_DEBUG
            if (offset + source_buffer.Length != target_buffer.Length) throw_exception();
#endif

            UnsafeUtility.MemCpy(target_buffer.GetUnsafePtr() + offset, source_buffer.GetUnsafePtr(), source_buffer.Length);//protected
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
#if ASM_UNSAFE_DEBUG
            if (offset + sizeof(T) != buffer.Length) throw_exception();
#endif
            UnsafeUtility.CopyStructureToPtr(ref val, buffer.Ptr + offset);//protected
        }
    }
    //public static void ns_generic<T>(NativeList<byte> buffer, T val) where T : unmanaged
    //{
    //    unsafe
    //    {
    //        int offset = buffer.Length;
    //        buffer.AddReplicate(0, sizeof(T));
    //        UnsafeUtility.CopyStructureToPtr(ref val, buffer.GetUnsafePtr() + offset);
    //    }
    //}
    public static void us_struct_offset<T>(NativeList<byte> buffer, T val, int offset) where T : unmanaged
    {


        unsafe
        {
#if ASM_UNSAFE_DEBUG
            if (offset + sizeof(T) > buffer.Length) throw_exception();
#endif
            UnsafeUtility.CopyStructureToPtr(ref val, buffer.GetUnsafePtr() + offset);//protected
        }
    }

    public static void us_struct_and_length<T>(NativeList<byte> buffer, T val) where T : unmanaged
    {
        unsafe
        {
            int length = sizeof(T);
            ns_generic(buffer, length);
            int offset = buffer.Length;
            buffer.AddReplicate(0, length);
#if ASM_UNSAFE_DEBUG
            if (offset + sizeof(T) > buffer.Length) throw_exception();
#endif
            UnsafeUtility.CopyStructureToPtr(ref val, buffer.GetUnsafePtr() + offset);//protected
        }
    }

    public static void us_na<T>(ref UnsafeList<byte> buffer, NativeArray<T> db) where T : unmanaged
    {
        unsafe
        {
            us_struct(ref buffer, db.Length);
            int offset = buffer.Length;
            int stride = sizeof(T);
            buffer.AddReplicate(0, stride * db.Length);
            for (int i = 0; i < db.Length; ++i)
            {
                var tmp = db[i];
#if ASM_UNSAFE_DEBUG
                if (offset + stride > buffer.Length) throw_exception();
#endif
                UnsafeUtility.CopyStructureToPtr(ref tmp, buffer.Ptr + offset);//protected
                offset += stride;
            }
        }
    }
    public static void us_na<T>(NativeList<byte> target_buffer, NativeArray<T> db) where T : unmanaged
    {
        unsafe
        {
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
            //target_buffer.AddRange(db.GetUnsafePtr(), stride * db.Length);
            //UnsafeUtility.MemCpy(target_buffer.GetUnsafePtr() + offset, db.GetUnsafePtr(), stride * db.Length);
        }
    }

    // for legacy compatibility
    public static void us_db<T>(NativeList<byte> target_buffer, NativeArray<T> db) where T : unmanaged
    {
        unsafe
        {
            int stride = sizeof(T);
            int db_stride = stride * db.Length;
            ns_generic(target_buffer, db.Length);
            int offset = target_buffer.Length;
            target_buffer.AddReplicate(0, db_stride);
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
   

    public static void ud_unsafe_list(UnsafeList<byte> buffer, out UnsafeList<byte> out_buffer, ref int offset, Allocator alloc)
    {
        unsafe
        {
            ud_struct(buffer, out int length, ref offset);
            out_buffer = new UnsafeList<byte>(length, alloc);
            out_buffer.AddReplicate(0, length);

#if ASM_UNSAFE_DEBUG
            if (length != out_buffer.Length) throw_exception();
#endif

#if ASM_UNSAFE_DEBUG
            if (offset + length > buffer.Length) throw_exception();
#endif

            UnsafeUtility.MemCpy(out_buffer.Ptr, buffer.Ptr + offset,  length);
            offset += length;
        }
    }
    public static void nd_native_list(NativeList<byte> buffer, out NativeList<byte> out_buffer, ref int offset, Allocator alloc)
    {
        unsafe
        {
            ud_struct(buffer, out int length, ref offset);
            out_buffer = new NativeList<byte>(length, alloc);
            out_buffer.AddReplicate(0, length);

#if ASM_UNSAFE_DEBUG
            if (length != out_buffer.Length) throw_exception();
#endif

#if ASM_UNSAFE_DEBUG
            if (offset + length > buffer.Length) throw_exception();
#endif
            UnsafeUtility.MemCpy(out_buffer.GetUnsafePtr(), buffer.GetUnsafePtr() + offset, length);
            offset += length;
        }
    }

    public static void ud_struct<T>(UnsafeList<byte> buffer, out T val, ref int offset) where T : unmanaged
    {
        unsafe
        {

#if ASM_UNSAFE_DEBUG
            if (offset + sizeof(T) > buffer.Length) throw_exception();
#endif

            UnsafeUtility.CopyPtrToStructure(buffer.Ptr + offset, out val);//protected
            offset += sizeof(T);
        }
    }

    public static void ud_bytes(UnsafeList<byte> unsafe_buffer, ref byte[] bytes_managed, ref int offset)
    {
        ud_struct(unsafe_buffer, out int bytes_managed_length, ref offset);
        if (bytes_managed_length > 0)
        {
            bytes_managed = new byte[bytes_managed_length];

#if ASM_UNSAFE_DEBUG
            if (bytes_managed_length != bytes_managed.Length) throw_exception();
#endif

#if ASM_UNSAFE_DEBUG
            if (offset + bytes_managed_length > unsafe_buffer.Length) throw_exception();
#endif

            unsafe
            {
                fixed (void* ptr = bytes_managed)
                {
                    UnsafeUtility.MemCpy(ptr, unsafe_buffer.Ptr + offset, bytes_managed_length);
                    offset += bytes_managed_length;
                }
            }
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
#if ASM_UNSAFE_DEBUG
            if (offset + sizeof(T) > buffer.Length) throw_exception();
#endif

            UnsafeUtility.CopyPtrToStructure(buffer.GetUnsafePtr() + offset, out val);//protected
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

#if ASM_UNSAFE_DEBUG
            if (na_length != db.Length) throw_exception();
#endif

            for (int i = 0; i < na_length; ++i)
            {

#if ASM_UNSAFE_DEBUG
                if (offset + stride > buffer.Length) throw_exception();
#endif

                UnsafeUtility.CopyPtrToStructure(buffer.Ptr + offset, out T tmp);//protected
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
#if ASM_UNSAFE_DEBUG
            if (na_length != db.Length) throw_exception();
#endif
            for (int i = 0; i < na_length; ++i)
            {
#if ASM_UNSAFE_DEBUG
                if (offset + stride > buffer.Length) throw_exception();
#endif
                UnsafeUtility.CopyPtrToStructure(buffer.GetUnsafePtr() + offset, out T tmp);//protected
                db[i] = tmp;
                offset += stride;
            }
        }
    }
   
}
