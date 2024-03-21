use anchor_lang::prelude::*;
use anchor_spl::{
    associated_token::AssociatedToken,
    token_2022 as token,
    token_interface::{CloseAccount, Mint, TokenAccount, TokenInterface, TransferChecked},
};

use crate::{constants, Admin, Battle, BattleStatus, ScoogiBattleError};

#[derive(Accounts)]
#[instruction(battle_result: u8, battle_id: u64)]
pub struct RecordBattleResult<'info> {
    #[account(mut, signer)]
    winner: Signer<'info>,

    /// CHECK: passed in here for use in the seeds
    pub player_one: AccountInfo<'info>,

    /// CHECK: passed in here for use in the seeds
    pub player_two: AccountInfo<'info>,

    /// CHECK: passed in here for use in the seeds
    pub admin: AccountInfo<'info>,

    #[account(seeds = [constants::ADMIN_SEED], bump, has_one = mint)]
    pub admin_account: Account<'info, Admin>,

    #[account(
        init_if_needed,
        payer = winner,
        associated_token::mint = mint,
        associated_token::authority = admin,
        associated_token::token_program = token_program
    )]
    pub admin_token_account: Box<InterfaceAccount<'info, TokenAccount>>,

    #[account(
        mut,
        close = admin,
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
            ctx.accounts.battle_account.battle_status = BattleStatus::Completed;

            ctx.accounts.battle_account.winner = match battle_result {
                0 => {
                    if ctx.accounts.winner.key() != ctx.accounts.battle_account.player_one {
                        return Err(ScoogiBattleError::Unauthorized.into());
                    }

                    ctx.accounts.battle_account.player_one
                }
                1 => {
                    if ctx.accounts.winner.key() != ctx.accounts.battle_account.player_two {
                        return Err(ScoogiBattleError::Unauthorized.into());
                    }

                    ctx.accounts.battle_account.player_two
                }
                _ => return Err(ScoogiBattleError::InvalidBattleResult.into()),
            };

            let player_one_key = ctx.accounts.player_one.key();
            let burn_amount = ctx
                .accounts
                .admin_account
                .burn_fee_bps
                .checked_mul(
                    ctx.accounts
                        .admin_account
                        .battle_price
                        .checked_mul(2)
                        .unwrap(),
                )
                .unwrap()
                .checked_div(10_000)
                .unwrap();

            let winner_amount = ctx
                .accounts
                .battle_token_account
                .amount
                .checked_sub(burn_amount)
                .unwrap();

            let bump = ctx.bumps.battle_token_account;
            let signer_seeds: &[&[&[u8]]] = &[&[
                constants::TOKEN_ACCOUNT_SEED,
                player_one_key.as_ref(),
                &[bump],
            ]];

            // Send to winner
            let cpi_program = ctx.accounts.token_program.to_account_info();
            let cpi_accounts = TransferChecked {
                from: ctx.accounts.battle_token_account.to_account_info(),
                mint: ctx.accounts.mint.to_account_info(),
                to: ctx.accounts.winner_token_account.to_account_info(),
                authority: ctx.accounts.battle_token_account.to_account_info(),
            };
            let cpi_ctx = CpiContext::new_with_signer(cpi_program, cpi_accounts, signer_seeds);

            token::transfer_checked(cpi_ctx, winner_amount, ctx.accounts.mint.decimals)?;

            // Send to admin account so tokens can be burned
            let cpi_program = ctx.accounts.token_program.to_account_info();

            let cpi_accounts = TransferChecked {
                from: ctx.accounts.battle_token_account.to_account_info(),
                mint: ctx.accounts.mint.to_account_info(),
                to: ctx.accounts.admin_token_account.to_account_info(),
                authority: ctx.accounts.battle_token_account.to_account_info(),
            };
            let cpi_ctx = CpiContext::new_with_signer(cpi_program, cpi_accounts, signer_seeds);

            token::transfer_checked(cpi_ctx, burn_amount, ctx.accounts.mint.decimals)?;

            // Close the battle token account
            let cpi_program = ctx.accounts.token_program.to_account_info();
            let cpi_accounts = CloseAccount {
                account: ctx.accounts.battle_token_account.to_account_info(),
                destination: ctx.accounts.admin.to_account_info(),
                authority: ctx.accounts.battle_token_account.to_account_info(),
            };
            let cpi_ctx = CpiContext::new_with_signer(cpi_program, cpi_accounts, signer_seeds);

            token::close_account(cpi_ctx)?;
        }
        _ => return Err(ScoogiBattleError::InvalidBattleStatus.into()),
    }

    Ok(())
}
