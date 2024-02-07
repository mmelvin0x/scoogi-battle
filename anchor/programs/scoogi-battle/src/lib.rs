use anchor_lang::prelude::*;

mod constants;
mod errors;
mod instructions;
mod state;

pub use constants::*;
pub use errors::*;
pub use instructions::*;
pub use state::*;

declare_id!("GM9mV2Km6iwaQsVjs5x4wb4CL2XBemS3cgZ28sCydiBR");

#[program]
pub mod scoogi_battle {
    use super::*;

    pub fn initialize(
        ctx: Context<Initialize>,
        burn_fee_bps: u64,
        battle_price: u64,
    ) -> Result<()> {
        instructions::initialize_ix(ctx, burn_fee_bps, battle_price)
    }

    pub fn update_burn_fee_bps(ctx: Context<UpdateBurnFeeBps>, burn_fee_bps: u64) -> Result<()> {
        instructions::update_burn_fee_bps_ix(ctx, burn_fee_bps)
    }

    pub fn update_battle_price(ctx: Context<UpdateBattlePrice>, battle_price: u64) -> Result<()> {
        instructions::update_battle_price_ix(ctx, battle_price)
    }

    pub fn update_mint(ctx: Context<UpdateMint>) -> Result<()> {
        instructions::update_mint_ix(ctx)
    }

    pub fn admin_withdrawal(ctx: Context<AdminWithdraw>, battle_id: u64) -> Result<()> {
        instructions::admin_withdraw_ix(ctx, battle_id)
    }

    pub fn create_battle(ctx: Context<CreateBattle>, battle_id: u64) -> Result<()> {
        instructions::create_battle_ix(ctx, battle_id)
    }

    pub fn join_battle(ctx: Context<JoinQuickBattle>, battle_id: u64) -> Result<()> {
        instructions::join_battle_ix(ctx, battle_id)
    }

    pub fn record_battle_result(
        ctx: Context<RecordBattleResult>,
        battle_result: u8,
        battle_id: u64,
    ) -> Result<()> {
        instructions::record_battle_result_ix(ctx, battle_result, battle_id)
    }

    pub fn withdraw_from_battle(ctx: Context<WithdrawFromBattle>, battle_id: u64) -> Result<()> {
        instructions::withdraw_from_battle_ix(ctx, battle_id)
    }
}
