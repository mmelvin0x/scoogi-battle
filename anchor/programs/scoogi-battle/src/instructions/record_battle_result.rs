use anchor_lang::prelude::*;
use anchor_spl::{
    associated_token::AssociatedToken,
    token_2022 as token,
    token_interface::{Mint, TokenAccount, TokenInterface, TransferChecked},
};

use crate::{constants, Admin, Battle, BattleStatus, ScoogiBattleError};

#[derive(Accounts)]
#[instruction(battle_id: u64)]
pub struct RecordBattleResult<'info> {
    #[account(mut, signer)]
    winner: Signer<'info>,

    /// CHECK: passed in here for use in the seeds
    pub player_one: AccountInfo<'info>,

    /// CHECK: passed in here for use in the seeds
    pub player_two: AccountInfo<'info>,

    #[account(seeds = [constants::ADMIN_SEED], bump, has_one = mint)]
    pub admin_account: Account<'info, Admin>,

    #[account(
        mut,
        seeds = [constants::BATTLE_SEED, player_one.key().as_ref(), player_two.key().as_ref(), &battle_id.to_le_bytes()[..]],
        bump,
        has_one = player_one,
        has_one = player_two
    )]
    pub battle_account: Account<'info, Battle>,

    #[account(
        mut,
        seeds = [constants::TOKEN_ACCOUNT_SEED, player_one.key().as_ref(), &battle_id.to_le_bytes()[..]],
        bump,
        token::mint = mint,
        token::authority = battle_token_account,
        token::token_program = token_program
    )]
    pub battle_token_account: Box<InterfaceAccount<'info, TokenAccount>>,

    #[account(
        init_if_needed,
        payer = winner,
        associated_token::mint = mint,
        associated_token::authority = winner,
        associated_token::token_program = token_program
    )]
    pub winner_token_account: Box<InterfaceAccount<'info, TokenAccount>>,

    #[account(constraint = mint.key() == admin_account.mint)]
    pub mint: Box<InterfaceAccount<'info, Mint>>,

    pub system_program: Program<'info, System>,
    pub associated_token_program: Program<'info, AssociatedToken>,
    pub token_program: Interface<'info, TokenInterface>,
}

pub fn record_battle_result_ix(
    ctx: Context<RecordBattleResult>,
    battle_result: u8,
    battle_id: u64,
) -> Result<()> {
    if ctx.accounts.battle_account.battle_id != battle_id {
        return Err(ScoogiBattleError::InvalidBattleId.into());
    }

    match ctx.accounts.battle_account.battle_status {
        BattleStatus::InProgress => {
            ctx.accounts.battle_account.player_two = ctx.accounts.player_two.key();
            ctx.accounts.battle_account.battle_status = BattleStatus::Completed;

            let bump = ctx.bumps.battle_token_account;
            let signer_seeds: &[&[&[u8]]] = &[&[
                constants::TOKEN_ACCOUNT_SEED,
                &battle_id.to_le_bytes(),
                &[bump],
            ]];

            let cpi_program = ctx.accounts.token_program.to_account_info();
            let cpi_accounts = TransferChecked {
                from: ctx.accounts.battle_token_account.to_account_info(),
                mint: ctx.accounts.mint.to_account_info(),
                to: ctx.accounts.winner_token_account.to_account_info(),
                authority: ctx.accounts.player_one.to_account_info(),
            };
            let cpi_ctx = CpiContext::new_with_signer(cpi_program, cpi_accounts, signer_seeds);

            token::transfer_checked(
                cpi_ctx,
                ctx.accounts.admin_account.battle_price,
                ctx.accounts.mint.decimals,
            )?;
        }
        _ => return Err(ScoogiBattleError::InvalidBattleStatus.into()),
    }

    ctx.accounts.battle_account.winner = match battle_result {
        0 => ctx.accounts.battle_account.player_one,
        1 => ctx.accounts.battle_account.player_two,
        _ => return Err(ScoogiBattleError::InvalidBattleResult.into()),
    };

    Ok(())
}
