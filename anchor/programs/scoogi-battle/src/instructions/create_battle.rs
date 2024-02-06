use anchor_lang::{prelude::*, solana_program::pubkey::Pubkey};
use anchor_spl::{
    associated_token::AssociatedToken,
    token_2022 as token,
    token_interface::{Mint, TokenAccount, TokenInterface, TransferChecked},
};

use crate::{constants, Admin, Battle, BattleStatus};

#[derive(Accounts)]
#[instruction(battle_id: u64)]
pub struct CreateBattle<'info> {
    #[account(mut, signer)]
    pub player_one: Signer<'info>,

    #[account(seeds = [constants::ADMIN_SEED], bump, has_one = mint)]
    pub admin_account: Account<'info, Admin>,

    #[account(
        init,
        payer = player_one,
        space = Battle::size(),
        seeds = [constants::BATTLE_SEED, player_one.key().as_ref(), &battle_id.to_le_bytes()[..]],
        bump
    )]
    pub battle_account: Account<'info, Battle>,

    #[account(
        mut,
        associated_token::mint = mint,
        associated_token::authority = player_one,
        associated_token::token_program = token_program
    )]
    pub player_one_token_account: Box<InterfaceAccount<'info, TokenAccount>>,

    #[account(
        init,
        seeds = [constants::TOKEN_ACCOUNT_SEED, player_one.key().as_ref(), &battle_id.to_le_bytes()[..]],
        bump,
        payer = player_one,
        token::mint = mint,
        token::authority = battle_token_account,
        token::token_program = token_program
    )]
    pub battle_token_account: Box<InterfaceAccount<'info, TokenAccount>>,

    #[account(constraint = mint.key() == admin_account.mint)]
    pub mint: Box<InterfaceAccount<'info, Mint>>,

    pub system_program: Program<'info, System>,
    pub associated_token_program: Program<'info, AssociatedToken>,
    pub token_program: Interface<'info, TokenInterface>,
}

pub fn create_battle_ix(ctx: Context<CreateBattle>, battle_id: u64) -> Result<()> {
    ctx.accounts.battle_account.battle_id = battle_id;
    ctx.accounts.battle_account.player_one = ctx.accounts.player_one.key();
    ctx.accounts.battle_account.player_two = Pubkey::default();
    ctx.accounts.battle_account.battle_status = BattleStatus::Pending;

    let cpi_program = ctx.accounts.token_program.to_account_info();
    let cpi_accounts = TransferChecked {
        from: ctx.accounts.player_one_token_account.to_account_info(),
        mint: ctx.accounts.mint.to_account_info(),
        to: ctx.accounts.battle_token_account.to_account_info(),
        authority: ctx.accounts.player_one.to_account_info(),
    };
    let cpi_ctx = CpiContext::new(cpi_program, cpi_accounts);

    token::transfer_checked(
        cpi_ctx,
        ctx.accounts.admin_account.battle_price,
        ctx.accounts.mint.decimals,
    )?;

    Ok(())
}
