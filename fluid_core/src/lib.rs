use std::slice;

#[unsafe(no_mangle)]
pub extern "C" fn process_bytes(data: *const u8, len: usize, out_len: *mut usize) -> *mut u8 {
    let text = bytes_to_str(data, len);
    println!("Rust: {}", text);
    str_to_ptr("Received !", out_len)
}

#[unsafe(no_mangle)]
pub extern "C" fn free_bytes(ptr: *mut u8, len: usize) {
    unsafe {
        let _ = Vec::from_raw_parts(ptr, len, len);
    }
    println!("Freed !");
}

fn bytes_to_str(data: *const u8, len: usize) -> &'static str {
    let input = unsafe { slice::from_raw_parts(data, len) };
    std::str::from_utf8(input).unwrap()
}

fn str_to_ptr(s: &str, out_len: *mut usize) -> *mut u8 {
    let mut bytes: Vec<u8> = s.as_bytes().to_vec();
    unsafe { *out_len = bytes.len() };
    let ptr = bytes.as_mut_ptr();
    std::mem::forget(bytes);
    ptr
}
