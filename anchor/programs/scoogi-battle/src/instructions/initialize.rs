use anchor_lang::prelude::*;
use anchor_spl::token_interface::Mint;

use crate::{constants, Admin};

#[derive(Accounts)]

pub struct Initialize<'info> {
    #[account(mut, signer)]
    pub admin: Signer<'info>,

    #[account(
        init,
        payer = admin,
        space = Admin::size(),
        seeds = [constants::ADMIN_SEED],
        bump
    )]
    pub admin_account: Account<'info, Admin>,

    pub mint: InterfaceAccount<'info, Mint>,

    pub system_program: Program<'info, System>,
}

pub fn initialize_ix(ctx: Context<Initialize>, burn_fee_bps: u64, battle_price: u64) -> Result<()> {
    ctx.accounts.admin_account.battle_price = battle_price
        .checked_mul((10u64).pow(ctx.accounts.mint.decimals as u32))
        .unwrap();
    ctx.accounts.admin_account.burn_fee_bps = burn_fee_bps;
    ctx.accounts.admin_account.admin = ctx.accounts.admin.key();
    ctx.accounts.admin_account.mint = ctx.accounts.mint.key();

    Ok(())
}
