mod core;

use core::bytecode::generate_bytecode;
use core::ffi::{free_vec, vec_into_raw};
use core::state;

// --- FFI Exports ---

#[unsafe(no_mangle)]
pub unsafe extern "C" fn init() -> u64 {
    state::init()
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn get_parsed_bytecode_by_token(
    token: *const u8,
    token_len: usize,
    arg: *const u8,
    arg_len: usize,
    out_len: *mut usize,
) -> *mut u8 {
    let token_slice = unsafe { std::slice::from_raw_parts(token, token_len) };
    let arg_slice = unsafe { raw_to_slice(arg, arg_len) };

    match state::resolve_by_token(token_slice, arg_slice) {
        Some(result) => unsafe { vec_into_raw(result, out_len) },
        None => std::ptr::null_mut(),
    }
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn get_parsed_bytecode(
    opcode: u8,
    arg: *const u8,
    arg_len: usize,
    out_len: *mut usize,
) -> *mut u8 {
    let arg_slice = unsafe { raw_to_slice(arg, arg_len) };

    match state::resolve_by_opcode(opcode, arg_slice) {
        Some(result) => unsafe { vec_into_raw(result, out_len) },
        None => std::ptr::null_mut(),
    }
}

/// Legacy debug : echo les bytes recus. Ne passe pas par le state.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn process_bytes(
    data: *const u8,
    len: usize,
    out_len: *mut usize,
) -> *mut u8 {
    unsafe {
        let input = std::slice::from_raw_parts(data, len);
        println!("Rust Received {:?}", input);
    }
    let bytes = "Received OpCode, sending Token".as_bytes().to_vec();
    unsafe { vec_into_raw(bytes, out_len) }
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn free_bytes(ptr: *mut u8, len: usize) {
    unsafe { free_vec(ptr, len) }
}

/// Retourne le token courant pour un opcode depuis le state.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn get_token(opcode: u8, out_len: *mut usize) -> *mut u8 {
    match state::current_token(opcode) {
        Some(token) => unsafe { vec_into_raw(token, out_len) },
        None => std::ptr::null_mut(),
    }
}

/// Rotate le token dans le state (met a jour TOKENS, BYTECODES, TOKEN_INDEX).
#[unsafe(no_mangle)]
pub unsafe extern "C" fn rotate_token(
    data: *const u8,
    len: usize,
    out_len: *mut usize,
) -> *mut u8 {
    let current = unsafe { std::slice::from_raw_parts(data, len) };
    match state::rotate(current) {
        Some(next) => unsafe { vec_into_raw(next, out_len) },
        None => std::ptr::null_mut(),
    }
}

/// Legacy test : genere un bytecode brut sans passer par le state.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn get_bytecode(
    opcode: u8,
    type_name: *const u8,
    type_name_len: usize,
    method_name: *const u8,
    method_name_len: usize,
    arg_type: *const u8,
    arg_type_len: usize,
    arg: *const u8,
    arg_len: usize,
    out_len: *mut usize,
) -> *mut u8 {
    let seed = state::get_seed();
    let type_name = unsafe {
        std::str::from_utf8(std::slice::from_raw_parts(type_name, type_name_len)).unwrap()
    };
    let method_name = unsafe {
        std::str::from_utf8(std::slice::from_raw_parts(method_name, method_name_len)).unwrap()
    };
    let arg_type = unsafe {
        std::str::from_utf8(std::slice::from_raw_parts(arg_type, arg_type_len)).unwrap()
    };
    let arg = unsafe { std::slice::from_raw_parts(arg, arg_len) };

    let bytecode = generate_bytecode(seed, opcode, type_name, method_name, arg_type, arg);
    unsafe { vec_into_raw(bytecode, out_len) }
}

// --- Helpers ---

/// # Safety
/// `ptr` must be valid for `len` bytes when `len > 0`.
unsafe fn raw_to_slice<'a>(ptr: *const u8, len: usize) -> &'a [u8] {
    if len > 0 {
        unsafe { std::slice::from_raw_parts(ptr, len) }
    } else {
        &[]
    }
}
