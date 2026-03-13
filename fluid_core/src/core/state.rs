use std::collections::HashMap;
use std::sync::atomic::{AtomicU64, Ordering};
use std::sync::Mutex;

use super::bytecode::{generate_bytecode, parse_bytecode, ParsedBytecode};
use super::seed::generate_seed;
use super::token::{generate_token, next_token};
use super::xor::xor_bytes;

use crate::generated::methods::{METHODS, SEED_BUILD};

// --- Global state ---

static SEED_RUNTIME: AtomicU64 = AtomicU64::new(0);
static TOKENS: Mutex<Option<HashMap<u8, Vec<u8>>>> = Mutex::new(None);
static BYTECODES: Mutex<Option<HashMap<u8, Vec<u8>>>> = Mutex::new(None);
// HashMap inversee : token -> opcode
static TOKEN_INDEX: Mutex<Option<HashMap<Vec<u8>, u8>>> = Mutex::new(None);

// --- Public API ---

pub fn get_seed_runtime() -> u64 {
    SEED_RUNTIME.load(Ordering::SeqCst)
}

pub fn init() -> u64 {
    let seed_runtime = generate_seed();
    SEED_RUNTIME.store(seed_runtime, Ordering::SeqCst);

    let mut tokens = TOKENS.lock().unwrap();
    let mut bytecodes = BYTECODES.lock().unwrap();
    let mut token_index = TOKEN_INDEX.lock().unwrap();

    let mut token_map: HashMap<u8, Vec<u8>> = HashMap::new();
    let mut bytecode_map: HashMap<u8, Vec<u8>> = HashMap::new();
    let mut index_map: HashMap<Vec<u8>, u8> = HashMap::new();

    for &(opcode, type_name, method_name, arg_type) in METHODS {
        let token_v1 = generate_token(opcode, seed_runtime);
        let bytecode_clear = generate_bytecode(SEED_BUILD, opcode, type_name, method_name, arg_type, &[]);
        let bytecode_xor = xor_bytes(&bytecode_clear, &token_v1);

        index_map.insert(token_v1.clone(), opcode);
        token_map.insert(opcode, token_v1);
        bytecode_map.insert(opcode, bytecode_xor);
    }

    *tokens = Some(token_map);
    *bytecodes = Some(bytecode_map);
    *token_index = Some(index_map);

    seed_runtime
}

/// Resolve un opcode depuis un token, dechiffre/parse le bytecode,
/// injecte l'arg, rotate le token, et retourne le resultat serialise.
pub fn resolve_by_token(token_slice: &[u8], args: &[&[u8]]) -> Option<Vec<u8>> {
    let seed_runtime = get_seed_runtime();

    let mut tokens = TOKENS.lock().ok()?;
    let mut bytecodes = BYTECODES.lock().ok()?;
    let mut token_index = TOKEN_INDEX.lock().ok()?;

    let index_map = token_index.as_mut()?;
    let token_map = tokens.as_mut()?;
    let bytecode_map = bytecodes.as_mut()?;

    let opcode = *index_map.get(token_slice)?;

    let (parsed, next) = decrypt_parse_rotate(opcode, token_map, bytecode_map, args, seed_runtime)?;

    // Mettre a jour le token index
    index_map.remove(token_slice);
    index_map.insert(next.clone(), opcode);

    Some(serialize_result(&parsed, &next))
}

/// Meme logique que `resolve_by_token` mais avec un opcode direct.
pub fn resolve_by_opcode(opcode: u8, args: &[&[u8]]) -> Option<Vec<u8>> {
    let seed_runtime = get_seed_runtime();

    let mut tokens = TOKENS.lock().ok()?;
    let mut bytecodes = BYTECODES.lock().ok()?;

    let token_map = tokens.as_mut()?;
    let bytecode_map = bytecodes.as_mut()?;

    let (parsed, next) = decrypt_parse_rotate(opcode, token_map, bytecode_map, args, seed_runtime)?;

    Some(serialize_result(&parsed, &next))
}

/// Retourne le token courant pour un opcode depuis TOKENS.
pub fn current_token(opcode: u8) -> Option<Vec<u8>> {
    let tokens = TOKENS.lock().ok()?;
    tokens.as_ref()?.get(&opcode).cloned()
}

/// Rotate le token associe a `current` dans le state.
/// Met a jour TOKENS, BYTECODES et TOKEN_INDEX de facon coherente.
pub fn rotate(current: &[u8]) -> Option<Vec<u8>> {
    let seed_runtime = get_seed_runtime();

    let mut tokens = TOKENS.lock().ok()?;
    let mut bytecodes = BYTECODES.lock().ok()?;
    let mut token_index = TOKEN_INDEX.lock().ok()?;

    let index_map = token_index.as_mut()?;
    let token_map = tokens.as_mut()?;
    let bytecode_map = bytecodes.as_mut()?;

    let opcode = *index_map.get(current)?;
    let bytecode_xor = bytecode_map.get(&opcode)?.clone();

    // Dechiffrer avec le token courant
    let bytecode_clear = xor_bytes(&bytecode_xor, current);

    // Generer le prochain token (runtime seed)
    let next = next_token(current, seed_runtime);

    // Re-chiffrer avec le nouveau token
    let bytecode_new = xor_bytes(&bytecode_clear, &next);

    // Mettre a jour les 3 maps
    index_map.remove(current);
    index_map.insert(next.clone(), opcode);
    token_map.insert(opcode, next.clone());
    bytecode_map.insert(opcode, bytecode_new);

    Some(next)
}

// --- Core interne ---

/// Dechiffre le bytecode, le parse, injecte l'arg, rotate le token,
/// re-chiffre avec le nouveau token, et met a jour les maps.
fn decrypt_parse_rotate(
    opcode: u8,
    token_map: &mut HashMap<u8, Vec<u8>>,
    bytecode_map: &mut HashMap<u8, Vec<u8>>,
    args: &[&[u8]],
    seed_runtime: u64,
) -> Option<(ParsedBytecode, Vec<u8>)> {
    let token = token_map.get(&opcode)?.clone();
    let bytecode_xor = bytecode_map.get(&opcode)?.clone();

    let bytecode_clear = xor_bytes(&bytecode_xor, &token);
    let mut parsed = parse_bytecode(&bytecode_clear, SEED_BUILD)?;

    if !args.is_empty() {
        parsed.args = args.iter().map(|a| a.to_vec()).collect();
    }

    let next = next_token(&token, seed_runtime);
    let bytecode_new = xor_bytes(&bytecode_clear, &next);

    token_map.insert(opcode, next.clone());
    bytecode_map.insert(opcode, bytecode_new);

    Some((parsed, next))
}

// --- Serialisation ---

/// Serialise : [next_token_len][next_token][type_len][type][method_len][method][arg_type_len][arg_type][arg_len][arg]
fn serialize_result(parsed: &ParsedBytecode, next_token: &[u8]) -> Vec<u8> {
    let mut buf = Vec::new();
    push_prefixed(&mut buf, next_token);
    push_prefixed(&mut buf, parsed.type_name.as_bytes());
    push_prefixed(&mut buf, parsed.method_name.as_bytes());
    push_prefixed(&mut buf, parsed.arg_type.as_bytes());
    buf.push(parsed.args.len() as u8); // nb_args
    for arg in &parsed.args {
        push_prefixed(&mut buf, arg);
    }
    buf
}

fn push_prefixed(buf: &mut Vec<u8>, data: &[u8]) {
    buf.push(data.len() as u8);
    buf.extend_from_slice(data);
}
