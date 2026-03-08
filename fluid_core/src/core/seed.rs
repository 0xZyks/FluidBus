use std::time::{SystemTime, UNIX_EPOCH};

pub fn generate_seed() -> u64 {
    let time = SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .unwrap()
        .as_nanos() as u64;
    let pid = std::process::id() as u64;
    time ^ pid
}
