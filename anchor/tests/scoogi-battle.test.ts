import * as anchor from '@coral-xyz/anchor';

import {
  ASSOCIATED_TOKEN_PROGRAM_ID,
  Account,
  TOKEN_PROGRAM_ID,
  createInitializeMintInstruction,
  createMint,
  getMintLen,
  getOrCreateAssociatedTokenAccount,
  mintToChecked,
} from '@solana/spl-token';
import {
  Connection,
  Keypair,
  LAMPORTS_PER_SOL,
  PublicKey,
  SystemProgram,
  Transaction,
} from '@solana/web3.js';

import { Program } from '@coral-xyz/anchor';
import { ScoogiBattle } from '../target/types/scoogi_battle';

enum BattleStatus {
  Pending,
  InProgress,
  Completed,
}

async function airdrop(
  connection: Connection,
  to: PublicKey,
  lamports = 2 * LAMPORTS_PER_SOL
) {
  await connection.confirmTransaction(
    await connection.requestAirdrop(to, lamports),
    'confirmed'
  );
}

const ADMIN_SEED = Buffer.from('admin');
const BATTLE_SEED = Buffer.from('battle');
const TOKEN_ACCOUNT_SEED = Buffer.from('token_account');

describe('🐸 Scoogi Battle 🤺', () => {
  // Configure the client to use the local cluster.
  anchor.setProvider(anchor.AnchorProvider.env());
  const connection = anchor.getProvider().connection;

  const program = anchor.workspace.ScoogiBattle as Program<ScoogiBattle>;

  let mint: PublicKey;
  let playerOneTokenAccount: Account;
  let playerTwoTokenAccount: Account;
  let battleAccountAddress: PublicKey;
  let battleTokenAccountAddress: PublicKey;

  const startBattleId = new anchor.BN(Math.ceil(new Date().getTime() / 1000));
  const admin = anchor.Wallet.local();
  const playerOne = Keypair.generate();
  const playerTwo = Keypair.generate();
  const [adminAccount] = PublicKey.findProgramAddressSync(
    [ADMIN_SEED],
    program.programId
  );

  beforeAll(async () => {
    try {
      await airdrop(connection, admin.publicKey);
      await airdrop(connection, playerOne.publicKey);
      await airdrop(connection, playerTwo.publicKey);
    } catch (error) {
      console.error('Error:', error);
    }

    mint = await createMint(
      connection,
      admin.payer,
      admin.publicKey,
      admin.publicKey,
      9,
      undefined,
      undefined,
      TOKEN_PROGRAM_ID
    );

    playerOneTokenAccount = await getOrCreateAssociatedTokenAccount(
      connection,
      admin.payer,
      mint,
      playerOne.publicKey,
      false,
      undefined,
      undefined,
      TOKEN_PROGRAM_ID,
      ASSOCIATED_TOKEN_PROGRAM_ID
    );

    playerTwoTokenAccount = await getOrCreateAssociatedTokenAccount(
      connection,
      admin.payer,
      mint,
      playerTwo.publicKey,
      false,
      undefined,
      undefined,
      TOKEN_PROGRAM_ID,
      ASSOCIATED_TOKEN_PROGRAM_ID
    );

    await mintToChecked(
      connection,
      admin.payer,
      mint,
      playerOneTokenAccount.address,
      admin.publicKey,
      10_000,
      9,
      [],
      undefined,
      TOKEN_PROGRAM_ID
    );

    await mintToChecked(
      connection,
      admin.payer,
      mint,
      playerTwoTokenAccount.address,
      admin.publicKey,
      10_000,
      9,
      [],
      undefined,
      TOKEN_PROGRAM_ID
    );

    [battleTokenAccountAddress] = PublicKey.findProgramAddressSync(
      [
        TOKEN_ACCOUNT_SEED,
        playerOne.publicKey.toBuffer(),
        startBattleId.toArrayLike(Buffer, 'le', 8),
      ],
      program.programId
    );

    [battleAccountAddress] = PublicKey.findProgramAddressSync(
      [
        BATTLE_SEED,
        playerOne.publicKey.toBuffer(),
        startBattleId.toArrayLike(Buffer, 'le', 8),
      ],
      program.programId
    );
  });

  it('Initialization', async () => {
    const burnFeeBps = new anchor.BN(100);
    const battlePrice = new anchor.BN(10_000);
    const tx = await program.methods
      .initialize(burnFeeBps, battlePrice)
      .accounts({
        admin: admin.publicKey,
        adminAccount,
        mint,
        systemProgram: SystemProgram.programId,
      })
      .signers([admin.payer])
      .rpc();

    await connection.confirmTransaction(tx, 'confirmed');

    const adminAccountData = await program.account.admin.fetch(adminAccount);

    expect(adminAccountData.admin.toBase58()).toBe(admin.publicKey.toBase58());
    expect(adminAccountData.mint.toBase58()).toBe(mint.toBase58());
    expect(adminAccountData.burnFeeBps.toString()).toBe(burnFeeBps.toString());
    expect(adminAccountData.battlePrice.toString()).toBe(
      battlePrice.mul(new anchor.BN(LAMPORTS_PER_SOL)).toString()
    );
  });

  it('Creates a battle', async () => {
    const tx = await program.methods
      .createBattle(startBattleId)
      .accounts({
        playerOne: playerOne.publicKey,
        adminAccount,
        battleAccount: battleAccountAddress,
        playerOneTokenAccount: playerOneTokenAccount.address,
        battleTokenAccount: battleTokenAccountAddress,
        mint,
        systemProgram: SystemProgram.programId,
        associatedTokenProgram: ASSOCIATED_TOKEN_PROGRAM_ID,
        tokenProgram: TOKEN_PROGRAM_ID,
      })
      .signers([playerOne])
      .rpc();

    await connection.confirmTransaction(tx, 'confirmed');

    const battleAccountData = await program.account.battle.fetch(
      battleAccountAddress
    );

    expect(battleAccountData.battleId.toString()).toBe(
      startBattleId.toString()
    );
    expect(battleAccountData.playerOne.toBase58()).toBe(
      playerOne.publicKey.toBase58()
    );
    expect(battleAccountData.playerTwo.toBase58()).toBe(new PublicKey(''));
    expect(battleAccountData.battleStatus).toBe(BattleStatus.Pending);
  });
});
