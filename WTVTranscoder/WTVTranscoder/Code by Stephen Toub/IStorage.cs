using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace FatAttitude.WTVTranscoder
{
    /// <summary>
    /// This interface provides methods for creating and managing the root storage, child storage, and stream objects. 
    /// These methods can create, open, enumerate, move, copy, rename, or delete the elements in the storage object.
    /// </summary>
    [ComImport]
    [Guid("0000000B-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IStorage
    {
        /// <summary>
        /// Creates and opens a stream object with the specified name contained in this storage object.
        /// </summary>
        /// <param name="pwcsName">Pointer to a wide character null-terminated Unicode string that contains the name of the newly created stream.</param>
        /// <param name="grfMode">Specifies the access mode to use when opening the newly created stream.</param>
        /// <param name="reserved1">Reserved for future use; must be zero.</param>
        /// <param name="reserved2">Reserved for future use; must be zero.</param>
        /// <returns>Pointer to the location of the new IStream interface pointer.</returns>
        IStream CreateStream(
            [In, MarshalAs(UnmanagedType.BStr)] string pwcsName,
            [In] uint grfMode, [In] uint reserved1, [In] uint reserved2);

        /// <summary>Opens an existing stream object within this storage object in the specified access mode</summary>
        /// <param name="pwcsName">Pointer to a wide character null-terminated Unicode string that contains the name of the stream to open.</param>
        /// <param name="reserved1">Reserved for future use; must be NULL.</param>
        /// <param name="grfMode">Specifies the access mode to be assigned to the open stream.</param>
        /// <param name="reserved2">Reserved for future use; must be zero.</param>
        /// <returns>Pointer to IStream pointer variable that receives the interface pointer to the newly opened stream object.</returns>
        IStream OpenStream(
            [In, MarshalAs(UnmanagedType.BStr)] string pwcsName,
            [In] IntPtr reserved1,
            [In] uint grfMode, [In] uint reserved2);

        /// <summary>
        /// Creates and opens a new storage object nested within this storage object with the specified name in the specified access mode.
        /// </summary>
        /// <param name="pwcsName">
        /// Pointer to a wide character null-terminated Unicode string that contains the name of the newly created storage object. The name can be used later to reopen the storage object.
        /// </param>
        /// <param name="grfMode">
        /// Specifies the access mode to use when opening the newly created storage object. For descriptions of the possible values, see the STGM enumeration.
        /// </param>
        /// <param name="reserved1">Reserved for future use; must be zero.</param>
        /// <param name="reserved2">Reserved for future use; must be zero.</param>
        /// <returns>
        /// When successful, pointer to the location of the IStorage pointer to the newly created storage object. This parameter is set to NULL if an error occurs.
        /// </returns>
        IStorage CreateStorage(
            [In, MarshalAs(UnmanagedType.BStr)] string pwcsName,
            [In] uint grfMode, [In] uint reserved1, [In] uint reserved2);

        /// <summary>
        /// Opens an existing storage object with the specified name in the specified access mode.
        /// </summary>
        /// <param name="pwcsName">Pointer to a wide character null-terminated Unicode string that contains the name of the storage object to open.</param>
        /// <param name="pstgPriority">Must be NULL. A non-NULL value will return STG_E_INVALIDPARAMETER.</param>
        /// <param name="grfMode">Specifies the access mode to use when opening the storage object.</param>
        /// <param name="snbExclude">Must be NULL. A non-NULL value will return STG_E_INVALIDPARAMETER.</param>
        /// <param name="reserved">Reserved for future use; must be zero.</param>
        /// <returns>When successful, pointer to the location of an IStorage pointer to the opened storage object.</returns>
        IStorage OpenStorage(
            [In, MarshalAs(UnmanagedType.BStr)] string pwcsName,
            [In] IntPtr pstgPriority,
            [In] uint grfMode, [In] IntPtr snbExclude, [In] uint reserved);

        /// <summary>Copies the entire contents of an open storage object to another storage object</summary>
        /// <param name="ciidExclude">The number of elements in the array pointed to by rgiidExclude. </param>
        /// <param name="rgiidExclude">
        /// An array of interface identifiers (IIDs) that either the caller knows about and does not want 
        /// copied or that the storage object does not support but whose state the caller will later explicitly copy.
        /// </param>
        /// <param name="snbExclude">
        /// A string name block (refer to SNB) that specifies a block of storage or stream objects that 
        /// are not to be copied to the destination.
        /// </param>
        /// <param name="pstgDest">
        /// Pointer to the open storage object into which this storage object is to be copied. 
        /// The destination storage object can be a different implementation of the IStorage interface from the source storage object. 
        /// </param>
        void CopyTo(int ciidExclude,
            [In, MarshalAs(UnmanagedType.LPArray)] Guid[] rgiidExclude,
            [In] IntPtr snbExclude,
            [In, MarshalAs(UnmanagedType.Interface)] IStorage pstgDest);

        /// <summary>Copies or moves a substorage or stream from this storage object to another storage object.</summary>
        /// <param name="pwcsName">
        /// Pointer to a wide character null-terminated Unicode string that contains the name of the element 
        /// in this storage object to be moved or copied.
        /// </param>
        /// <param name="pstgDest">IStorage pointer to the destination storage object.</param>
        /// <param name="pwcsNewName">Pointer to a wide character null-terminated unicode string that contains the new name for the element in its new storage object.</param>
        /// <param name="grfFlags">Specifies whether the operation should be a move (STGMOVE_MOVE) or a copy (STGMOVE_COPY).</param>
        void MoveElementTo([In, MarshalAs(UnmanagedType.BStr)] string pwcsName,
            [In, MarshalAs(UnmanagedType.Interface)] IStorage pstgDest,
            [In, MarshalAs(UnmanagedType.BStr)] string pwcsNewName,
            [In] int grfFlags);

        /// <summary>Ensures that any changes made to a storage object open in transacted mode are reflected in the parent storage.</summary>
        /// <param name="grfCommitFlags">Controls how the changes are committed to the storage object.</param>
        void Commit([In] uint grfCommitFlags);

        /// <summary>Discards all changes that have been made to the storage object since the last commit operation.</summary>
        void Revert();

        /// <summary>
        /// Retrieves a pointer to an enumerator object that can be used to enumerate the storage and stream 
        /// objects contained within this storage object.
        /// </summary>
        /// <param name="reserved1">Reserved for future use; must be zero.</param>
        /// <param name="reserved2">Reserved for future use; must be NULL.</param>
        /// <param name="reserved3">Reserved for future use; must be zero.</param>
        /// <returns>Pointer to IEnumSTATSTG* pointer variable that receives the interface pointer to the new enumerator object.</returns>
        [return: MarshalAs(UnmanagedType.Interface)]
        object EnumElements([In] uint reserved1, [In] IntPtr reserved2, [In] uint reserved3);

        /// <summary>Removes the specified storage or stream from this storage object.</summary>
        /// <param name="pwcsName">Pointer to a wide character null-terminated Unicode string that contains the name of the storage or stream to be removed.</param>
        void DestroyElement([In, MarshalAs(UnmanagedType.BStr)] string pwcsName);

        /// <summary>Renames the specified substorage or stream in this storage object.</summary>
        /// <param name="pwcsOldName">Pointer to a wide character null-terminated Unicode string that contains the name of the substorage or stream to be changed.</param>
        /// <param name="pwcsNewName">Pointer to a wide character null-terminated unicode string that contains the new name for the specified substorage or stream.</param>
        void RenameElement([In, MarshalAs(UnmanagedType.BStr)] string pwcsOldName,
            [In, MarshalAs(UnmanagedType.BStr)] string pwcsNewName);

        /// <summary>
        /// Sets the modification, access, and creation times of the specified storage element, if the underlying file system supports this method.
        /// </summary>
        /// <param name="pwcsName">The name of the storage object element whose times are to be modified.</param>
        /// <param name="pctime">Either the new creation time for the element or NULL if the creation time is not to be modified.</param>
        /// <param name="patime">Either the new access time for the element or NULL if the access time is not to be modified.</param>
        /// <param name="pmtime"> Either the new modification time for the element or NULL if the modification time is not to be modified.</param>
        void SetElementTimes([In, MarshalAs(UnmanagedType.BStr)] string pwcsName,
            [In] ulong pctime, [In] ulong patime, [In] ulong pmtime);

        /// <summary>Assigns the specified class identifier (CLSID) to this storage object.</summary>
        /// <param name="clsid">The CLSID that is to be associated with the storage object.</param>
        void SetClass([In] ref Guid clsid);

        /// <summary>Stores up to 32 bits of state information in this storage object. This method is reserved for future use.</summary>
        /// <param name="grfStateBits">
        /// Specifies the new values of the bits to set. No legal values are defined for these bits; they are all reserved for future use and must not be used by applications. 
        /// </param>
        /// <param name="grfMask">A binary mask indicating which bits in grfStateBits are significant in this call.</param>
        void SetStateBits([In] uint grfStateBits, [In] uint grfMask);

        /// <summary>Retrieves the STATSTG structure for this open storage object.</summary>
        /// <param name="pStatStg">
        /// On return, pointer to a STATSTG structure where this method places information about the open storage object. This parameter is NULL if an error occurs.
        /// </param>
        /// <param name="grfStatFlag">
        /// Specifies that some of the members in the STATSTG structure are not returned, thus saving a memory allocation operation. Values are taken from the STATFLAG enumeration. 
        /// </param>
        void Stat([Out] out System.Runtime.InteropServices.ComTypes.STATSTG pStatStg, [In] uint grfStatFlag);
    }
}
