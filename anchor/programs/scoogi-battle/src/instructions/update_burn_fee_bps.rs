use anchor_lang::prelude::*;

use crate::{constants, Admin};

#[derive(Accounts)]
pub struct UpdateBurnFeeBps<'info> {
    #[account(mut, signer)]
    pub admin: Signer<'info>,

    #[account(
        mut,
        seeds = [constants::ADMIN_SEED],
        bump,
        constraint = admin_account.admin == admin.key(),
        has_one = admin
    )]
    pub admin_account: Account<'info, Admin>,

    pub system_program: Program<'info, System>,
}

pub fn update_burn_fee_bps_ix(ctx: Context<UpdateBurnFeeBps>, burn_fee_bps: u64) -> Result<()> {
    ctx.accounts.admin_account.burn_fee_bps = burn_fee_bps;

    Ok(())
}
