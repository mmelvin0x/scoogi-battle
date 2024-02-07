use anchor_lang::prelude::*;

#[derive(AnchorSerialize, AnchorDeserialize, Clone)]
pub enum BattleStatus {
    Pending,
    InProgress,
    Completed,
}

#[account]
pub struct Battle {
    pub battle_id: u64,
    pub player_one: Pubkey,
    pub player_two: Pubkey,
    pub winner: Pubkey,
    pub battle_status: BattleStatus,
}

impl Battle {
    pub fn size() -> usize {
        8 + 8 + 32 + 32 + 32 + 1
    }
}
