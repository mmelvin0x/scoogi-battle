use anchor_lang::prelude::*;

#[account]
pub struct Admin {
    pub admin: Pubkey,
    pub mint: Pubkey,
    pub burn_fee_bps: u64,
    pub battle_price: u64,
}

impl Admin {
    pub fn size() -> usize {
        8 + 32 + 32 + 8 + 8
    }
}
