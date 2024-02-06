use anchor_lang::prelude::*;
use anchor_spl::token_interface::Mint;

use crate::{constants, Admin};

#[derive(Accounts)]
pub struct UpdateBattlePrice<'info> {
    #[account(mut, signer, constraint = admin.key() == admin_account.admin)]
    pub admin: Signer<'info>,

    #[account(mut, seeds = [constants::ADMIN_SEED], bump, has_one = admin, has_one = mint)]
    pub admin_account: Account<'info, Admin>,

    #[account(constraint = mint.key() == admin_account.mint)]
    pub mint: InterfaceAccount<'info, Mint>,
}

pub fn update_battle_price_ix(ctx: Context<UpdateBattlePrice>, battle_price: u64) -> Result<()> {
    ctx.accounts.admin_account.battle_price = battle_price
        .checked_mul((10u64).pow(ctx.accounts.mint.decimals as u32))
        .unwrap();

    Ok(())
}
