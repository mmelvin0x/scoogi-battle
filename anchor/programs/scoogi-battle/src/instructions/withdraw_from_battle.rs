use anchor_lang::prelude::*;
use anchor_spl::{
    associated_token::AssociatedToken,
    token_2022 as token,
    token_interface::{Mint, TokenAccount, TokenInterface, TransferChecked},
};

use crate::{constants, Admin, Battle, BattleStatus, ScoogiBattleError};

#[derive(Accounts)]
#[instruction(battle_id: u64)]
pub struct WithdrawFromBattle<'info> {
    #[account(mut, signer)]
    withdrawer: Signer<'info>,

    /// CHECK: passed in here for use in the seeds
    pub player_one: AccountInfo<'info>,

    #[account(seeds = [constants::ADMIN_SEED], bump, has_one = mint)]
    pub admin_account: Account<'info, Admin>,

    #[account(
        mut,
        close = player_one,
        seeds = [constants::BATTLE_SEED, player_one.key().as_ref()],
        bump,
        has_one = player_one,
    )]
    pub battle_account: Account<'info, Battle>,

    #[account(
        mut,
        seeds = [constants::TOKEN_ACCOUNT_SEED, player_one.key().as_ref()],
        bump,
        token::mint = mint,
        token::authority = battle_token_account,
        token::token_program = token_program
    )]
    pub battle_token_account: InterfaceAccount<'info, TokenAccount>,

    #[account(
        init_if_needed,
        payer = withdrawer,
        associated_token::mint = mint,
        associated_token::authority = player_one,
        associated_token::token_program = token_program
    )]
    pub player_one_token_account: InterfaceAccount<'info, TokenAccount>,

    #[account(constraint = mint.key() == admin_account.mint)]
    pub mint: InterfaceAccount<'info, Mint>,

    pub system_program: Program<'info, System>,
    pub associated_token_program: Program<'info, AssociatedToken>,
    pub token_program: Interface<'info, TokenInterface>,
}

pub fn withdraw_from_battle_ix(ctx: Context<WithdrawFromBattle>, battle_id: u64) -> Result<()> {
    if ctx.accounts.battle_account.battle_id != battle_id {
        return Err(ScoogiBattleError::InvalidBattleId.into());
    }

    match ctx.accounts.battle_account.battle_status {
        // only player one has joined the battle
        BattleStatus::Pending => {
            if ctx.accounts.withdrawer.key() != ctx.accounts.player_one.key() {
                return Err(ScoogiBattleError::InvalidWithdrawal.into());
            }

            // transfer player one's tokens back to them
            let bump = ctx.bumps.battle_token_account;
            let signer_seeds: &[&[&[u8]]] = &[&[constants::TOKEN_ACCOUNT_SEED, &[bump]]];

            let cpi_program = ctx.accounts.token_program.to_account_info();
            let cpi_accounts = TransferChecked {
                from: ctx.accounts.battle_token_account.to_account_info(),
                mint: ctx.accounts.mint.to_account_info(),
                to: ctx.accounts.player_one_token_account.to_account_info(),
                authority: ctx.accounts.battle_token_account.to_account_info(),
            };
            let cpi_ctx = CpiContext::new_with_signer(cpi_program, cpi_accounts, signer_seeds);

            token::transfer_checked(
                cpi_ctx,
                ctx.accounts.battle_token_account.amount,
                ctx.accounts.mint.decimals,
            )?;
        }
        // both players have joined the battle, requires admin withdrawal
        _ => return Err(ScoogiBattleError::InvalidBattleStatus.into()),
    }

    Ok(())
}
