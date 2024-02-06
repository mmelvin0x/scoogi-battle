use anchor_lang::prelude::*;
use anchor_spl::token_interface::Mint;

use crate::{constants, Admin};

#[derive(Accounts)]
pub struct UpdateMint<'info> {
    #[account(mut, signer, constraint = admin.key() == admin_account.admin)]
    pub admin: Signer<'info>,

    #[account(mut, seeds = [constants::ADMIN_SEED], bump, has_one = admin)]
    pub admin_account: Account<'info, Admin>,

    pub mint: InterfaceAccount<'info, Mint>,
}

pub fn update_mint_ix(ctx: Context<UpdateMint>) -> Result<()> {
    ctx.accounts.admin_account.mint = ctx.accounts.mint.key();

    Ok(())
}
