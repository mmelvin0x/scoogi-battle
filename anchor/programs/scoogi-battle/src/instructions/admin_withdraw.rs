use anchor_lang::prelude::*;
use anchor_spl::{
    associated_token::AssociatedToken,
    token_2022 as token,
    token_interface::{CloseAccount, Mint, TokenAccount, TokenInterface, TransferChecked},
};

use crate::{constants, Admin, Battle, ScoogiBattleError};

#[derive(Accounts)]
#[instruction(battle_id: u64)]
pub struct AdminWithdraw<'info> {
    #[account(mut, signer)]
    admin: Signer<'info>,

    /// CHECK: passed in here for use in the seeds
    pub player_one: AccountInfo<'info>,

    /// CHECK: passed in here for use in the seeds
    pub player_two: AccountInfo<'info>,

    #[account(seeds = [constants::ADMIN_SEED], bump, has_one = mint)]
    pub admin_account: Account<'info, Admin>,

    #[account(
        mut,
        close = admin,
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
        payer = admin,
        associated_token::mint = mint,
        associated_token::authority = player_one,
        associated_token::token_program = token_program
    )]
    pub player_one_token_account: Box<InterfaceAccount<'info, TokenAccount>>,

    #[account(
        init_if_needed,
        payer = admin,
        associated_token::mint = mint,
        associated_token::authority = player_two,
        associated_token::token_program = token_program
    )]
    pub player_two_token_account: Box<InterfaceAccount<'info, TokenAccount>>,

    #[account(constraint = mint.key() == admin_account.mint)]
    pub mint: InterfaceAccount<'info, Mint>,

    pub system_program: Program<'info, System>,
    pub associated_token_program: Program<'info, AssociatedToken>,
    pub token_program: Interface<'info, TokenInterface>,
}

pub fn admin_withdraw_ix(ctx: Context<AdminWithdraw>, battle_id: u64) -> Result<()> {
    if ctx.accounts.battle_account.battle_id != battle_id {
        return Err(ScoogiBattleError::InvalidBattleId.into());
    }

    if ctx.accounts.admin.key() != ctx.accounts.admin_account.admin {
        return Err(ScoogiBattleError::InvalidWithdrawal.into());
    }

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
        to: ctx.accounts.player_one_token_account.to_account_info(),
        authority: ctx.accounts.battle_token_account.to_account_info(),
    };
    let cpi_ctx = CpiContext::new_with_signer(cpi_program, cpi_accounts, signer_seeds);

    let amount = ctx
        .accounts
        .battle_token_account
        .amount
        .checked_div(2)
        .unwrap();

    token::transfer_checked(cpi_ctx, amount, ctx.accounts.mint.decimals)?;

    let cpi_program = ctx.accounts.token_program.to_account_info();
    let cpi_accounts = TransferChecked {
        from: ctx.accounts.battle_token_account.to_account_info(),
        mint: ctx.accounts.mint.to_account_info(),
        to: ctx.accounts.player_two_token_account.to_account_info(),
        authority: ctx.accounts.battle_token_account.to_account_info(),
    };
    let cpi_ctx = CpiContext::new_with_signer(cpi_program, cpi_accounts, signer_seeds);

    token::transfer_checked(cpi_ctx, amount, ctx.accounts.mint.decimals)?;

    let cpi_program = ctx.accounts.token_program.to_account_info();
    let cpi_accounts = CloseAccount {
        account: ctx.accounts.battle_token_account.to_account_info(),
        destination: ctx.accounts.admin.to_account_info(),
        authority: ctx.accounts.battle_token_account.to_account_info(),
    };
    let cpi_ctx = CpiContext::new_with_signer(cpi_program, cpi_accounts, signer_seeds);

    token::close_account(cpi_ctx)?;

    Ok(())
}
