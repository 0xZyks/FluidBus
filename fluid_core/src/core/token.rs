use std::collections::hash_map::DefaultHasher;
use std::hash::{Hash, Hasher};

pub fn generate_token(opcode: u8, seed: u64) -> Vec<u8> {
    let mut hasher = DefaultHasher::new();
    opcode.hash(&mut hasher);
    seed.hash(&mut hasher);
    let hash = hasher.finish();
    hash.to_le_bytes().to_vec()
}

pub fn next_token(current_token: &[u8], seed: u64) -> Vec<u8> {
    let mut hasher = DefaultHasher::new();
    current_token.hash(&mut hasher);
    seed.hash(&mut hasher);
    let hash = hasher.finish();
    hash.to_le_bytes().to_vec()
}
