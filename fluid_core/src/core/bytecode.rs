use std::collections::hash_map::DefaultHasher;
use std::hash::{Hash, Hasher};

fn make_sig(seed: u64, section_id: u8) -> [u8; 2] {
    let mut hasher = DefaultHasher::new();
    seed.hash(&mut hasher);
    section_id.hash(&mut hasher);
    let hash = hasher.finish();
    let bytes = hash.to_le_bytes();
    [bytes[0], bytes[1]]
}

fn sig_header(seed: u64) -> [u8; 2] { make_sig(seed, 0x01) }
fn sig_opcode(seed: u64) -> [u8; 2] { make_sig(seed, 0x02) }
fn sig_type(seed: u64)   -> [u8; 2] { make_sig(seed, 0x03) }
fn sig_method(seed: u64) -> [u8; 2] { make_sig(seed, 0x04) }
fn sig_args(seed: u64)   -> [u8; 2] { make_sig(seed, 0x05) }

pub fn generate_bytecode(
    seed: u64,
    opcode: u8,
    type_name: &str,
    method_name: &str,
    arg_type: &str,
    arg: &[u8],
) -> Vec<u8> {
    let mut buf = Vec::new();

    // Header
    buf.extend_from_slice(&sig_header(seed));
    buf.push(0x04); // nb sections

    // Opcode
    buf.extend_from_slice(&sig_opcode(seed));
    buf.push(0x01); // len
    buf.push(opcode);

    // Type
    let type_bytes = type_name.as_bytes();
    buf.extend_from_slice(&sig_type(seed));
    buf.push(type_bytes.len() as u8);
    buf.extend_from_slice(type_bytes);

    // Method
    let method_bytes = method_name.as_bytes();
    buf.extend_from_slice(&sig_method(seed));
    buf.push(method_bytes.len() as u8);
    buf.extend_from_slice(method_bytes);

    // Args
    let arg_type_bytes = arg_type.as_bytes();
    buf.extend_from_slice(&sig_args(seed));
    buf.push(arg_type_bytes.len() as u8);
    buf.extend_from_slice(arg_type_bytes);
    buf.push(arg.len() as u8);
    buf.extend_from_slice(arg);

    buf
}
