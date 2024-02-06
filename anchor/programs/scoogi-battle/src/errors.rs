use anchor_lang::prelude::*;

#[error_code]
pub enum ScoogiBattleError {
    /// 0x1770 - 6000
    #[msg("Invalid battle result")]
    InvalidBattleResult,

    /// 0x1771 - 6001
    #[msg("Invalid battle id")]
    InvalidBattleId,

    /// 0x1772 - 6002
    #[msg("Invalid battle status")]
    InvalidBattleStatus,

    /// 0x1773 - 6003
    #[msg("Invalid withdrawal")]
    InvalidWithdrawal,

    /// 0x1774 - 6004
    #[msg("Internal error")]
    InternalError,
}
