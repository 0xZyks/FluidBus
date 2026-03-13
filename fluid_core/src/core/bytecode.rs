use std::collections::hash_map::DefaultHasher;
use std::hash::{Hash, Hasher};

// Section IDs
const SECTION_OPCODE: u8 = 0x02;
const SECTION_TYPE: u8 = 0x03;
const SECTION_METHOD: u8 = 0x04;
const SECTION_ARGS: u8 = 0x05;

// --- Signatures ---

fn make_valid_sig(seed: u64, section_id: u8) -> [u8; 2] {
    let target_xor = ((seed & 0xFF) as u8).wrapping_add(section_id);
    // target_sum derive de target_xor pour garantir une solution
    // a=0, b=target_xor satisfait toujours XOR
    // donc target_sum = 0 + target_xor = target_xor
    // Mais on veut que ca depende aussi du seed donc :
    let a = ((seed >> 16) & 0xFF) as u8;
    let b = a ^ target_xor;
    [a, b]
}

pub fn is_valid_sig(sig: [u8; 2], seed: u64, section_id: u8) -> bool {
    let target_xor = ((seed & 0xFF) as u8).wrapping_add(section_id);
    let a = ((seed >> 16) & 0xFF) as u8;
    let expected_b = a ^ target_xor;
    sig[0] == a && sig[1] == expected_b
}

// --- Junk ---

fn junk_len(seed: u64, position: usize) -> usize {
    let mut hasher = DefaultHasher::new();
    seed.hash(&mut hasher);
    position.hash(&mut hasher);
    let hash = hasher.finish();
    // Entre 4 et 67 bytes de junk
    4 + (hash % 64) as usize
}

fn generate_junk(seed: u64, position: usize) -> Vec<u8> {
    let len = junk_len(seed, position);
    let mut junk = Vec::with_capacity(len);
    for i in 0..len {
        let mut hasher = DefaultHasher::new();
        seed.hash(&mut hasher);
        position.hash(&mut hasher);
        i.hash(&mut hasher);
        let hash = hasher.finish();
        junk.push((hash & 0xFF) as u8);
    }
    junk
}

// --- Sections ---

fn make_section(seed: u64, section_id: u8, data: &[u8]) -> Vec<u8> {
    let sig = make_valid_sig(seed, section_id);
    let mut section = Vec::new();
    section.extend_from_slice(&sig);
    section.push(data.len() as u8);
    section.extend_from_slice(data);
    section
}

// --- Generate ---

pub fn generate_bytecode(
    seed: u64,
    opcode: u8,
    type_name: &str,
    method_name: &str,
    arg_type: &str,
    args: &[&[u8]],
) -> Vec<u8> {
    // Construire les 4 sections
    let section_opcode = make_section(seed, SECTION_OPCODE, &[opcode]);
    let section_type = make_section(seed, SECTION_TYPE, type_name.as_bytes());
    let section_method = make_section(seed, SECTION_METHOD, method_name.as_bytes());

    let mut args_data = Vec::new();
    args_data.push(arg_type.len() as u8);
    args_data.extend_from_slice(arg_type.as_bytes());
    args_data.push(args.len() as u8); // nb_args
    for arg in args {
        args_data.push(arg.len() as u8);
        args_data.extend_from_slice(arg);
    }
    let section_args = make_section(seed, SECTION_ARGS, &args_data);

    // Ordre aleatoire des sections via seed
    let mut sections: Vec<Vec<u8>> = vec![
        section_opcode,
        section_type,
        section_method,
        section_args,
    ];
    shuffle_sections(&mut sections, seed);

    // Assembler avec junk intercale
    let mut buf = Vec::new();
    let mut junk_counter = 0usize;

    // Header fixe (position connue)
    let sig_header = make_valid_sig(seed, 0x01);
    buf.extend_from_slice(&sig_header);
    buf.push(0x04); // nb sections

    for section in &sections {
        // Junk avant chaque section
        let junk = generate_junk(seed, junk_counter);
        buf.extend_from_slice(&junk);
        junk_counter += 1;

        buf.extend_from_slice(section);
    }

    // Junk final
    let junk = generate_junk(seed, junk_counter);
    buf.extend_from_slice(&junk);

    buf
}

fn shuffle_sections(sections: &mut Vec<Vec<u8>>, seed: u64) {
    // Fisher-Yates avec seed deterministe
    let len = sections.len();
    for i in (1..len).rev() {
        let mut hasher = DefaultHasher::new();
        seed.hash(&mut hasher);
        i.hash(&mut hasher);
        let hash = hasher.finish();
        let j = (hash % (i + 1) as u64) as usize;
        sections.swap(i, j);
    }
}

// --- Parser ---

#[derive(Debug)]
pub struct ParsedBytecode {
    pub opcode: u8,
    pub type_name: String,
    pub method_name: String,
    pub arg_type: String,
    pub args: Vec<Vec<u8>>,
}

pub fn parse_bytecode(data: &[u8], seed: u64) -> Option<ParsedBytecode> {
    let mut opcode: Option<u8> = None;
    let mut type_name: Option<String> = None;
    let mut method_name: Option<String> = None;
    let mut arg_type: Option<String> = None;
    let mut arg: Option<Vec<Vec<u8>>> = None;

    let mut i = 0usize;

    if data.len() < 3 {
        return None;
    }
    i += 3; // skip header

    while i + 1 < data.len() {
        let sig = [data[i], data[i + 1]];
        let mut matched = false;

        for section_id in [SECTION_OPCODE, SECTION_TYPE, SECTION_METHOD, SECTION_ARGS] {
            if is_valid_sig(sig, seed, section_id) {
                i += 2;
                if i >= data.len() { return None; }
                let len = data[i] as usize;
                i += 1;
                if i + len > data.len() {
                    return None;
                }
                let payload = &data[i..i + len];

                match section_id {
                    SECTION_OPCODE => {
                        if payload.len() >= 1 {
                            opcode = Some(payload[0]);
                        }
                    }
                    SECTION_TYPE => {
                        type_name = Some(String::from_utf8_lossy(payload).to_string());
                    }
                    SECTION_METHOD => {
                        method_name = Some(String::from_utf8_lossy(payload).to_string());
                    }
                    SECTION_ARGS => {
                        if payload.len() < 2 {
                            return None;
                        }
                        let at_len = payload[0] as usize;
                        if 1 + at_len + 1 > payload.len() {
                            return None;
                        }
                        let at = String::from_utf8_lossy(&payload[1..1 + at_len]).to_string();

                        let mut cursor = 1 + at_len;
                        let nb_args = payload[cursor] as usize;
                        cursor += 1;

                        let mut parsed_args: Vec<Vec<u8>> = Vec::new();
                        for _ in 0..nb_args {
                            if cursor >= payload.len() { return None; }
                            let a_len = payload[cursor] as usize;
                            cursor += 1;
                            if cursor + a_len > payload.len() { return None; }
                            parsed_args.push(payload[cursor..cursor + a_len].to_vec());
                            cursor += a_len;
                        }

                        arg_type = Some(at);
                        arg = Some(parsed_args);
                    }
                    _ => {}
                }

                i += len;
                matched = true;
                break;
            }
        }

        if !matched {
            i += 1;
        }
    }

    Some(ParsedBytecode {
        opcode: opcode?,
        type_name: type_name?,
        method_name: method_name?,
        arg_type: arg_type?,
        args: arg?,
    })
}
