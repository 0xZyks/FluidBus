mod core;

use core::seed::generate_seed;
use core::token::{generate_token, next_token};
use core::bytecode::{generate_bytecode};
use std::{slice};
use std::sync::atomic::{AtomicU64, Ordering};


static SEED: AtomicU64 = AtomicU64::new(0);

#[unsafe(no_mangle)]
pub extern "C" fn process_bytes(data: *const u8, len: usize, out_len: *mut usize) -> *mut u8 {
    unsafe {
        let input = slice::from_raw_parts(data, len);
        println!("Rust Received {:?}", input);
    };

    let mut bytes: Vec<u8> = "Received OpCode, sending Token".as_bytes().to_vec();
    unsafe { *out_len = bytes.len(); };
    let ptr = bytes.as_mut_ptr();
    std::mem::forget(bytes);
    ptr
}

#[unsafe(no_mangle)]
pub extern "C" fn free_bytes(ptr: *mut u8, len: usize) {
    unsafe {
        let _ = Vec::from_raw_parts(ptr, len, len);
    }
    println!("Freed !");
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn init() -> u64 {
    let seed = generate_seed();
    SEED.store(seed, Ordering::SeqCst);
    seed
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn get_token(opcode: u8, out_len: *mut usize) -> *mut u8 {
    let seed = SEED.load(Ordering::SeqCst);
    let mut token = generate_token(opcode, seed);
    unsafe {
        *out_len = token.len();
        let ptr = token.as_mut_ptr();
        std::mem::forget(token);
        ptr
    }
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn rotate_token(data: *const u8, len: usize, out_len: *mut usize) -> *mut u8 {
    let seed = SEED.load(Ordering::SeqCst);
    let current = unsafe { std::slice::from_raw_parts(data, len) };
    let mut next = next_token(current, seed);
    unsafe {
        *out_len = next.len();
        let ptr = next.as_mut_ptr();
        std::mem::forget(next);
        ptr
    }
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn get_bytecode(
    opcode: u8,
    type_name: *const u8, type_name_len: usize,
    method_name: *const u8, method_name_len: usize,
    arg_type: *const u8, arg_type_len: usize, arg: *const u8, arg_len: usize,
    out_len: *mut usize) -> *mut u8 {
    let seed = SEED.load(Ordering::SeqCst);

    let type_name = unsafe { std::str::from_utf8(std::slice::from_raw_parts(type_name, type_name_len)).unwrap() };
    let method_name = unsafe { std::str::from_utf8(std::slice::from_raw_parts(method_name, method_name_len)).unwrap() };
    let arg_type = unsafe { std::str::from_utf8(std::slice::from_raw_parts(arg_type, arg_type_len)).unwrap() };
    let arg = unsafe { std::slice::from_raw_parts(arg, arg_len) };

    let mut bytecode = generate_bytecode(seed, opcode, type_name, method_name, arg_type, arg);

    unsafe { *out_len = bytecode.len() };
    let ptr = bytecode.as_mut_ptr();
    std::mem::forget(bytecode);
    ptr
}
/*
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
*/
