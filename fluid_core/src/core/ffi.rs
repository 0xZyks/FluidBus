/// Transfers ownership of a Vec<u8> to C, writing the length to `out_len`.
/// The caller must free the returned pointer with `free_vec`.
///
/// # Safety
/// `out_len` must be a valid, aligned pointer to a `usize`.
pub unsafe fn vec_into_raw(mut bytes: Vec<u8>, out_len: *mut usize) -> *mut u8 {
    unsafe { *out_len = bytes.len() };
    let ptr = bytes.as_mut_ptr();
    std::mem::forget(bytes);
    ptr
}

/// Reclaims a buffer previously returned by `vec_into_raw`.
///
/// # Safety
/// `ptr` must have been returned by `vec_into_raw` with the same `len`.
pub unsafe fn free_vec(ptr: *mut u8, len: usize) {
    unsafe {
        let _ = Vec::from_raw_parts(ptr, len, len);
    }
}
